using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace DmAdminApi.Infrastructure.Email;

public class SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendCampaignInvitationAsync(string toEmail, string toName, string campaignName, string invitationUrl)
    {
        var subject = $"Te han invitado a la campaña \"{campaignName}\" en DM Admin";
        var body = $"""
            <div style="font-family:sans-serif;max-width:520px;margin:0 auto">
              <h2 style="color:#3F51B5">Invitación a campaña</h2>
              <p>Hola {toName},</p>
              <p>Has sido invitado a unirte a la campaña <strong>{campaignName}</strong> en DM Admin.</p>
              <p style="text-align:center;margin:32px 0">
                <a href="{invitationUrl}"
                   style="background:#3F51B5;color:#fff;padding:12px 28px;border-radius:6px;text-decoration:none;font-weight:bold">
                  Unirse a la campaña
                </a>
              </p>
              <p style="color:#757575;font-size:13px">Si no esperabas esta invitación, puedes ignorar este mensaje.</p>
            </div>
            """;

        await SendAsync(toEmail, toName, subject, body);
    }

    public async Task SendWelcomeAsync(string toEmail, string displayName)
    {
        var subject = "¡Bienvenido a DM Admin!";
        var body = $"""
            <div style="font-family:sans-serif;max-width:520px;margin:0 auto">
              <h2 style="color:#3F51B5">¡Hola, {displayName}!</h2>
              <p>Gracias por registrarte en DM Admin, la plataforma para directores de juego.</p>
              <p>Ya puedes comenzar a crear tus campañas, construir tu mundo y gestionar tus jugadores.</p>
              <p style="text-align:center;margin:32px 0">
                <a href="{_settings.AppBaseUrl}/campaigns"
                   style="background:#3F51B5;color:#fff;padding:12px 28px;border-radius:6px;text-decoration:none;font-weight:bold">
                  Ir a mis campañas
                </a>
              </p>
            </div>
            """;

        await SendAsync(toEmail, displayName, subject, body);
    }

    public async Task SendEmailVerificationAsync(string toEmail, string displayName, string verificationUrl)
    {
        var subject = "Verifica tu correo en DM Admin";
        var body = $"""
            <div style="font-family:sans-serif;max-width:520px;margin:0 auto;color:#1a1a1a">
              <div style="background:#3F51B5;padding:24px 32px;border-radius:8px 8px 0 0">
                <h1 style="color:#fff;margin:0;font-size:22px">DM Admin</h1>
              </div>
              <div style="background:#fff;border:1px solid #e0e0e0;border-top:none;padding:32px;border-radius:0 0 8px 8px">
                <h2 style="margin-top:0;color:#3F51B5">Verifica tu correo electrónico</h2>
                <p>Hola <strong>{displayName}</strong>,</p>
                <p>Gracias por registrarte. Para activar tu cuenta haz clic en el botón de abajo. El enlace es válido por <strong>24 horas</strong>.</p>
                <p style="text-align:center;margin:32px 0">
                  <a href="{verificationUrl}"
                     style="background:#3F51B5;color:#fff;padding:14px 32px;border-radius:6px;text-decoration:none;font-weight:bold;font-size:15px;display:inline-block">
                    Verificar mi cuenta
                  </a>
                </p>
                <p style="color:#757575;font-size:13px">Si no creaste esta cuenta puedes ignorar este mensaje. El enlace expirará automáticamente.</p>
                <hr style="border:none;border-top:1px solid #e0e0e0;margin:24px 0"/>
                <p style="color:#9e9e9e;font-size:12px;margin:0">
                  Si el botón no funciona, copia y pega este enlace en tu navegador:<br/>
                  <a href="{verificationUrl}" style="color:#3F51B5;word-break:break-all">{verificationUrl}</a>
                </p>
              </div>
            </div>
            """;

        await SendAsync(toEmail, displayName, subject, body);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(_settings.SmtpHost))
        {
            logger.LogWarning("Email not sent (SMTP not configured): {Subject} to {Email}", subject, toEmail);
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.UseSsl,
                Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword),
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };

            message.To.Add(new MailAddress(toEmail, toName));
            await client.SendMailAsync(message);
            logger.LogInformation("Email sent: {Subject} to {Email}", subject, toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email: {Subject} to {Email}", subject, toEmail);
        }
    }
}
