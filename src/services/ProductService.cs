public class ProductService : IProductService
{
  private readonly IProductRepository _productRepository;

  public ProductService(IProductRepository productRepository)
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
