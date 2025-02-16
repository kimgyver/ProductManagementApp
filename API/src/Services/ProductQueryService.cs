using API.Infrastructure;
using API.Models;

namespace API.Services;

public class ProductQueryService : IProductQueryService
{
  private readonly IProductRepository _productRepository;

  public ProductQueryService(IProductRepository productRepository)
  {
    _productRepository = productRepository;
  }

  public async Task<IEnumerable<Product>> GetAllProductsAsync()
  {
    return await _productRepository.GetAllProductsAsync();
  }

  public async Task<Product?> GetProductByIdAsync(int id)
  {
    return await _productRepository.GetProductByIdAsync(id);
  }
}
