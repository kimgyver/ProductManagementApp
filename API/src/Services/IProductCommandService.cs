using API.Models;

namespace API.Services;

public interface IProductCommandService
{
  Task<bool> AddProductAsync(Product product, bool isAdmin);
  Task<bool> UpdateProductAsync(Product product, bool isAdmin);
  Task<bool> DeleteProductAsync(int id, bool isAdmin);
}
