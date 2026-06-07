using Amuse.Domain.Catalog;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Discovery;

public sealed class Playlist
{
    public const int MaxDescriptionLength = 100;
    public const string LikedCollectionTitle = "Liked";

    public PlaylistId Id { get; private set; }
    public ListenerProfileId OwnerListenerProfileId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PlaylistKind Kind { get; private set; }
    public PlaylistVisibility Visibility { get; private set; }
    public PlaylistId? ForkedFromPlaylistId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<PlaylistItem> _items = [];
    private readonly List<PlaylistShareGrant> _shareGrants = [];

    public IReadOnlyList<PlaylistItem> Items => _items;
    public IReadOnlyList<PlaylistShareGrant> ShareGrants => _shareGrants;

    public bool BecamePrivate { get; private set; }

    private Playlist()
    {
    }

    public static Result<Playlist> CreateOwned(
        ListenerProfileId ownerId,
        string title,
        PlaylistVisibility visibility,
        DateTimeOffset now)
    {
        var titleResult = PlaylistTitle.TryCreate(title);
        if (!titleResult.IsSuccess)
            return Result<Playlist>.Failure(titleResult.Error!);

        return Result<Playlist>.Success(new Playlist
        {
            Id = PlaylistId.New(),
            OwnerListenerProfileId = ownerId,
            Title = titleResult.Value!.Value,
            Kind = PlaylistKind.User,
            Visibility = visibility,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public static Result<Playlist> CreateLiked(ListenerProfileId ownerId, DateTimeOffset now)
    {
        var titleResult = PlaylistTitle.TryCreate(LikedCollectionTitle);
        if (!titleResult.IsSuccess)
            return Result<Playlist>.Failure(titleResult.Error!);

        return Result<Playlist>.Success(new Playlist
        {
            Id = PlaylistId.New(),
            OwnerListenerProfileId = ownerId,
            Title = titleResult.Value!.Value,
            Kind = PlaylistKind.Liked,
            Visibility = PlaylistVisibility.Private,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public static Playlist Rehydrate(
        PlaylistId id,
        ListenerProfileId ownerId,
        string title,
        string? description,
        PlaylistKind kind,
        PlaylistVisibility visibility,
        PlaylistId? forkedFromPlaylistId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IEnumerable<PlaylistItem> items,
        IEnumerable<PlaylistShareGrant> shareGrants)
    {
        var playlist = new Playlist
        {
            Id = id,
            OwnerListenerProfileId = ownerId,
            Title = title,
            Description = description,
            Kind = kind,
            Visibility = visibility,
            ForkedFromPlaylistId = forkedFromPlaylistId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
        playlist._items.AddRange(items);
        playlist._shareGrants.AddRange(shareGrants);
        return playlist;
    }

    public bool IsLikedCollection => Kind == PlaylistKind.Liked;

    public Result Rename(string title, DateTimeOffset now)
    {
        if (IsLikedCollection)
            return Result.Failure(DiscoveryErrors.CannotRenameLikedPlaylist);

        var titleResult = PlaylistTitle.TryCreate(title);
        if (!titleResult.IsSuccess)
            return Result.Failure(titleResult.Error!);

        Title = titleResult.Value!.Value;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result SetDescription(string? description, DateTimeOffset now)
    {
        if (IsLikedCollection)
            return Result.Failure(DiscoveryErrors.CannotRenameLikedPlaylist);

        if (description is not null)
        {
            var trimmed = description.Trim();
            if (trimmed.Length > MaxDescriptionLength)
                return Result.Failure(DiscoveryErrors.InvalidPlaylistDescription);

            Description = trimmed.Length is 0 ? null : trimmed;
        }
        else
        {
            Description = null;
        }

        UpdatedAt = now;
        return Result.Success();
    }

    public Result SetVisibility(PlaylistVisibility visibility, DateTimeOffset now)
    {
        if (Visibility == PlaylistVisibility.Public && visibility == PlaylistVisibility.Private)
            BecamePrivate = true;

        Visibility = visibility;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result GrantShare(string email, DateTimeOffset now)
    {
        if (Visibility != PlaylistVisibility.Private)
            return Result.Failure(DiscoveryErrors.ShareOnlyOnPrivatePlaylist);

        var emailResult = ShareGrantEmail.TryCreate(email);
        if (!emailResult.IsSuccess)
            return Result.Failure(emailResult.Error!);

        ShareGrantEmail shareEmail = emailResult.Value!;
        if (_shareGrants.Any(g => g.Email.Value == shareEmail.Value))
            return Result.Success();

        _shareGrants.Add(PlaylistShareGrant.Create(shareEmail, now));
        UpdatedAt = now;
        return Result.Success();
    }

    public Result RevokeShare(string email, DateTimeOffset now)
    {
        if (Visibility != PlaylistVisibility.Private)
            return Result.Failure(DiscoveryErrors.ShareOnlyOnPrivatePlaylist);

        var emailResult = ShareGrantEmail.TryCreate(email);
        if (!emailResult.IsSuccess)
            return Result.Failure(emailResult.Error!);

        ShareGrantEmail revokedEmail = emailResult.Value!;
        var removed = _shareGrants.RemoveAll(g => g.Email.Value == revokedEmail.Value);
        if (removed > 0)
            UpdatedAt = now;

        return Result.Success();
    }

    public Result ReplaceShares(IReadOnlyList<string> emails, DateTimeOffset now)
    {
        if (Visibility != PlaylistVisibility.Private)
            return Result.Failure(DiscoveryErrors.ShareOnlyOnPrivatePlaylist);

        var normalized = new List<ShareGrantEmail>();
        foreach (var email in emails)
        {
            var emailResult = ShareGrantEmail.TryCreate(email);
            if (!emailResult.IsSuccess)
                return Result.Failure(emailResult.Error!);

            ShareGrantEmail parsedEmail = emailResult.Value!;
            if (normalized.All(e => e.Value != parsedEmail.Value))
                normalized.Add(parsedEmail);
        }

        _shareGrants.Clear();
        foreach (var email in normalized)
            _shareGrants.Add(PlaylistShareGrant.Create(email, now));

        UpdatedAt = now;
        return Result.Success();
    }

    public Result<PlaylistItem> AddTrack(TrackId trackId, DateTimeOffset now)
    {
        if (_items.Any(i => i.TrackId == trackId))
            return Result<PlaylistItem>.Failure(DiscoveryErrors.PlaylistTrackDuplicate);

        var item = PlaylistItem.Create(trackId, _items.Count + 1, now);
        _items.Add(item);
        UpdatedAt = now;
        return Result<PlaylistItem>.Success(item);
    }

    public Result RemoveTrack(PlaylistItemId itemId, DateTimeOffset now)
    {
        var index = _items.FindIndex(i => i.Id == itemId);
        if (index < 0)
            return Result.Failure(DiscoveryErrors.PlaylistItemNotFound);

        _items.RemoveAt(index);
        CompactPositions();
        UpdatedAt = now;
        return Result.Success();
    }

    public Result RemoveTrackByTrackId(TrackId trackId, DateTimeOffset now)
    {
        var item = _items.FirstOrDefault(i => i.TrackId == trackId);
        if (item is null)
            return Result.Failure(DiscoveryErrors.LikedTrackNotFound);

        return RemoveTrack(item.Id, now);
    }

    public Result Reorder(PlaylistItemId itemId, int newPosition, DateTimeOffset now)
    {
        if (newPosition < 1 || newPosition > _items.Count)
            return Result.Failure(DiscoveryErrors.InvalidPlaylistPosition);

        var index = _items.FindIndex(i => i.Id == itemId);
        if (index < 0)
            return Result.Failure(DiscoveryErrors.PlaylistItemNotFound);

        var item = _items[index];
        _items.RemoveAt(index);
        _items.Insert(newPosition - 1, item);
        CompactPositions();
        UpdatedAt = now;
        return Result.Success();
    }

    public bool CanBeViewedBy(PlaylistViewContext context)
    {
        if (Visibility == PlaylistVisibility.Public)
            return true;

        if (context.ViewerProfileId is not null && context.ViewerProfileId == OwnerListenerProfileId)
            return true;

        if (!string.IsNullOrEmpty(context.ViewerEmailNormalized)
            && _shareGrants.Any(g => g.Email.Value == context.ViewerEmailNormalized))
        {
            return true;
        }

        return false;
    }

    public Result<Playlist> ForkFor(ListenerProfileId listenerId, PlaylistViewContext context, DateTimeOffset now)
    {
        if (IsLikedCollection)
            return Result<Playlist>.Failure(DiscoveryErrors.CannotForkLikedPlaylist);

        if (!CanBeViewedBy(context))
            return Result<Playlist>.Failure(DiscoveryErrors.CannotForkPrivatePlaylist);

        var forkResult = CreateOwned(listenerId, Title, PlaylistVisibility.Private, now);
        if (!forkResult.IsSuccess)
            return forkResult;

        var fork = forkResult.Value!;
        fork.ForkedFromPlaylistId = Id;
        if (Description is not null)
            fork.SetDescription(Description, now);

        foreach (var item in _items.OrderBy(i => i.Position))
        {
            var addResult = fork.AddTrack(item.TrackId, now);
            if (!addResult.IsSuccess)
                return Result<Playlist>.Failure(addResult.Error!);
        }

        return Result<Playlist>.Success(fork);
    }

    public void CutForkOrigin(DateTimeOffset now)
    {
        if (ForkedFromPlaylistId is null)
            return;

        ForkedFromPlaylistId = null;
        UpdatedAt = now;
    }

    private void CompactPositions()
    {
        for (var i = 0; i < _items.Count; i++)
            _items[i].SetPosition(i + 1);
    }
}
