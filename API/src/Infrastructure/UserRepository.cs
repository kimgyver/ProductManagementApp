
using Microsoft.EntityFrameworkCore;

public class UserRepository : IUserRepository
{
  private readonly ApplicationDbContext _context;

  public UserRepository(ApplicationDbContext context)
  {
    _context = context;
  }

  public async Task AddAsync(User user)
  {
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();
  }

  public async Task<IEnumerable<User>> GetAllUsersAsync()
  {
    return await _context.Users.ToListAsync();
  }

  public async Task RemoveUserAsync(int id)
  {
    var user = await _context.Users.FindAsync(id);
    if (user != null)
    {
      _context.Users.Remove(user);
      await _context.SaveChangesAsync();
    }
  }

  public async Task<User?> UpdateUserAsync(User user)
  {
    var userFound = await _context.Users.FindAsync(user.Id);
    if (userFound == null)
      return null;

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