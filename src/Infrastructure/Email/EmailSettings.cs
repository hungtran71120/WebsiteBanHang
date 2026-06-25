namespace ShopeeClone.Infrastructure.Email;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "ShopeeClone";
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
