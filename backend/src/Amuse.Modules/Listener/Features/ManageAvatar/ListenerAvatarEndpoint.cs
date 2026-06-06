using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Listener.Features.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Listener.Features.ManageAvatar;

public static class ListenerAvatarEndpoint
{
    public static IEndpointRouteBuilder MapListenerAvatarEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/listener/profile/avatar/presign-upload", async (
                PresignListenerAvatarUploadRequest request,
                PresignListenerAvatarUploadHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithRequestValidation()
            .WithName("PresignListenerAvatarUpload")
            .WithSummary("Return a short-lived presigned PUT URL for uploading a listener avatar.")
            .Produces<PresignListenerAvatarUploadResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        endpoints.MapPost("/api/v1/listener/profile/avatar/complete", async (
                CompleteListenerAvatarUploadRequest request,
                CompleteListenerAvatarUploadHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithRequestValidation()
            .WithName("CompleteListenerAvatarUpload")
            .WithSummary("Confirm listener avatar upload and attach it to the profile.")
            .Produces<CompleteListenerAvatarUploadResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}

public sealed class PresignListenerAvatarUploadRequestValidator
    : AbstractValidator<PresignListenerAvatarUploadRequest>
{
    public PresignListenerAvatarUploadRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
    }
}

public sealed class CompleteListenerAvatarUploadRequestValidator
    : AbstractValidator<CompleteListenerAvatarUploadRequest>
{
    public CompleteListenerAvatarUploadRequestValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(ProfileAvatarStorage.MaxObjectKeyLength);
    }
}
