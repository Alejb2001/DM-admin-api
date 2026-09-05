using System.ComponentModel.DataAnnotations;

namespace DmAdminApi.Features.Campaigns.Dtos;

public record CreateCampaignDto(
    [Required, MaxLength(200)] string Name,
    string? Description
);

public record UpdateCampaignDto(
    [Required, MaxLength(200)] string Name,
    string? Description
);

public record CampaignDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    DateTime CreatedAt,
    string UserRole   // "owner" | role name
);

public record CampaignDetailDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    DateTime CreatedAt,
    List<MemberDto> Members,
    List<RoleDto> Roles
);

public record MemberDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    Guid RoleId,
    string RoleName,
    DateTime JoinedAt
);

public record RoleDto(
    Guid Id,
    string Name,
    bool IsSystemDefault
);

public record CreateInvitationDto(
    [Required] Guid RoleId,
    int ExpiryHours = 168,  // 7 days
    [EmailAddress] string? Email = null,
    string? RecipientName = null
);

public record InvitationDto(
    Guid Id,
    string Token,
    string RoleName,
    DateTime ExpiresAt
);

public record JoinCampaignDto(
    [Required] string Token
);

public record UpdateMemberRoleDto(
    [Required] Guid RoleId
);
