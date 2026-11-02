namespace DmAdminApi.Features.Jukebox.Dtos;

public record JukeboxTrackDto(Guid Id, string Name, string Url, int SortOrder);
public record CreateTrackDto(string Name, string Url);
public record UpdateTrackDto(string Name, string Url);
public record PlayTrackDto(string Url, string Name, bool Loop);
