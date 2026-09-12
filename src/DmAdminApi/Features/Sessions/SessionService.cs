using DmAdminApi.Features.Sessions.Dtos;
using DmAdminApi.Infrastructure.Data;
using DmAdminApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DmAdminApi.Features.Sessions;

public class SessionService(AppDbContext db)
{
    public async Task<GameSessionDto> CreateAsync(Guid campaignId, Guid userId, CreateSessionDto dto)
    {
        var hasActive = await db.GameSessions.AnyAsync(s => s.CampaignId == campaignId && s.Status == "active");
        if (hasActive)
            throw new InvalidOperationException("Ya hay una sesión activa en esta campaña.");

        var session = new GameSession
        {
            CampaignId = campaignId,
            Name = dto.Name,
            CreatedById = userId,
            StartedAt = DateTimeOffset.UtcNow,
            Status = "active",
        };
        db.GameSessions.Add(session);
        await db.SaveChangesAsync();

        var creator = await db.Users.FindAsync(userId);
        return ToDto(session, creator?.DisplayName ?? "DM");
    }

    public async Task EndAsync(Guid sessionId, Guid campaignId)
    {
        var session = await db.GameSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.CampaignId == campaignId && s.Status == "active")
            ?? throw new KeyNotFoundException("Sesión activa no encontrada.");

        session.Status = "ended";
        session.EndedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<List<GameSessionDto>> GetAllAsync(Guid campaignId)
    {
        var sessions = await db.GameSessions
            .Where(s => s.CampaignId == campaignId)
            .Include(s => s.CreatedBy)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();

        return sessions.Select(s => ToDto(s, s.CreatedBy.DisplayName)).ToList();
    }

    public async Task<GameSessionDetailDto> GetDetailAsync(Guid sessionId)
    {
        var session = await db.GameSessions
            .Include(s => s.CreatedBy)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new KeyNotFoundException("Sesión no encontrada.");

        var messages = await db.ChatMessages
            .Where(m => m.SessionId == sessionId && !m.IsSecret)
            .Include(m => m.User)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        var recentDtos = messages.TakeLast(100).Select(ToMessageDto).ToList();

        return new GameSessionDetailDto(ToDto(session, session.CreatedBy.DisplayName), recentDtos);
    }

    public async Task<List<ChatMessageDto>> GetMessagesPagedAsync(Guid sessionId, int page, int pageSize)
    {
        var messages = await db.ChatMessages
            .Where(m => m.SessionId == sessionId && !m.IsSecret)
            .Include(m => m.User)
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return messages.Select(ToMessageDto).ToList();
    }

    private static GameSessionDto ToDto(GameSession s, string createdBy) =>
        new(s.Id, s.Name, createdBy, s.CreatedById, s.StartedAt, s.EndedAt, s.Status);

    public static ChatMessageDto ToMessageDto(ChatMessage m) =>
        new(m.Id, m.SessionId, m.UserId, m.User.DisplayName,
            m.Type, m.Content,
            ChatService.DeserializeDiceResult(m.DiceResultJson),
            m.IsSecret, m.CreatedAt);
}
