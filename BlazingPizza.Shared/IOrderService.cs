namespace BlazingPizza.Shared
{
    public interface IOrderService
    {
        Task<int> PlaceOrder(Order order);
        // Other methods if needed
    }
}
