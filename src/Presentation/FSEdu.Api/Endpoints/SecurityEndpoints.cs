using FSEdu.Application.Features.Security;
using FSEdu.Shared.Contracts.Security;
using FSEdu.Shared.Kernel.Results;
using MediatR;

namespace FSEdu.Api.Endpoints;

public static class SecurityEndpoints
{
    public static void MapSecurityEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/security/totp")
                   .RequireAuthorization()
                   .WithTags("Security.Totp");

        g.MapGet("/status", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetTotpStatusQuery(), ct)).ToHttpResult());

        g.MapPost("/setup", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new BeginTotpSetupCommand(), ct)).ToHttpResult());

        g.MapPost("/confirm", async (TotpVerifyRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ConfirmTotpSetupCommand(req.Code), ct)).ToHttpResult());

        g.MapPost("/disable", async (TotpVerifyRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DisableTotpCommand(req.Code), ct)).ToHttpResult());

        g.MapPost("/recovery/regenerate", async (TotpVerifyRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RegenerateTotpRecoveryCommand(req.Code), ct)).ToHttpResult());
    }
}
