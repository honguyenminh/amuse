using Amuse.Modules.Discovery.Features.Shared;
using FluentValidation;

namespace Amuse.Modules.Discovery.Features.AddTrackToPlaylist;

internal sealed class AddTrackToPlaylistRequestValidator : AbstractValidator<AddPlaylistItemRequest>
{
    public AddTrackToPlaylistRequestValidator()
    {
        RuleFor(x => x.TrackId)
            .NotEmpty();
    }
}
