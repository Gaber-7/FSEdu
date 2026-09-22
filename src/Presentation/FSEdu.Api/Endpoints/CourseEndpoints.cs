using FSEdu.Application.Features.Admin.Courses;
using FSEdu.Application.Features.Courses;
using FSEdu.Shared.Contracts.Courses;
using MediatR;

namespace FSEdu.Api.Endpoints;

public static class CourseEndpoints
{
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        // ─── Public Browse ──────────────────────────────
        var publicGroup = app.MapGroup("/api/v1/courses").WithTags("Courses");

        publicGroup.MapGet("/", async (int? stageId, int? subjectId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new BrowseCoursesQuery(stageId, subjectId), ct)).ToHttpResult());

        publicGroup.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetCourseDetailsQuery(id), ct)).ToHttpResult());

        publicGroup.MapGet("/{id:guid}/reviews", async (Guid id, int? take, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetCourseReviewsQuery(id, take ?? 50), ct)).ToHttpResult());

        publicGroup.MapPost("/{id:guid}/review",
            async (Guid id, SubmitReviewRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SubmitCourseReviewCommand(id, req.Rating, req.Comment), ct)).ToHttpResult())
           .RequireAuthorization();

        // Lesson Q&A
        publicGroup.MapGet("/lessons/{id:guid}/questions",
            async (Guid id, int? take, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetLessonQuestionsQuery(id, take ?? 50), ct)).ToHttpResult());

        publicGroup.MapPost("/lessons/{id:guid}/questions",
            async (Guid id, AskLessonQuestionRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AskLessonQuestionCommand(id, req.Body), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapPost("/questions/{questionId:guid}/answer",
            async (Guid questionId, AnswerLessonQuestionRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AnswerLessonQuestionCommand(questionId, req.Body), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapPost("/questions/{questionId:guid}/vote",
            async (Guid questionId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ToggleQuestionVoteCommand(questionId), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapGet("/lessons/{id:guid}/play", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new WatchLessonQuery(id), ct)).ToHttpResult())
           .RequireAuthorization();

        // Lesson notes (private to current student)
        publicGroup.MapGet("/lessons/{id:guid}/note",
            async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetLessonNoteQuery(id), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapPut("/lessons/{id:guid}/note",
            async (Guid id, SaveLessonNoteRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SaveLessonNoteCommand(id, req.Body), ct)).ToHttpResult())
           .RequireAuthorization();

        // Course announcements
        publicGroup.MapGet("/{id:guid}/announcements",
            async (Guid id, int? take, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetCourseAnnouncementsQuery(id, take ?? 30), ct)).ToHttpResult());

        publicGroup.MapPost("/{id:guid}/announcements",
            async (Guid id, PostAnnouncementRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new PostAnnouncementCommand(id, req.Title, req.Body, req.Pinned), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapDelete("/announcements/{annId:guid}",
            async (Guid annId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteAnnouncementCommand(annId), ct)).ToHttpResult())
           .RequireAuthorization();

        // Lesson attachments
        publicGroup.MapGet("/lessons/{id:guid}/attachments",
            async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetLessonAttachmentsQuery(id), ct)).ToHttpResult());

        publicGroup.MapPost("/lessons/{id:guid}/attachments",
            async (Guid id, AddLessonAttachmentRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AddLessonAttachmentCommand(
                    id, req.Title, req.FileUrl, req.FileType, req.SizeBytes), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapDelete("/attachments/{attId:guid}",
            async (Guid attId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteLessonAttachmentCommand(attId), ct)).ToHttpResult())
           .RequireAuthorization();

        // Lesson markers (private timestamp bookmarks)
        publicGroup.MapGet("/lessons/{id:guid}/markers",
            async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetLessonMarkersQuery(id), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapPost("/lessons/{id:guid}/markers",
            async (Guid id, AddLessonMarkerRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AddLessonMarkerCommand(id, req.PositionSec, req.Label), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapDelete("/markers/{markerId:guid}",
            async (Guid markerId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteLessonMarkerCommand(markerId), ct)).ToHttpResult())
           .RequireAuthorization();

        // Lesson reactions
        publicGroup.MapGet("/lessons/{id:guid}/reactions",
            async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetLessonReactionsQuery(id), ct)).ToHttpResult());

        publicGroup.MapPost("/lessons/{id:guid}/reactions",
            async (Guid id, ToggleReactionRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ToggleLessonReactionCommand(id, req.Type), ct)).ToHttpResult())
           .RequireAuthorization();

        // Course Discussions
        publicGroup.MapGet("/{id:guid}/discussions",
            async (Guid id, int? take, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetCourseDiscussionsQuery(id, take ?? 50), ct)).ToHttpResult());

        publicGroup.MapPost("/{id:guid}/discussions",
            async (Guid id, CreateDiscussionRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new CreateDiscussionCommand(id, req.Title, req.Body), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapGet("/discussions/{discussionId:guid}",
            async (Guid discussionId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetDiscussionQuery(discussionId), ct)).ToHttpResult());

        publicGroup.MapPost("/discussions/{discussionId:guid}/replies",
            async (Guid discussionId, PostReplyRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new PostDiscussionReplyCommand(discussionId, req.Body), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapDelete("/discussions/{discussionId:guid}",
            async (Guid discussionId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteDiscussionCommand(discussionId), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapDelete("/discussions/replies/{replyId:guid}",
            async (Guid replyId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteDiscussionReplyCommand(replyId), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapPost("/discussions/{discussionId:guid}/pin",
            async (Guid discussionId, PinLockBody body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ToggleDiscussionPinCommand(discussionId, body.Value), ct)).ToHttpResult())
           .RequireAuthorization();

        publicGroup.MapPost("/discussions/{discussionId:guid}/lock",
            async (Guid discussionId, PinLockBody body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ToggleDiscussionLockCommand(discussionId, body.Value), ct)).ToHttpResult())
           .RequireAuthorization();

        // Multipart upload — returns the URL the teacher can submit via AddLessonAttachmentCommand
        app.MapPost("/api/v1/teacher/courses/lessons/upload-attachment",
            async (IFormFile file, IWebHostEnvironment env, CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "لم يتم رفع ملف" });

            var allowedExtensions = new[]
            {
                ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
                ".txt", ".md", ".rtf",
                ".zip", ".rar", ".7z",
                ".jpg", ".jpeg", ".png", ".webp",
                ".mp3", ".mp4"
            };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return Results.BadRequest(new { error = "صيغة الملف غير مدعومة." });

            // 50 MB cap for attachments
            if (file.Length > 50L * 1024 * 1024)
                return Results.BadRequest(new { error = "الحد الأقصى لحجم الملف 50 ميجابايت" });

            var folder = Path.Combine(env.ContentRootPath, "wwwroot", "uploads", "attachments");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folder, fileName);
            await using (var stream = File.Create(fullPath))
                await file.CopyToAsync(stream, ct);

            var url = $"/uploads/attachments/{fileName}";
            return Results.Ok(new FSEdu.Shared.Contracts.Courses.UploadAttachmentResponse(url, ext.TrimStart('.'), file.Length));
        })
        .DisableAntiforgery()
        .RequireAuthorization(p => p.RequireRole("Teacher"))
        .WithTags("Teacher.Courses");

        // ─── Student self-service profile ───────────────
        app.MapGet("/api/v1/student/profile",
            async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new FSEdu.Application.Features.Students.GetMyStudentProfileQuery(), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Profile");

        app.MapPut("/api/v1/student/profile",
            async (FSEdu.Shared.Contracts.Auth.UpdateStudentProfileRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new FSEdu.Application.Features.Students.UpdateStudentProfileCommand(
                    req.FullName, req.Email, req.Phone, req.WhatsAppNumber), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Profile");

        // ─── Student referrals ──────────────────────────
        app.MapGet("/api/v1/student/referral",
            async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new FSEdu.Application.Features.Students.GetMyReferralInfoQuery(), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Referral");

        app.MapPost("/api/v1/student/referral/redeem",
            async (FSEdu.Shared.Contracts.Students.RedeemReferralCodeRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new FSEdu.Application.Features.Students.RedeemReferralCodeCommand(req.Code), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Referral");

        // ─── Student bookmarks ──────────────────────────
        app.MapGet("/api/v1/student/bookmarks",
            async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyBookmarksQuery(), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Bookmarks");

        app.MapGet("/api/v1/student/bookmarks/{courseId:guid}/state",
            async (Guid courseId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new IsCourseBookmarkedQuery(courseId), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Bookmarks");

        app.MapPost("/api/v1/student/bookmarks/{courseId:guid}",
            async (Guid courseId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ToggleCourseBookmarkCommand(courseId), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Bookmarks");

        // ─── My notes (consolidated) ────────────────────
        app.MapGet("/api/v1/student/notes",
            async (string? search, Guid? courseId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyNotesQuery(search, courseId), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Notes");

        // ─── Weekly schedule ────────────────────────────
        app.MapGet("/api/v1/student/schedule",
            async (int? days, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetStudentScheduleQuery(days ?? 14), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Schedule");

        // ─── Smart suggestions ──────────────────────────
        app.MapGet("/api/v1/student/suggestions",
            async (int? take, ISender sender, CancellationToken ct) =>
                (await sender.Send(new FSEdu.Application.Features.Students.GetMySuggestionsQuery(take ?? 4), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Suggestions");

        // ─── Daily mission ──────────────────────────────
        app.MapGet("/api/v1/student/daily-mission",
            async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new FSEdu.Application.Features.Students.GetDailyMissionQuery(), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.DailyMission");

        // ─── Detailed stats ─────────────────────────────
        app.MapGet("/api/v1/student/detailed-stats",
            async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new FSEdu.Application.Features.Students.GetMyDetailedStatsQuery(), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Stats");

        // ─── Activity feed ──────────────────────────────
        app.MapGet("/api/v1/student/activity",
            async (int? take, ISender sender, CancellationToken ct) =>
                (await sender.Send(new FSEdu.Application.Features.Students.GetMyActivityFeedQuery(take ?? 30), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Activity");

        // ─── Recommendations ────────────────────────────
        app.MapGet("/api/v1/student/recommendations",
            async (int? take, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetCourseRecommendationsQuery(take ?? 6), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Recommendations");

        // ─── Continue watching ──────────────────────────
        app.MapGet("/api/v1/student/continue-watching",
            async (int? take, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetContinueWatchingQuery(take ?? 6), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Progress");

        // ─── Student progress ───────────────────────────
        app.MapGet("/api/v1/student/progress",
            async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyProgressQuery(), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Progress");

        app.MapGet("/api/v1/student/courses/{id:guid}/certificate-data",
            async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetCourseCertificateQuery(id), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Progress");

        app.MapGet("/api/v1/student/courses/{id:guid}/certificate.pdf",
            async (Guid id, ISender sender, FSEdu.Application.Abstractions.ICertificatePdfRenderer renderer, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetCourseCertificateQuery(id), ct);
                if (result.IsFailure) return result.ToHttpResult();
                var bytes = renderer.Render(result.Value);
                var fileName = $"FSEdu-Certificate-{result.Value.CertificateNumber}.pdf";
                return Results.File(bytes, "application/pdf", fileName);
            })
           .RequireAuthorization()
           .WithTags("Student.Progress");

        app.MapGet("/api/v1/student/certificates",
            async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyCertificatesQuery(), ct)).ToHttpResult())
           .RequireAuthorization()
           .WithTags("Student.Progress");

        // ─── Teacher ────────────────────────────────────
        var teacherGroup = app.MapGroup("/api/v1/teacher/courses")
            .WithTags("Teacher.Courses")
            .RequireAuthorization(p => p.RequireRole("Teacher"));

        teacherGroup.MapGet("/my", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyCoursesQuery(), ct)).ToHttpResult());

        teacherGroup.MapGet("/qa-inbox",
            async (bool? unansweredOnly, int? take, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetTeacherQuestionsInboxQuery(
                    unansweredOnly ?? true, take ?? 100), ct)).ToHttpResult());

        teacherGroup.MapPost("/", async (CreateCourseRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateCourseCommand(
                req.SubjectId, req.StageId, req.Title, req.Description, req.ThumbnailUrl, req.Term, req.PreviewVideoUrl), ct);
            return result.ToHttpResult();
        });

        teacherGroup.MapPost("/{courseId:guid}/chapters", async (
            Guid courseId, AddChapterRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AddChapterCommand(courseId, req.Title, req.OrderNum), ct);
            return result.ToHttpResult();
        });

        teacherGroup.MapPost("/chapters/{chapterId:guid}/lessons", async (
            Guid chapterId, AddLessonRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AddLessonCommand(
                chapterId, req.Title, req.Type, req.DurationSeconds,
                req.VideoUrl, req.ScheduledAtUtc, req.IsFreePreview, req.OrderNum), ct);
            return result.ToHttpResult();
        });

        teacherGroup.MapPost("/{courseId:guid}/submit", async (
            Guid courseId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SubmitCourseForReviewCommand(courseId), ct)).ToHttpResult());

        teacherGroup.MapPost("/{courseId:guid}/duplicate", async (
            Guid courseId, DuplicateCourseBody? body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DuplicateCourseCommand(courseId, body?.NewTitle), ct)).ToHttpResult());

        teacherGroup.MapGet("/{courseId:guid}/insights", async (
            Guid courseId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetCourseInsightsQuery(courseId), ct)).ToHttpResult());

        teacherGroup.MapGet("/{courseId:guid}/roster", async (
            Guid courseId, int? take, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetCourseRosterQuery(courseId, take ?? 200), ct)).ToHttpResult());

        teacherGroup.MapPost("/{courseId:guid}/message", async (
            Guid courseId, SendCourseMessageRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SendCourseMessageCommand(
                courseId, req.StudentIds ?? Array.Empty<Guid>(),
                req.Title, req.Body, req.Url), ct)).ToHttpResult());

        teacherGroup.MapGet("/activity-feed", async (int? take, ISender sender, CancellationToken ct) =>
            (await sender.Send(new FSEdu.Application.Features.Teachers.GetTeacherActivityFeedQuery(take ?? 30), ct)).ToHttpResult());

        teacherGroup.MapDelete("/chapters/{chapterId:guid}", async (
            Guid chapterId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DeleteChapterCommand(chapterId), ct)).ToHttpResult());

        teacherGroup.MapDelete("/lessons/{lessonId:guid}", async (
            Guid lessonId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DeleteLessonCommand(lessonId), ct)).ToHttpResult());

        // ─── Admin ──────────────────────────────────────
        var adminGroup = app.MapGroup("/api/v1/admin/courses")
            .WithTags("Admin.Courses")
            .RequireAuthorization(p => p.RequireRole("Admin"));

        adminGroup.MapGet("/pending", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetPendingCoursesQuery(), ct)).ToHttpResult());

        adminGroup.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ApproveCourseCommand(id), ct)).ToHttpResult());

        adminGroup.MapPost("/{id:guid}/reject", async (Guid id, string? reason, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RejectCourseCommand(id, reason), ct)).ToHttpResult());

        return app;
    }
}

public sealed record DuplicateCourseBody(string? NewTitle);
public sealed record PinLockBody(bool Value);
