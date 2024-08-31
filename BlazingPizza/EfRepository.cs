using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace BlazingPizza;

public class EfRepository : IRepository
{
    private readonly PizzaStoreContext _context;
    private readonly IDatabase _redisDatabase;

    public EfRepository(PizzaStoreContext context, IConnectionMultiplexer redis)
    {
        _context = context;
        _redisDatabase = redis.GetDatabase();
    }

    public async Task<List<OrderWithStatus>> GetOrdersAsync()
    {
        var cacheKey = "orders_all";
        var cachedOrders = await _redisDatabase.StringGetAsync(cacheKey);
        if (!cachedOrders.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<List<OrderWithStatus>>(cachedOrders);
        }

        var orders = await _context.Orders
                        .Include(o => o.DeliveryLocation)
                        .Include(o => o.Pizzas).ThenInclude(p => p.Special)
                        .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
                        .OrderByDescending(o => o.CreatedTime)
                        .ToListAsync();

        var ordersWithStatus = orders.Select(o => OrderWithStatus.FromOrder(o)).ToList();

        await _redisDatabase.StringSetAsync(cacheKey, JsonSerializer.Serialize(ordersWithStatus));

        return ordersWithStatus;
    }

    public async Task<List<OrderWithStatus>> GetOrdersAsync(string userId)
    {
        var cacheKey = $"orders_{userId}";
        var cachedOrders = await _redisDatabase.StringGetAsync(cacheKey);
        if (!cachedOrders.IsNullOrEmpty)
        {
            var ordersFromCache = JsonSerializer.Deserialize<List<OrderWithStatus>>(cachedOrders);
            Console.WriteLine($"Fetched {ordersFromCache.Count} orders from cache for user {userId}");
            return ordersFromCache;
        }

        var orders = await _context.Orders
                        .Where(o => o.UserId == userId)
                        .Include(o => o.DeliveryLocation)
                        .Include(o => o.Pizzas).ThenInclude(p => p.Special)
                        .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
                        .OrderByDescending(o => o.CreatedTime)
                        .ToListAsync();

        var ordersWithStatus = orders.Select(o => OrderWithStatus.FromOrder(o)).ToList();

        await _redisDatabase.StringSetAsync(cacheKey, JsonSerializer.Serialize(ordersWithStatus));
        Console.WriteLine($"Fetched {ordersWithStatus.Count} orders from database for user {userId}");

        return ordersWithStatus;
    }

    public async Task<OrderWithStatus> GetOrderWithStatus(int orderId)
    {
        var cacheKey = $"order_{orderId}";
        var cachedOrder = await _redisDatabase.StringGetAsync(cacheKey);
        if (!cachedOrder.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<OrderWithStatus>(cachedOrder);
        }

        var order = await _context.Orders
                        .Where(o => o.OrderId == orderId)
                        .Include(o => o.DeliveryLocation)
                        .Include(o => o.Pizzas).ThenInclude(p => p.Special)
                        .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
                        .SingleOrDefaultAsync();

        if (order is null) throw new ArgumentNullException(nameof(order));

        var orderWithStatus = OrderWithStatus.FromOrder(order);

        await _redisDatabase.StringSetAsync(cacheKey, JsonSerializer.Serialize(orderWithStatus));

        return orderWithStatus;
    }

    public async Task<OrderWithStatus> GetOrderWithStatus(int orderId, string userId)
    {
        var cacheKey = $"order_{orderId}_{userId}";
        var cachedOrder = await _redisDatabase.StringGetAsync(cacheKey);
        if (!cachedOrder.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<OrderWithStatus>(cachedOrder);
        }

        var order = await _context.Orders
                        .Where(o => o.OrderId == orderId && o.UserId == userId)
                        .Include(o => o.DeliveryLocation)
                        .Include(o => o.Pizzas).ThenInclude(p => p.Special)
                        .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
                        .SingleOrDefaultAsync();

        if (order is null) throw new ArgumentNullException(nameof(order));

        var orderWithStatus = OrderWithStatus.FromOrder(order);

        await _redisDatabase.StringSetAsync(cacheKey, JsonSerializer.Serialize(orderWithStatus));

        return orderWithStatus;
    }

    public async Task<List<PizzaSpecial>> GetSpecials()
    {
        return await _context.Specials.ToListAsync();
    }

    public async Task<List<Topping>> GetToppings()
    {
        return await _context.Toppings.OrderBy(t => t.Name).ToListAsync();
    }

    public async Task<int> PlaceOrder(BlazingPizza.Shared.Order order)
    {
        // Log entry into the method
        Console.WriteLine("PlaceOrder method called in EfRepository");

        // Add the order to the database
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        Console.WriteLine("Order saved to database");

        // Prepare the order with status object
        var orderWithStatus = OrderWithStatus.FromOrder(order);

        // Use Redis transaction to ensure atomic updates
        var tran = _redisDatabase.CreateTransaction();

        // Cache the individual order
        var orderCacheKey = $"order_{order.OrderId}";
        var orderSetTask = tran.StringSetAsync(orderCacheKey, JsonSerializer.Serialize(orderWithStatus));
        Console.WriteLine($"Prepared to set order cache with key: {orderCacheKey}");

        // Update the user's orders list in the cache
        var userCacheKey = $"orders_{order.UserId}";
        var cachedOrders = await _redisDatabase.StringGetAsync(userCacheKey);
        Console.WriteLine($"Fetched current orders for user: {order.UserId}");

        List<OrderWithStatus> userOrders;
        if (!cachedOrders.IsNullOrEmpty)
        {
            userOrders = JsonSerializer.Deserialize<List<OrderWithStatus>>(cachedOrders) ?? new List<OrderWithStatus>();
            Console.WriteLine($"Deserialized {userOrders.Count} existing orders for user: {order.UserId}");
        }
        else
        {
            userOrders = new List<OrderWithStatus>();
            Console.WriteLine($"No existing orders found for user: {order.UserId}");
        }

        userOrders.Add(orderWithStatus);
        Console.WriteLine($"Added new order to user orders list, total orders now: {userOrders.Count}");

        var userOrdersSetTask = tran.StringSetAsync(userCacheKey, JsonSerializer.Serialize(userOrders));
        Console.WriteLine($"Prepared to update user orders cache with key: {userCacheKey}");

        // Execute the transaction
        bool committed = await tran.ExecuteAsync();
        if (!committed)
        {
            throw new Exception("Redis transaction failed to commit.");
        }

        // Verify if individual set operations succeeded
        bool orderSetResult = await orderSetTask;
        bool userOrdersSetResult = await userOrdersSetTask;

        if (!orderSetResult || !userOrdersSetResult)
        {
            throw new Exception("Failed to set individual keys in Redis.");
        }

        Console.WriteLine("Order and user orders list updated in Redis");

        return order.OrderId;
    }

    public Task SubscribeToNotifications(NotificationSubscription subscription)
    {
        throw new NotImplementedException();
    }
}