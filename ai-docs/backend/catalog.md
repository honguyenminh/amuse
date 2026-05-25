# Catalog module

The Catalog bounded context owns artists, albums, and tracks. It exposes a read-only HTTP surface used by the listener consumer app to populate the home feed and detail pages.

## Domain model

| Aggregate | Children | Identifying fields |
|-----------|----------|--------------------|
| `Artist` | – | `id` (Guid v7), unique `slug` |
| `Album`  | `Track[]` | `id`, unique (`artist_id`, `slug`), `release_type` (PG enum) |
| `Track`  | child of `Album` | `id`, unique (`album_id`, `track_number`) |

Value objects (`record struct` or `partial record struct`):

- `ArtistId`, `AlbumId`, `TrackId` — V7 GUID wrappers with `From(Guid)` factories.
- `Slug` — lowercase `[a-z0-9](-[a-z0-9]+)*`, max 96.
- `TrackDuration` — positive int milliseconds.
- `ReleaseType` — `single | ep | album | compilation` mapped to Postgres enum `catalog.release_type` via `npgsql.MapEnum<ReleaseType>("release_type", "catalog")` + `modelBuilder.HasPostgresEnum<ReleaseType>(schema: "catalog", name: "release_type")`.

Errors live in `Amuse.Domain.Catalog.CatalogErrors`:

- `catalog.artist_not_found`
- `catalog.album_not_found`
- `catalog.invalid_slug`, `catalog.invalid_artist`, `catalog.invalid_album`, `catalog.invalid_track`

## Endpoints

All require `Authorization: Bearer <jwt>`.

| Method | Path | Handler | Notes |
|--------|------|---------|-------|
| `GET` | `/api/v1/catalog/home` | `BrowseHomeHandler` | Returns 8 most-recent albums + 6 featured artists. |
| `GET` | `/api/v1/catalog/artists/{artistId:guid}` | `GetArtistDetailHandler` | Returns artist + discography. `400 catalog.artist_not_found` if missing. |
| `GET` | `/api/v1/catalog/albums/{albumId:guid}` | `GetAlbumDetailHandler` | Returns album + ordered tracks. `400 catalog.album_not_found` if missing. |

Response DTOs are defined in `Catalog/Features/Shared/CatalogDtos.cs` and per-feature handler files. Track durations are returned as `durationMs` (int milliseconds).

## Persistence

- Schema: `catalog`
- Migration: `Catalog/Persistence/Migrations/20260525133857_InitialCatalog.cs` creates `catalog.release_type` enum and the `artist`, `album`, `track` tables. FK from track to album is `ON DELETE CASCADE`.
- `Album.Tracks` is a private list exposed as `IReadOnlyList<Track>`. EF Core access mode is configured to `Field` in `AlbumConfiguration`.

## Dev seed

`CatalogDevSeeding.SeedAsync` is invoked once at API startup **only when `app.Environment.IsDevelopment()`**. It is idempotent: returns early if any artist already exists. The fixture (`AmuseApiFixture`) calls the same method for integration tests so endpoint tests can rely on a populated catalog.

Sample data: 3 artists (Aurora Lights, Iron Palms, Velvet Monsoon), 5 albums spanning every `ReleaseType` value, with placeholder cover art URLs from `picsum.photos`.

## Frontend wiring

`frontend/consumer/src/lib/api/catalogClient.ts` exposes `browseCatalogHome()`, `getCatalogArtist(id)`, `getCatalogAlbum(id)`, all routed through `authFetch` for transparent refresh.

The album page uses `useCoverArtSeed(coverArtUrl)` (see `frontend/consumer/src/theme/useCoverArtSeed.ts`) to:

1. Resolve a deterministic hash seed immediately (fast first paint).
2. Asynchronously load the image with `crossOrigin = "anonymous"`, sample it on a 16×16 canvas, convert to OKLCH, and update the page seed.

That seed is fed into the theme system through `usePageSeed`, which keeps the existing precedence order intact: page seed > playing seed > default.

## Adding new catalog features

1. Domain changes in `backend/src/Amuse.Domain/Catalog`. Value objects must remain `record struct` or sealed with private constructors.
2. New endpoint in `backend/src/Amuse.Modules/Catalog/Features/<Feature>/` (handler + endpoint + DTOs).
3. Register handler in `CatalogModule.AddCatalogModule`. Map endpoint in `CatalogModule.MapCatalogModule`.
4. Add OpenAPI metadata on the endpoint via `.WithSummary`, `.Produces<T>()`, and `.ProducesProblem(StatusCodes.Status400BadRequest)` referencing the relevant `CatalogErrors.*` code in the summary.
5. Update the integration tests fixture or add a new `Catalog*Tests` file under `tests/Amuse.Api.IntegrationTests`.
