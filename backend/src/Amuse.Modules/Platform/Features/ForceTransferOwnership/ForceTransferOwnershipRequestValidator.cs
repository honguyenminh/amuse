using Amuse.Modules.Tenancy.Features.Common;
using FluentValidation;

namespace Amuse.Modules.Platform.Features.ForceTransferOwnership;

internal sealed class ForceTransferOwnershipRequestValidator : AbstractValidator<TransferOwnershipRequest>
{
    public ForceTransferOwnershipRequestValidator()
    {
        RuleFor(x => x.TargetMemberId).NotEmpty();
    }
}
