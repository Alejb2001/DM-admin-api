namespace DmAdminApi.Infrastructure.Data.Entities;

public class SceneTemplate
{
    public Guid Id { get; set; }
    public Guid SceneId { get; set; }
    /// <summary>circle | cone | ray</summary>
    public string Type { get; set; } = "circle";
    public double X { get; set; }
    public double Y { get; set; }
    public double Rotation { get; set; }
    public string Color { get; set; } = "#f44336";
    /// <summary>Radius or length in grid cells</summary>
    public double SizeInCells { get; set; } = 3;
    public string? Label { get; set; }

    public SessionScene Scene { get; set; } = null!;
}
