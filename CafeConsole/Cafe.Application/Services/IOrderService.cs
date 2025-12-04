using Cafe.Domain;

namespace Cafe.Application;

public interface IOrderService
{
    Result PlaceOrder(OrderRequest orderRequest);
}
