using Microsoft.EntityFrameworkCore;
using API.Models;
using API.Exceptions;
using Npgsql;
using System.Text;

namespace API.Infrastructure;

public class UserRepository : IUserRepository
{
  private readonly ApplicationDbContext _context;

  public UserRepository(ApplicationDbContext context)
  {
    _context = context;
  }

  public async Task AddAsync(User user)
  {
    try
    {
      await _context.Users.AddAsync(user);
      await _context.SaveChangesAsync();
    }
    catch (DbUpdateException ex) when (!IsDuplicateEmailError(ex))
    {
      _context.Entry(user).State = EntityState.Detached;
      await AddToLegacyUserSchemaAsync(user);
    }
    catch (Exception ex) when (IsDuplicateEmailError(ex))
    {
      throw new DuplicateEmailException($"Email `{user.Email} is already registered.`", ex);
    }
    catch (Exception ex) when (IsLegacySchemaMismatch(ex))
    {
      await AddToLegacyUserSchemaAsync(user);
    }
  }

  private async Task AddToLegacyUserSchemaAsync(User user)
  {
    await using var connection = _context.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
      await connection.OpenAsync();
    }

    var columns = await GetLegacyUserColumnsAsync(connection);
    if (columns.Count == 0)
    {
      throw new InvalidOperationException("Legacy user table was not found.");
    }

    if (!columns.Contains("email", StringComparer.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException("Legacy user table does not have an email column.");
    }

    if (await LegacyEmailExistsAsync(connection, user.Email))
    {
      throw new DuplicateEmailException($"Email `{user.Email} is already registered.`", new InvalidOperationException("Legacy duplicate email"));
    }

    var insertColumns = new List<string>();
    var values = new List<object?>();

    AddIfPresent(columns, insertColumns, values, "email", user.Email);
    AddIfPresent(columns, insertColumns, values, "username", user.Username);
    AddIfPresent(columns, insertColumns, values, "name", user.Username);
    AddIfPresent(columns, insertColumns, values, "hashedpassword", user.HashedPassword);
    AddIfPresent(columns, insertColumns, values, "hashed_password", user.HashedPassword);
    AddIfPresent(columns, insertColumns, values, "password", user.HashedPassword);
    AddIfPresent(columns, insertColumns, values, "isadmin", user.IsAdmin);
    AddIfPresent(columns, insertColumns, values, "is_admin", user.IsAdmin);
    AddIfPresent(columns, insertColumns, values, "verified", user.Verified);
    AddIfPresent(columns, insertColumns, values, "role", string.IsNullOrWhiteSpace(user.Role) ? "customer" : user.Role);
    AddIfPresent(columns, insertColumns, values, "createdat", DateTime.UtcNow);
    AddIfPresent(columns, insertColumns, values, "created_at", DateTime.UtcNow);
    AddIfPresent(columns, insertColumns, values, "updatedat", DateTime.UtcNow);
    AddIfPresent(columns, insertColumns, values, "updated_at", DateTime.UtcNow);

    if (insertColumns.Count == 0)
    {
      throw new InvalidOperationException("No compatible columns were found for legacy user insert.");
    }

    await using var insert = connection.CreateCommand();
    var columnSql = string.Join(", ", insertColumns.Select(c => $"\"{c}\""));
    var valueSql = new StringBuilder();

    for (var i = 0; i < values.Count; i++)
    {
      if (i > 0)
      {
        valueSql.Append(", ");
      }

      var paramName = $"@p{i}";
      valueSql.Append(paramName);

      var p = insert.CreateParameter();
      p.ParameterName = paramName;
      p.Value = values[i] ?? DBNull.Value;
      insert.Parameters.Add(p);
    }

    insert.CommandText = $"INSERT INTO \"User\" ({columnSql}) VALUES ({valueSql})";
    await insert.ExecuteNonQueryAsync();
  }

  private static void AddIfPresent(IReadOnlyCollection<string> availableColumns, ICollection<string> insertColumns, ICollection<object?> values, string columnName, object? value)
  {
    var actualColumnName = availableColumns.FirstOrDefault(c => c.Equals(columnName, StringComparison.OrdinalIgnoreCase));
    if (actualColumnName != null && !insertColumns.Contains(actualColumnName, StringComparer.OrdinalIgnoreCase))
    {
      insertColumns.Add(actualColumnName);
      values.Add(value);
    }
  }

  private static bool IsDuplicateEmailError(Exception ex)
  {
    if (ex is PostgresException pg && pg.SqlState == "23505")
    {
      return true;
    }

    if (ex.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    return ex.InnerException != null && IsDuplicateEmailError(ex.InnerException);
  }

  private static async Task<HashSet<string>> GetLegacyUserColumnsAsync(System.Data.Common.DbConnection connection)
  {
    var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'User'";

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
      var name = reader.GetString(0);
      result.Add(name);
    }

    return result;
  }

  private static async Task<bool> LegacyEmailExistsAsync(System.Data.Common.DbConnection connection, string email)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT 1 FROM \"User\" WHERE lower(email) = lower(@email) LIMIT 1";

    var p = command.CreateParameter();
    p.ParameterName = "@email";
    p.Value = email;
    command.Parameters.Add(p);

    var scalar = await command.ExecuteScalarAsync();
    return scalar != null && scalar != DBNull.Value;
  }

  public async Task<IEnumerable<User>> GetAllUsersAsync()
  {
    try
    {
      return await _context.Users
        .Select(u => new User
        {
          Id = u.Id,
          Username = u.Username,
          Email = u.Email,
          HashedPassword = u.HashedPassword,
          IsAdmin = u.IsAdmin
        })
        .ToListAsync();
    }
    catch (Exception ex) when (IsLegacySchemaMismatch(ex))
    {
      return await GetAllUsersFromLegacySchemaAsync();
    }
  }

  public async Task<User?> GetUserForLoginByEmailAsync(string email)
  {
    try
    {
      return await _context.Users
        .Where(u => u.Email == email)
        .Select(u => new User
        {
          Id = u.Id,
          Username = u.Username,
          Email = u.Email,
          HashedPassword = u.HashedPassword,
          IsAdmin = u.IsAdmin
        })
        .FirstOrDefaultAsync();
    }
    catch (Exception ex) when (IsLegacySchemaMismatch(ex))
    {
      return await GetUserForLoginFromLegacySchemaAsync(email);
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

  private async Task<User?> GetUserForLoginFromLegacySchemaAsync(string email)
  {
    await using var connection = _context.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
      await connection.OpenAsync();
    }

    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT * FROM \"User\" WHERE lower(email) = lower(@email) LIMIT 1";

    var emailParam = command.CreateParameter();
    emailParam.ParameterName = "@email";
    emailParam.Value = email;
    command.Parameters.Add(emailParam);

    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
      return null;
    }

    var userEmail = GetString(reader, "email") ?? email;
    var username = GetString(reader, "username", "name") ?? userEmail;
    var passwordHash = GetString(reader, "hashedpassword", "hashed_password", "password") ?? string.Empty;
    var role = GetString(reader, "role") ?? "customer";
    var isAdmin = GetBoolean(reader, "isadmin", "is_admin") || role.Equals("admin", StringComparison.OrdinalIgnoreCase);

    return new User
    {
      Id = 0,
      Username = username,
      Email = userEmail,
      HashedPassword = passwordHash,
      IsAdmin = isAdmin,
      Role = role
    };
  }

  private async Task<IEnumerable<User>> GetAllUsersFromLegacySchemaAsync()
  {
    var users = new List<User>();

    await using var connection = _context.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
      await connection.OpenAsync();
    }

    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT * FROM \"User\"";

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
      var userEmail = GetString(reader, "email") ?? string.Empty;
      var username = GetString(reader, "username", "name") ?? userEmail;
      var role = GetString(reader, "role") ?? "customer";
      var isAdmin = GetBoolean(reader, "isadmin", "is_admin") || role.Equals("admin", StringComparison.OrdinalIgnoreCase);

      users.Add(new User
      {
        Id = 0,
        Username = username,
        Email = userEmail,
        HashedPassword = GetString(reader, "hashedpassword", "hashed_password", "password") ?? string.Empty,
        IsAdmin = isAdmin,
        Role = role
      });
    }

    return users;
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

  private static bool GetBoolean(System.Data.Common.DbDataReader reader, params string[] candidateNames)
  {
    foreach (var name in candidateNames)
    {
      var ordinal = GetOrdinal(reader, name);
      if (ordinal >= 0 && !reader.IsDBNull(ordinal))
      {
        var value = reader.GetValue(ordinal);
        if (value is bool b)
        {
          return b;
        }

        if (bool.TryParse(value.ToString(), out var parsed))
        {
          return parsed;
        }
      }
    }

    return false;
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

  public async Task RemoveUserAsync(int id)
  {
    var user = await _context.Users.FindAsync(id);
    if (user == null)
    {
      throw new UserNotFoundException($"User with Id {id} not found");
    }

    _context.Users.Remove(user);
    await _context.SaveChangesAsync();

  }

  public async Task<User?> UpdateUserAsync(User user)
  {
    var userFound = await _context.Users.FindAsync(user.Id);
    if (userFound == null)
    {
      throw new UserNotFoundException($"User with Id {user.Id} not found");
    }

    userFound.Username = user.Username;
    userFound.Email = user.Email;
    await _context.SaveChangesAsync();
    return userFound;
  }

  public async Task MarkUserUnverifiedAsync(string email)
  {
    var user = await _context.Users.FirstOrDefaultAsync(user => user.Email == email);
    if (user == null) return;

    user.Verified = false;
    await _context.SaveChangesAsync();
  }
}