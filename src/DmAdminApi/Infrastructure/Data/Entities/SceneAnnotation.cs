namespace DmAdminApi.Infrastructure.Data.Entities;

public class SceneAnnotation
{
    public Guid Id { get; set; }
    public Guid SceneId { get; set; }
    /// <summary>pen | line | arrow | rect | circle | text</summary>
    public string Type { get; set; } = "pen";
    /// <summary>JSON-serialized flat array of doubles: [x1,y1,x2,y2,...]</summary>
    public string Points { get; set; } = "[]";
    public string Color { get; set; } = "#ef5350";
    public int StrokeWidth { get; set; } = 2;
    public string? FillColor { get; set; }
    public double FillOpacity { get; set; } = 0;
    public string? Text { get; set; }
    public int FontSize { get; set; } = 16;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public SessionScene Scene { get; set; } = null!;
    public User Creator { get; set; } = null!;
}
