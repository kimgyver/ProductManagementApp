namespace API.Exceptions;

public class DuplicateEmailException : Exception
{
  public DuplicateEmailException(string message, Exception innerException)
      : base(message, innerException)
  {
  }
}