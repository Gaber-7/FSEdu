using FSEdu.Application.Abstractions;
using FSEdu.Application.Features.Auth.Login;
using FSEdu.Application.Features.Auth.RegisterParent;
using FSEdu.Application.Features.Auth.RegisterStudent;
using FSEdu.Application.Features.Auth.RegisterTeacher;
using FSEdu.Application.Features.Users;
using FSEdu.Shared.Contracts.Auth;
using FSEdu.Shared.Kernel.Results;
using MediatR;

namespace FSEdu.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register/student", async (RegisterStudentRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RegisterStudentCommand(
                req.FullName, req.Phone, req.Email, req.Password,
                req.StageId, req.RegionId, req.SchoolId, req.ParentPhone), ct);
            return result.ToHttpResult();
        });

        group.MapPost("/register/parent", async (RegisterParentRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RegisterParentCommand(
                req.FullName, req.Phone, req.Email, req.Password,
                req.NationalId, req.Occupation), ct);
            return result.ToHttpResult();
        });

        group.MapPost("/register/teacher", async (RegisterTeacherRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RegisterTeacherCommand(
                req.FullName, req.Phone, req.Email, req.Password,
                req.Bio, req.YearsOfExperience,
                req.SubjectIds, req.RegionIds, req.Qualifications), ct);
            return result.ToHttpResult();
        });

        group.MapPost("/login", async (LoginRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginCommand(req.Phone, req.Password, req.TotpCode), ct);
            return result.ToHttpResult();
        });

        group.MapPost("/otp/send", async (SendOtpRequest req, IIdentityService identity, CancellationToken ct) =>
            (await identity.SendOtpAsync(req.Phone, ct)).ToHttpResult());

        group.MapPost("/otp/verify", async (VerifyOtpRequest req, IIdentityService identity, CancellationToken ct) =>
            (await identity.VerifyOtpAsync(req.Phone, req.Code, ct)).ToHttpResult());

        group.MapPost("/refresh", async (RefreshTokenRequest req, IIdentityService identity, CancellationToken ct) =>
            (await identity.RefreshAsync(req.RefreshToken, ct)).ToHttpResult());

        // ─── Self avatar (any authenticated user) ────────────
        var me = app.MapGroup("/api/v1/me").WithTags("Me").RequireAuthorization();

        // Multipart upload — saves to wwwroot/uploads/avatars and returns the URL.
        // Does NOT set the user's avatar; client follows up with PUT /api/v1/me/avatar.
        me.MapPost("/avatar/upload", async (
            IFormFile file, IWebHostEnvironment env, CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "لم يتم رفع ملف" });

            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
                return Results.BadRequest(new { error = "الصيغة غير مدعومة. JPG/PNG/WEBP/GIF فقط" });

            if (file.Length > 3 * 1024 * 1024)
                return Results.BadRequest(new { error = "الحد الأقصى 3 ميجابايت" });

            var folder = Path.Combine(env.ContentRootPath, "wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folder, fileName);
            await using (var stream = File.Create(fullPath))
                await file.CopyToAsync(stream, ct);

            var url = $"/uploads/avatars/{fileName}";
            return Results.Ok(new UploadAvatarResponse(url));
        })
        .DisableAntiforgery();

        me.MapPut("/avatar", async (SetAvatarBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetMyAvatarCommand(body.Url), ct)).ToHttpResult());

        me.MapDelete("/avatar", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetMyAvatarCommand(null), ct)).ToHttpResult());

        return app;
    }
}

public sealed record SetAvatarBody(string? Url);

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.Ok() : MapError(result.Error);

    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error);

    private static IResult MapError(Shared.Kernel.Results.Error error) =>
        error.Type switch
        {
            Shared.Kernel.Results.ErrorType.NotFound => Results.NotFound(new ErrorResponse(error.Code, error.MessageAr)),
            Shared.Kernel.Results.ErrorType.Validation => Results.BadRequest(new ErrorResponse(error.Code, error.MessageAr)),
            Shared.Kernel.Results.ErrorType.Conflict => Results.Conflict(new ErrorResponse(error.Code, error.MessageAr)),
            Shared.Kernel.Results.ErrorType.Unauthorized => Results.Json(new ErrorResponse(error.Code, error.MessageAr), statusCode: 401),
            Shared.Kernel.Results.ErrorType.Forbidden => Results.Json(new ErrorResponse(error.Code, error.MessageAr), statusCode: 403),
            _ => Results.Json(new ErrorResponse(error.Code, error.MessageAr), statusCode: 500)
        };
}
