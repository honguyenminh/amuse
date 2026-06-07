using Amuse.Modules.Discovery.Features.Common;
using FluentValidation;

namespace Amuse.Modules.Discovery.Features.ReplacePlaylistShares;

internal sealed class ReplacePlaylistSharesRequestValidator : AbstractValidator<ReplacePlaylistSharesRequest>
{
    public ReplacePlaylistSharesRequestValidator()
    {
        RuleFor(x => x.Emails)
            .NotNull();
    }
}
