using DmAdminApi.Features.Campaigns.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using DmAdminApi.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DmAdminApi.Features.Campaigns;

public class CampaignService(AppDbContext db, IEmailService email, IOptions<EmailSettings> emailSettings)
{
    public async Task<List<CampaignDto>> GetUserCampaignsAsync(Guid userId)
    {
        var owned = await db.Campaigns
            .Where(c => c.OwnerId == userId)
            .Select(c => new CampaignDto(c.Id, c.Name, c.Description, c.OwnerId, c.CreatedAt, "owner"))
            .ToListAsync();

        var joined = await db.CampaignMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Campaign)
            .Include(m => m.Role)
            .Select(m => new CampaignDto(
                m.Campaign.Id, m.Campaign.Name, m.Campaign.Description,
                m.Campaign.OwnerId, m.Campaign.CreatedAt, m.Role.Name))
            .ToListAsync();

        return [.. owned, .. joined];
    }

    public async Task<CampaignDetailDto> GetCampaignDetailAsync(Guid campaignId)
    {
        var campaign = await db.Campaigns
            .Include(c => c.Roles)
            .Include(c => c.Members).ThenInclude(m => m.User)
            .Include(c => c.Members).ThenInclude(m => m.Role)
            .FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new KeyNotFoundException("Campaign not found.");

        return new CampaignDetailDto(
            campaign.Id, campaign.Name, campaign.Description, campaign.OwnerId, campaign.CreatedAt,
            campaign.Members.Select(m => new MemberDto(
                m.UserId, m.User.DisplayName, m.User.AvatarUrl,
                m.RoleId, m.Role.Name, m.JoinedAt)).ToList(),
            campaign.Roles.Select(r => new RoleDto(r.Id, r.Name, r.IsSystemDefault)).ToList(),
            campaign.JoinCode
        );
    }

    public async Task<CampaignDto> CreateAsync(CreateCampaignDto dto, Guid ownerId)
    {
        var campaign = new Campaign
        {
            OwnerId = ownerId,
            Name = dto.Name,
            Description = dto.Description,
            JoinCode = await GenerateUniqueJoinCodeAsync(),
            CreatedAt = DateTime.UtcNow,
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        // Seed the 3 system roles
        var roles = new[]
        {
            new CampaignRole { CampaignId = campaign.Id, Name = SystemRoles.CoDm,      IsSystemDefault = true },
            new CampaignRole { CampaignId = campaign.Id, Name = SystemRoles.Player,    IsSystemDefault = true },
            new CampaignRole { CampaignId = campaign.Id, Name = SystemRoles.Spectator, IsSystemDefault = true },
        };
        db.CampaignRoles.AddRange(roles);
        await db.SaveChangesAsync();

        return new CampaignDto(campaign.Id, campaign.Name, campaign.Description, campaign.OwnerId, campaign.CreatedAt, "owner");
    }

    public async Task<CampaignDto> UpdateAsync(Guid campaignId, UpdateCampaignDto dto)
    {
        var campaign = await db.Campaigns.FindAsync(campaignId)
            ?? throw new KeyNotFoundException("Campaign not found.");

        campaign.Name = dto.Name;
        campaign.Description = dto.Description;
        await db.SaveChangesAsync();

        return new CampaignDto(campaign.Id, campaign.Name, campaign.Description, campaign.OwnerId, campaign.CreatedAt, "owner");
    }

    public async Task DeleteAsync(Guid campaignId)
    {
        var campaign = await db.Campaigns.FindAsync(campaignId)
            ?? throw new KeyNotFoundException("Campaign not found.");

        db.Campaigns.Remove(campaign);
        await db.SaveChangesAsync();
    }

    public async Task<InvitationDto> CreateInvitationAsync(Guid campaignId, CreateInvitationDto dto)
    {
        var role = await db.CampaignRoles.FindAsync(dto.RoleId)
            ?? throw new KeyNotFoundException("Role not found.");

        var campaign = await db.Campaigns.FindAsync(campaignId)
            ?? throw new KeyNotFoundException("Campaign not found.");

        var invitation = new CampaignInvitation
        {
            CampaignId = campaignId,
            RoleId = dto.RoleId,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddHours(dto.ExpiryHours),
            CreatedAt = DateTime.UtcNow,
        };
        db.CampaignInvitations.Add(invitation);
        await db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(dto.Email))
        {
            var appBase = emailSettings.Value.AppBaseUrl.TrimEnd('/');
            var inviteUrl = $"{appBase}/join?token={invitation.Token}";
            _ = email.SendCampaignInvitationAsync(dto.Email, dto.RecipientName ?? dto.Email, campaign.Name, inviteUrl);
        }

        return new InvitationDto(invitation.Id, invitation.Token, role.Name, invitation.ExpiresAt);
    }

    public async Task<CampaignDto> JoinCampaignAsync(string token, Guid userId)
    {
        var invitation = await db.CampaignInvitations
            .Include(i => i.Campaign)
            .FirstOrDefaultAsync(i => i.Token == token)
            ?? throw new InvalidOperationException("Invalid or expired invitation.");

        if (invitation.IsUsed || invitation.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Invitation has expired or already been used.");

        var alreadyMember = await db.CampaignMembers.AnyAsync(
            m => m.CampaignId == invitation.CampaignId && m.UserId == userId);
        if (alreadyMember)
            throw new InvalidOperationException("Already a member of this campaign.");

        var member = new CampaignMember
        {
            CampaignId = invitation.CampaignId,
            UserId = userId,
            RoleId = invitation.RoleId,
            JoinedAt = DateTime.UtcNow,
        };
        db.CampaignMembers.Add(member);
        invitation.IsUsed = true;
        await db.SaveChangesAsync();

        var c = invitation.Campaign;
        return new CampaignDto(c.Id, c.Name, c.Description, c.OwnerId, c.CreatedAt, "Player");
    }

    public async Task UpdateMemberRoleAsync(Guid campaignId, Guid memberId, UpdateMemberRoleDto dto)
    {
        var member = await db.CampaignMembers
            .FirstOrDefaultAsync(m => m.CampaignId == campaignId && m.UserId == memberId)
            ?? throw new KeyNotFoundException("Member not found.");

        var role = await db.CampaignRoles
            .FirstOrDefaultAsync(r => r.Id == dto.RoleId && r.CampaignId == campaignId)
            ?? throw new KeyNotFoundException("Role not found in this campaign.");

        member.RoleId = dto.RoleId;
        await db.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Guid campaignId, Guid memberId)
    {
        var member = await db.CampaignMembers
            .FirstOrDefaultAsync(m => m.CampaignId == campaignId && m.UserId == memberId)
            ?? throw new KeyNotFoundException("Member not found.");

        db.CampaignMembers.Remove(member);
        await db.SaveChangesAsync();
    }

    public async Task LeaveAsync(Guid campaignId, Guid userId)
    {
        var member = await db.CampaignMembers
            .FirstOrDefaultAsync(m => m.CampaignId == campaignId && m.UserId == userId)
            ?? throw new KeyNotFoundException("Not a member of this campaign.");

        db.CampaignMembers.Remove(member);
        await db.SaveChangesAsync();
    }

    public async Task<CampaignDto> JoinByCodeAsync(string code, Guid userId)
    {
        var campaign = await db.Campaigns
            .Include(c => c.Roles)
            .FirstOrDefaultAsync(c => c.JoinCode == code.ToUpper())
            ?? throw new KeyNotFoundException("Código de campaña no encontrado.");

        if (campaign.OwnerId == userId)
            throw new InvalidOperationException("Eres el director de esta campaña.");

        var alreadyMember = await db.CampaignMembers
            .AnyAsync(m => m.CampaignId == campaign.Id && m.UserId == userId);
        if (alreadyMember)
            throw new InvalidOperationException("Ya eres miembro de esta campaña.");

        var playerRole = campaign.Roles.FirstOrDefault(r => r.Name == SystemRoles.Player)
            ?? campaign.Roles.First();

        db.CampaignMembers.Add(new CampaignMember
        {
            CampaignId = campaign.Id,
            UserId = userId,
            RoleId = playerRole.Id,
            JoinedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return new CampaignDto(campaign.Id, campaign.Name, campaign.Description,
            campaign.OwnerId, campaign.CreatedAt, playerRole.Name);
    }

    public async Task<string> RegenerateCodeAsync(Guid campaignId)
    {
        var campaign = await db.Campaigns.FindAsync(campaignId)
            ?? throw new KeyNotFoundException("Campaign not found.");

        campaign.JoinCode = await GenerateUniqueJoinCodeAsync();
        await db.SaveChangesAsync();
        return campaign.JoinCode;
    }

    public async Task<CampaignPreviewDto?> GetPreviewByCodeAsync(string code)
    {
        var campaign = await db.Campaigns
            .FirstOrDefaultAsync(c => c.JoinCode == code.ToUpper());
        return campaign is null ? null : new CampaignPreviewDto(campaign.Name);
    }

    private async Task<string> GenerateUniqueJoinCodeAsync()
    {
        string code;
        do { code = Guid.NewGuid().ToString("N")[..8].ToUpper(); }
        while (await db.Campaigns.AnyAsync(c => c.JoinCode == code));
        return code;
    }
}
