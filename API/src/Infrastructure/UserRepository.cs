using Microsoft.EntityFrameworkCore;
using API.Models;
using API.Exceptions;
using Npgsql;

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
    catch (Exception ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
    {
      throw new DuplicateEmailException($"Email `{user.Email} is already registered.`", ex);
    }
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