public interface IUserRepository
{
  public Task<IEnumerable<User>> GetAllUsersAsync();
  public Task AddAsync(User user);
  public Task RemoveUserAsync(int id);
  public Task UpdateUserAsync(User user);
}