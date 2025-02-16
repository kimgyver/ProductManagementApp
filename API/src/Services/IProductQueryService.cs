using API.Models;

namespace API.Services;

public interface IProductQueryService
{
  Task<IEnumerable<Product>> GetAllProductsAsync();
  Task<Product?> GetProductByIdAsync(int id);
}
