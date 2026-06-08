using Amuse.Domain.Discovery;
using Amuse.Modules.Discovery.Features.Common;
using FluentValidation;

namespace Amuse.Modules.Discovery.Features.ForkPlaylist;

internal sealed class ForkPlaylistRequestValidator : AbstractValidator<ForkPlaylistRequest>
{
    public ForkPlaylistRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(PlaylistTitle.MaxLength);
    }
}
