using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Tenancy.Features.Common;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.ManagePortalAvatar;

public static class PortalAvatarEndpoint
{
    public static RouteGroupBuilder MapPortalAvatarEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/portal-profile/avatar/presign-upload", async (
                PresignPortalAvatarUploadRequest request,
                PresignPortalAvatarUploadHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, httpContext.User, cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireBusinessPortalPersona)
            .WithRequestValidation()
            .WithName("PresignPortalAvatarUpload")
            .WithSummary("Return a short-lived presigned PUT URL for uploading a business portal avatar.")
            .Produces<PresignPortalAvatarUploadResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/portal-profile/avatar/complete", async (
                CompletePortalAvatarUploadRequest request,
                CompletePortalAvatarUploadHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, httpContext.User, cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireBusinessPortalPersona)
            .WithRequestValidation()
            .WithName("CompletePortalAvatarUpload")
            .WithSummary("Confirm business portal avatar upload and attach it to the profile.")
            .Produces<CompletePortalAvatarUploadResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return group;
    }
}

public sealed class PresignPortalAvatarUploadRequestValidator
    : AbstractValidator<PresignPortalAvatarUploadRequest>
{
    public PresignPortalAvatarUploadRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
    }
}

public sealed class CompletePortalAvatarUploadRequestValidator
    : AbstractValidator<CompletePortalAvatarUploadRequest>
{
    public CompletePortalAvatarUploadRequestValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(PortalAvatarStorage.MaxObjectKeyLength);
    }
}
