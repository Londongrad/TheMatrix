namespace Matrix.Identity.Infrastructure.Integration.Email
{
    public sealed class EmailOptions
    {
        public const string SectionName = "Email";

        public EmailDeliveryMode DeliveryMode { get; init; } = EmailDeliveryMode.LogOnly;

        public string FromEmail { get; init; } = string.Empty;
        public string FromName { get; init; } = "The Matrix";

        public string SmtpHost { get; init; } = string.Empty;
        public int SmtpPort { get; init; } = 587;
        public string SmtpUsername { get; init; } = string.Empty;
        public string SmtpPassword { get; init; } = string.Empty;
        public bool UseSsl { get; init; } = true;
    }
}
