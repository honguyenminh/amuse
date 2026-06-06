using Amuse.Domain.Discovery;
using Amuse.Modules.Discovery.Features.Shared;
using FluentValidation;

namespace Amuse.Modules.Discovery.Features.CreatePlaylist;

internal sealed class CreatePlaylistRequestValidator : AbstractValidator<CreatePlaylistRequest>
{
    public CreatePlaylistRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(PlaylistTitle.MaxLength);

        RuleFor(x => x.Visibility)
            .NotEmpty()
            .Must(v => v is "private" or "public")
            .WithMessage("Visibility must be 'private' or 'public'.");

        RuleFor(x => x.Description)
            .MaximumLength(Playlist.MaxDescriptionLength)
            .When(x => x.Description is not null);
    }
}
