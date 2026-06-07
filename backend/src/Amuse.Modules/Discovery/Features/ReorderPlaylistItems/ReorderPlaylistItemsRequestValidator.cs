using Amuse.Modules.Discovery.Features.Common;
using FluentValidation;

namespace Amuse.Modules.Discovery.Features.ReorderPlaylistItems;

internal sealed class ReorderPlaylistItemsRequestValidator : AbstractValidator<ReorderPlaylistItemsRequest>
{
    public ReorderPlaylistItemsRequestValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty();

        RuleFor(x => x.NewPosition)
            .GreaterThan(0);
    }
}
