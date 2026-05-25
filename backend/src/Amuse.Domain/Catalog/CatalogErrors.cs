using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Catalog;

public static class CatalogErrors
{
    public static readonly DomainError ArtistNotFound =
        new("catalog.artist_not_found", "Artist was not found.");

    public static readonly DomainError AlbumNotFound =
        new("catalog.album_not_found", "Album was not found.");

    public static readonly DomainError InvalidSlug =
        new("catalog.invalid_slug", "Slug must be lowercase alphanumerics with single hyphens.");

    public static readonly DomainError InvalidArtist =
        new("catalog.invalid_artist", "Artist data is invalid.");

    public static readonly DomainError InvalidAlbum =
        new("catalog.invalid_album", "Album data is invalid.");

    public static readonly DomainError InvalidTrack =
        new("catalog.invalid_track", "Track data is invalid.");
}
