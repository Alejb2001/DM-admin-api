namespace DmAdminApi.Infrastructure.Data.Entities;

public class MapToken
{
    public Guid Id { get; set; }
    public Guid SceneId { get; set; }
    public Guid? EntityId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Color { get; set; } = "#7E57C2";
    public double X { get; set; }
    public double Y { get; set; }
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
    public bool IsVisible { get; set; } = true;
    public Guid? ControlledBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public SessionScene Scene { get; set; } = null!;
    public WorldEntity? Entity { get; set; }
    public User? Controller { get; set; }
    public ICollection<TokenCondition> Conditions { get; set; } = [];
}
