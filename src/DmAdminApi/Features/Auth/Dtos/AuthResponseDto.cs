namespace DmAdminApi.Features.Auth.Dtos;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    UserDto User
);

public record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string SubscriptionTier
);
