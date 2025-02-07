public interface IProductService
{
  Task<IEnumerable<Product>> GetAllProductsAsync();
  Task<Product?> GetProductByIdAsync(int id);
  Task<bool> AddProductAsync(Product product, bool isAdmin);
  Task<bool> UpdateProductAsync(Product product, bool isAdmin);
  Task<bool> DeleteProductAsync(int id, bool isAdmin);
}
