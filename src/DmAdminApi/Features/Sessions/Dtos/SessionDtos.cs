namespace DmAdminApi.Features.Sessions.Dtos;

// ── Requests ──────────────────────────────────────────────────────────────────
public record CreateSessionDto(string Name);
public record SendMessageDto(string Type, string Content, bool IsSecret);

// ── Responses ─────────────────────────────────────────────────────────────────
public record GameSessionDto(
    Guid Id,
    string Name,
    string CreatedBy,
    Guid CreatedById,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string Status);

public record GameSessionDetailDto(
    GameSessionDto Session,
    List<ChatMessageDto> RecentMessages);

public record ChatMessageDto(
    Guid Id,
    Guid SessionId,
    Guid UserId,
    string UserDisplayName,
    string Type,
    string Content,
    DiceResultDto? DiceResult,
    bool IsSecret,
    DateTimeOffset CreatedAt);

public record DiceResultDto(
    string Formula,
    string? Label,
    int Modifier,
    string? Advantage,
    List<int> Rolls,
    List<int> Kept,
    int Total);
