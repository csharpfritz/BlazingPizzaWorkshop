namespace BlazingPizza
{
    public class OrderService : IOrderService
    {
        private readonly IRepository _repository;

        public OrderService(IRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> PlaceOrder(Order order)
        {
            return await _repository.PlaceOrder(order);
        }

        // Other methods if needed
    }
}
