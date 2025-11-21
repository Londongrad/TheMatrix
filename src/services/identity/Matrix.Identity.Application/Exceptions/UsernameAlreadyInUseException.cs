namespace Matrix.Identity.Application.Exceptions
{
    public class UsernameAlreadyInUseException(string username)
        : Exception($"User with username '{username}' already exists.")
    {
        public string Username { get; } = username;
    }
}
