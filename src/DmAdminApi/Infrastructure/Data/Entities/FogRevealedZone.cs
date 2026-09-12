namespace DmAdminApi.Infrastructure.Data.Entities;

public class FogRevealedZone
{
    public Guid Id { get; set; }
    public Guid SceneId { get; set; }
    public string Shape { get; set; } = "rect"; // "rect" | "circle"
    public float X { get; set; }
    public float Y { get; set; }
    public float? Width { get; set; }
    public float? Height { get; set; }
    public float? Radius { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public SessionScene Scene { get; set; } = null!;
}
