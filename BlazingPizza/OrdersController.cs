using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebPush;
using BlazingPizza.Shared;

namespace BlazingPizza;

[Route("orders")]
[ApiController]
[Authorize]
public class OrdersController : Controller
{
    private readonly PizzaStoreContext _db;
    private readonly IDatabase _redisDatabase;
    private readonly JsonSerializerOptions _jsonOptions;

    public OrdersController(PizzaStoreContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redisDatabase = redis.GetDatabase();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Use PascalCase
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles // Avoid circular references
        };
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderWithStatus>>> GetOrders()
    {
        var userId = PizzaApiExtensions.GetUserId(HttpContext);
        var cacheKey = $"orders_{userId}";
        var cachedOrders = await _redisDatabase.StringGetAsync(cacheKey);

        if (!cachedOrders.IsNullOrEmpty)
        {
            var ordersFromCache = JsonSerializer.Deserialize<List<OrderWithStatus>>(cachedOrders, _jsonOptions);
            return ordersFromCache!;
        }

        var orders = await _db.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.DeliveryLocation)
            .Include(o => o.Pizzas).ThenInclude(p => p.Special)
            .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
            .OrderByDescending(o => o.CreatedTime)
            .ToListAsync();

        var ordersWithStatus = orders.Select(o => OrderWithStatus.FromOrder(o)).ToList();

        await _redisDatabase.StringSetAsync(cacheKey, JsonSerializer.Serialize(ordersWithStatus, _jsonOptions));

        return ordersWithStatus;
    }

    [HttpGet("{orderId}")]
    public async Task<ActionResult<OrderWithStatus>> GetOrderWithStatus(int orderId)
    {
        var cacheKey = $"order_{orderId}";
        var cachedOrder = await _redisDatabase.StringGetAsync(cacheKey);

        if (!cachedOrder.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<OrderWithStatus>(cachedOrder, _jsonOptions);
        }

        var order = await _db.Orders
            .Where(o => o.OrderId == orderId)
            .Where(o => o.UserId == PizzaApiExtensions.GetUserId(HttpContext))
            .Include(o => o.DeliveryLocation)
            .Include(o => o.Pizzas).ThenInclude(p => p.Special)
            .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
            .SingleOrDefaultAsync();

        if (order == null)
        {
            return NotFound();
        }

        var orderWithStatus = OrderWithStatus.FromOrder(order);

        await _redisDatabase.StringSetAsync(cacheKey, JsonSerializer.Serialize(orderWithStatus, _jsonOptions));

        return orderWithStatus;
    }

    [HttpPost]
    public async Task<ActionResult<int>> PlaceOrder(BlazingPizza.Shared.Order order)
    {
        order.CreatedTime = DateTime.UtcNow; // Set to UTC for Postgres
        order.DeliveryLocation = new LatLong(51.5001, -0.1239);
        order.UserId = PizzaApiExtensions.GetUserId(HttpContext);

        // Enforce existence of Pizza.SpecialId and Topping.ToppingId in the database - prevent the submitter from making up new specials and toppings
        foreach (var pizza in order.Pizzas)
        {
            pizza.SpecialId = pizza.Special?.Id ?? 0;
            pizza.Special = null;

            foreach (var topping in pizza.Toppings)
            {
                topping.ToppingId = topping.Topping?.Id ?? 0;
                topping.Topping = null;
            }
        }

        _db.Orders.Attach(order);
        await _db.SaveChangesAsync();

        // Ensure related data is fully populated
        foreach (var pizza in order.Pizzas)
        {
            pizza.Special = await _db.Specials.FindAsync(pizza.SpecialId);
            foreach (var topping in pizza.Toppings)
            {
                topping.Topping = await _db.Toppings.FindAsync(topping.ToppingId);
            }
        }

        // Update Redis cache
        var orderWithStatus = OrderWithStatus.FromOrder(order);
        var orderCacheKey = $"order_{order.OrderId}";
        var userCacheKey = $"orders_{order.UserId}";

        // Cache the individual order
        await _redisDatabase.StringSetAsync(orderCacheKey, JsonSerializer.Serialize(orderWithStatus, _jsonOptions));

        // Refresh the cache for total orders
        var userOrders = await _db.Orders
            .Where(o => o.UserId == order.UserId)
            .Include(o => o.DeliveryLocation)
            .Include(o => o.Pizzas).ThenInclude(p => p.Special)
            .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
            .OrderByDescending(o => o.CreatedTime)
            .Select(o => OrderWithStatus.FromOrder(o))
            .ToListAsync();

        await _redisDatabase.StringSetAsync(userCacheKey, JsonSerializer.Serialize(userOrders, _jsonOptions));

        // In the background, send push notifications if possible
        var subscription = await _db.NotificationSubscriptions
            .Where(e => e.UserId == PizzaApiExtensions.GetUserId(HttpContext))
            .SingleOrDefaultAsync();
        if (subscription != null)
        {
            _ = TrackAndSendNotificationsAsync(order, subscription);
        }

        return order.OrderId;
    }

    private static async Task TrackAndSendNotificationsAsync(BlazingPizza.Shared.Order order, NotificationSubscription subscription)
    {
        // In a realistic case, some other backend process would track order delivery progress and send us notifications when it changes. Since we don't have any such process here, fake it.
        await Task.Delay(OrderWithStatus.PreparationDuration);
        await SendNotificationAsync(order, subscription, "Your order has been dispatched!");

        await Task.Delay(OrderWithStatus.DeliveryDuration);
        await SendNotificationAsync(order, subscription, "Your order is now delivered. Enjoy!");
    }

    private static async Task SendNotificationAsync(BlazingPizza.Shared.Order order, NotificationSubscription subscription, string message)
    {
        // For a real application, generate your own
        var publicKey = "BLC8GOevpcpjQiLkO7JmVClQjycvTCYWm6Cq_a7wJZlstGTVZvwGFFHMYfXt6Njyvgx_GlXJeo5cSiZ1y4JOx1o";
        var privateKey = "OrubzSz3yWACscZXjFQrrtDwCKg-TGFuWhluQ2wLXDo";

        var pushSubscription = new PushSubscription(subscription.Url, subscription.P256dh, subscription.Auth);
        var vapidDetails = new VapidDetails("mailto:<someone@example.com>", publicKey, privateKey);
        var webPushClient = new WebPushClient();
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                message,
                url = $"myorders/{order.OrderId}",
            });
            await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error sending push notification: " + ex.Message);
        }
    }
}