using FSEdu.Application.Features.Admin.Payments;
using FSEdu.Application.Features.Subscriptions;
using FSEdu.Shared.Contracts.Payments;
using MediatR;
using Microsoft.Extensions.Options;

namespace FSEdu.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        // ─── Public: payment instructions ──────────────
        app.MapGet("/api/v1/payments/instructions", (IOptions<PaymentAccountsOptions> opts) =>
        {
            var a = opts.Value;
            return Results.Ok(new PaymentInstructionsDto(
                a.InstaPayNumber, a.InstaPayLink, a.VodafoneCashNumber,
                a.BankName, a.BankAccount, a.BankIban, a.AccountHolder));
        })
        .WithTags("Payments");

        // ─── Student: subscribe with payment ────────────
        var student = app.MapGroup("/api/v1/payments")
            .WithTags("Payments")
            .RequireAuthorization();

        student.MapPost("/subscribe", async (
            SubscribeWithPaymentRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SubscribeWithPaymentCommand(
                req.Type, req.SubjectId, req.StageId, req.Term,
                req.PaymentMethod, req.ReferenceNumber,
                req.ReceiptImageUrl, req.SenderName, req.Notes,
                req.CouponCode), ct);
            return result.ToHttpResult();
        });

        student.MapPost("/validate-coupon", async (
            ValidateCouponRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new FSEdu.Application.Features.Subscriptions.ValidateCouponQuery(req.Code, req.Amount), ct)).ToHttpResult());

        student.MapPost("/upload-receipt", async (
            IFormFile file, IWebHostEnvironment env, CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "لم يتم رفع ملف" });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return Results.BadRequest(new { error = "صيغة الملف غير مدعومة. الأنواع المسموحة: JPG, PNG, PDF, WEBP" });

            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest(new { error = "الحد الأقصى لحجم الملف 5 ميجابايت" });

            var folder = Path.Combine(env.ContentRootPath, "wwwroot", "uploads", "receipts");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            await using (var stream = File.Create(fullPath))
                await file.CopyToAsync(stream, ct);

            var url = $"/uploads/receipts/{fileName}";
            return Results.Ok(new UploadReceiptResponse(url));
        })
        .DisableAntiforgery(); // multipart from Blazor

        // ─── Admin: review payments ────────────────────
        var admin = app.MapGroup("/api/v1/admin/payments")
            .WithTags("Admin.Payments")
            .RequireAuthorization(p => p.RequireRole("Admin"));

        admin.MapGet("/pending", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetPendingPaymentsQuery(), ct)).ToHttpResult());

        admin.MapPost("/{id:guid}/approve", async (
            Guid id, ApprovePaymentRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ApprovePaymentCommand(id, req.Note), ct)).ToHttpResult());

        admin.MapPost("/{id:guid}/reject", async (
            Guid id, RejectPaymentRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RejectPaymentCommand(id, req.Reason), ct)).ToHttpResult());

        return app;
    }
}

public sealed class PaymentAccountsOptions
{
    public const string SectionName = "PaymentAccounts";
    public string InstaPayNumber { get; set; } = "";
    public string InstaPayLink { get; set; } = "";
    public string VodafoneCashNumber { get; set; } = "";
    public string BankName { get; set; } = "";
    public string BankAccount { get; set; } = "";
    public string BankIban { get; set; } = "";
    public string AccountHolder { get; set; } = "";
}
