namespace DmAdminApi.Infrastructure.Data.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = null!; // "chat" | "narration" | "roll" | "secret_roll" | "system"
    public string Content { get; set; } = null!;
    public string? DiceResultJson { get; set; } // stored as jsonb column "dice_result"
    public bool IsSecret { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public GameSession Session { get; set; } = null!;
    public User User { get; set; } = null!;
}
