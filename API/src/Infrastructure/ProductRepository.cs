using Microsoft.EntityFrameworkCore;
using API.Models;
using Npgsql;

namespace API.Infrastructure;

public class ProductRepository : IProductRepository
{
  private readonly ApplicationDbContext _context;

  public ProductRepository(ApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<IEnumerable<Product>> GetAllProductsAsync()
  {
    try
    {
      return await _context.Products.ToListAsync();
    }
    catch (Exception ex) when (IsLegacySchemaMismatch(ex))
    {
      return await GetAllProductsFromLegacySchemaAsync();
    }
  }

  private static bool IsLegacySchemaMismatch(Exception ex)
  {
    if (ex is PostgresException pg && (pg.SqlState == "42P01" || pg.SqlState == "42703" || pg.SqlState == "22P02"))
    {
      return true;
    }

    if (ex.InnerException != null)
    {
      return IsLegacySchemaMismatch(ex.InnerException);
    }

    return false;
  }

  public async Task<Product?> GetProductByIdAsync(int id)
  {
    return await _context.Products.FindAsync(id);
  }

  private async Task<IEnumerable<Product>> GetAllProductsFromLegacySchemaAsync()
  {
    var products = new List<Product>();

    await using var connection = _context.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
      await connection.OpenAsync();
    }

    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT * FROM \"Product\"";

    await using var reader = await command.ExecuteReaderAsync();
    var index = 1;
    while (await reader.ReadAsync())
    {
      products.Add(new Product
      {
        Id = index++,
        Sku = GetString(reader, "sku", "id") ?? $"SKU-{index}",
        Name = GetString(reader, "name", "title") ?? "Unnamed Product",
        Description = GetString(reader, "description") ?? string.Empty,
        Category = GetString(reader, "category") ?? string.Empty,
        Price = GetDecimal(reader, "price"),
        Stock = GetInt(reader, "stock", "quantity"),
        Rating = (float?)GetDecimalNullable(reader, "rating"),
        ReviewCount = GetIntNullable(reader, "reviewcount", "review_count")
      });
    }

    return products;
  }

  private static string? GetString(System.Data.Common.DbDataReader reader, params string[] candidateNames)
  {
    foreach (var name in candidateNames)
    {
      var ordinal = GetOrdinal(reader, name);
      if (ordinal >= 0 && !reader.IsDBNull(ordinal))
      {
        return reader.GetValue(ordinal)?.ToString();
      }
    }

    return null;
  }

  private static decimal GetDecimal(System.Data.Common.DbDataReader reader, params string[] candidateNames)
  {
    return GetDecimalNullable(reader, candidateNames) ?? 0m;
  }

  private static decimal? GetDecimalNullable(System.Data.Common.DbDataReader reader, params string[] candidateNames)
  {
    foreach (var name in candidateNames)
    {
      var ordinal = GetOrdinal(reader, name);
      if (ordinal >= 0 && !reader.IsDBNull(ordinal))
      {
        var value = reader.GetValue(ordinal);
        if (value is decimal d)
        {
          return d;
        }

        if (decimal.TryParse(value.ToString(), out var parsed))
        {
          return parsed;
        }
      }
    }

    return null;
  }

  private static int GetInt(System.Data.Common.DbDataReader reader, params string[] candidateNames)
  {
    return GetIntNullable(reader, candidateNames) ?? 0;
  }

  private static int? GetIntNullable(System.Data.Common.DbDataReader reader, params string[] candidateNames)
  {
    foreach (var name in candidateNames)
    {
      var ordinal = GetOrdinal(reader, name);
      if (ordinal >= 0 && !reader.IsDBNull(ordinal))
      {
        var value = reader.GetValue(ordinal);
        if (value is int i)
        {
          return i;
        }

        if (int.TryParse(value.ToString(), out var parsed))
        {
          return parsed;
        }
      }
    }

    return null;
  }

  private static int GetOrdinal(System.Data.Common.DbDataReader reader, string columnName)
  {
    for (var i = 0; i < reader.FieldCount; i++)
    {
      if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
      {
        return i;
      }
    }

    return -1;
  }

  public async Task AddProductAsync(Product product)
  {
    _context.Products.Add(product);
    await _context.SaveChangesAsync();
  }

  public async Task UpdateProductAsync(Product product)
  {
    _context.Products.Update(product);
    await _context.SaveChangesAsync();
  }

  public async Task DeleteProductAsync(int id)
  {
    var product = await _context.Products.FindAsync(id);
    if (product != null)
    {
      _context.Products.Remove(product);
      await _context.SaveChangesAsync();
    }
  }
}
