namespace DmAdminApi.Infrastructure.Email;

public interface IEmailService
{
    Task SendCampaignInvitationAsync(string toEmail, string toName, string campaignName, string invitationUrl);
    Task SendWelcomeAsync(string toEmail, string displayName);
}
