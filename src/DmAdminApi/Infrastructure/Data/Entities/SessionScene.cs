namespace DmAdminApi.Infrastructure.Data.Entities;

public class SessionScene
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BackgroundUrl { get; set; }
    public int GridSize { get; set; } = 50;
    public bool GridEnabled { get; set; } = true;
    public bool IsActive { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }

    public GameSession Session { get; set; } = null!;
    public ICollection<MapToken> Tokens { get; set; } = [];
}
