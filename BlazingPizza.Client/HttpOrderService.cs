using BlazingPizza.Shared;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BlazingPizza.Client
{
    public class HttpOrderService : IOrderService
    {
        private readonly HttpClient _httpClient;

        public HttpOrderService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<int> PlaceOrder(Order order)
        {
            var response = await _httpClient.PostAsJsonAsync("orders", order);
            response.EnsureSuccessStatusCode();
            var newOrderId = await response.Content.ReadFromJsonAsync<int>();
            return newOrderId;
        }
    }
}
