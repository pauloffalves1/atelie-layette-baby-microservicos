namespace AtelieBebe.Catalog.Core.Application.SiteImages;

public sealed record SiteImageDto(Guid Id, string Key, string Url, int SortOrder, DateTime UpdatedAt);

public enum MoveDirection { Up, Down }
