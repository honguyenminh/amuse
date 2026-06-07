using Amuse.Domain.Discovery;
using Amuse.Modules.Discovery.Features.Common;
using FluentValidation;

namespace Amuse.Modules.Discovery.Features.UpdatePlaylist;

internal sealed class UpdatePlaylistRequestValidator : AbstractValidator<UpdatePlaylistRequest>
{
    public UpdatePlaylistRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(PlaylistTitle.MaxLength)
            .When(x => x.Title is not null);

        RuleFor(x => x.Description)
            .MaximumLength(Playlist.MaxDescriptionLength)
            .When(x => x.Description is not null);

        RuleFor(x => x.Visibility)
            .Must(v => v is null or "private" or "public")
            .WithMessage("Visibility must be 'private' or 'public'.");

        RuleFor(x => x)
            .Must(x => x.Title is not null || x.Description is not null || x.Visibility is not null)
            .WithMessage("At least one playlist field must be provided.");
    }
}
