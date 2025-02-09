public interface ISessionService
{
  public Task GenerateSessionAsync(string username, bool isAdmin);
  public Task RemoveSessionAsync();
}