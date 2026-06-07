using Amuse.Modules.Tenancy.Features.Common;
using FluentValidation;

namespace Amuse.Modules.Tenancy.Features.TransferOwnership;

internal sealed class TransferOwnershipRequestValidator : AbstractValidator<TransferOwnershipRequest>
{
    public TransferOwnershipRequestValidator()
    {
        RuleFor(x => x.TargetMemberId).NotEmpty();
    }
}
