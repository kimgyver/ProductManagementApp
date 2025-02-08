public class ProductCommandService : IProductCommandService
{
  private readonly IProductRepository _productRepository;

  public ProductCommandService(IProductRepository productRepository)
  {
    _productRepository = productRepository;
  }

  public async Task<bool> AddProductAsync(Product product, bool isAdmin)
  {
    if (!isAdmin) return false;
    await _productRepository.AddProductAsync(product);
    return true;
  }

  public async Task<bool> UpdateProductAsync(Product product, bool isAdmin)
  {
    if (!isAdmin) return false;
    await _productRepository.UpdateProductAsync(product);
    return true;
  }

  public async Task<bool> DeleteProductAsync(int id, bool isAdmin)
  {
    if (!isAdmin) return false;
    await _productRepository.DeleteProductAsync(id);
    return true;
  }
}
