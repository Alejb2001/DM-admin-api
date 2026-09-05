using DmAdminApi.Features.Auth.Dtos;
using DmAdminApi.Infrastructure.Auth;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using DmAdminApi.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Auth;

public class AuthService(AppDbContext db, JwtService jwt, IEmailService email)
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await db.Users.AnyAsync(u => u.Email == dto.Email.ToLower()))
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            Email = dto.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
            DisplayName = dto.DisplayName,
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        _ = email.SendWelcomeAsync(user.Email, user.DisplayName);

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower())
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponseDto> RefreshAsync(string rawToken)
    {
        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == rawToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired or revoked.");

        // Rotate: revoke the old token
        stored.IsRevoked = true;
        await db.SaveChangesAsync();

        return await IssueTokensAsync(stored.User);
    }

    public async Task LogoutAsync(Guid userId)
    {
        await db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.IsRevoked, true));
    }

    public async Task<UserDto> GetMeAsync(Guid userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        return ToUserDto(user);
    }

    // --- private helpers ---

    private async Task<AuthResponseDto> IssueTokensAsync(User user)
    {
        var accessToken = jwt.GenerateAccessToken(user);
        var rawRefreshToken = jwt.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = rawRefreshToken,
            ExpiresAt = jwt.RefreshTokenExpiry(),
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();

        return new AuthResponseDto(accessToken, rawRefreshToken, ToUserDto(user));
    }

    private static UserDto ToUserDto(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.AvatarUrl, user.SubscriptionTier);
}
