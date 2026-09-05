namespace DmAdminApi.Infrastructure.Email;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public string FromAddress { get; set; } = "noreply@dmadmin.app";
    public string FromName { get; set; } = "DM Admin";
    public string AppBaseUrl { get; set; } = "http://localhost:4200";
}
