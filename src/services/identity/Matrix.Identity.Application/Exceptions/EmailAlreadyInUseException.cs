namespace Matrix.Identity.Application.Exceptions
{
    public sealed class EmailAlreadyInUseException(string email) 
        : Exception($"User with email '{email}' already exists.")
    {
        public string Email { get; } = email;
    }
}
