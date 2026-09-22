using System.Net.Http.Headers;
using System.Net.Http.Json;
using FSEdu.Shared.Contracts.Admin;
using FSEdu.Shared.Contracts.Assessments;
using FSEdu.Shared.Contracts.Auth;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Contracts.Dashboard;
using FSEdu.Shared.Contracts.Parent;
using FSEdu.Shared.Contracts.Payments;
using FSEdu.Shared.Contracts.Reference;
using FSEdu.Shared.Contracts.Gamification;
using FSEdu.Shared.Contracts.LiveClassroom;
using FSEdu.Shared.Contracts.Notifications;
using FSEdu.Shared.Contracts.Subscriptions;
using FSEdu.Shared.Contracts.Tickets;

namespace FSEdu.Web.Services;

public sealed class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http) => _http = http;

    public void SetAuthToken(string? token)
    {
        _http.DefaultRequestHeaders.Authorization =
            token is null ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    // ─── Reference ──────────────────────────────────────
    public async Task<List<StageDto>> GetStagesAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<StageDto>>("api/v1/reference/stages", ct) ?? new();

    public async Task<List<RegionDto>> GetRegionsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<RegionDto>>("api/v1/reference/regions", ct) ?? new();

    public async Task<List<SchoolDto>> GetSchoolsAsync(int? regionId = null, CancellationToken ct = default)
    {
        var url = regionId.HasValue ? $"api/v1/reference/schools?regionId={regionId}" : "api/v1/reference/schools";
        return await _http.GetFromJsonAsync<List<SchoolDto>>(url, ct) ?? new();
    }

    // ─── Auth ────────────────────────────────────────────
    public async Task<(AuthResponse? Data, ErrorResponse? Error)> RegisterStudentAsync(
        RegisterStudentRequest req, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/v1/auth/register/student", req, ct);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<AuthResponse>(ct), null);
        var err = await response.Content.ReadFromJsonAsync<ErrorResponse>(ct);
        return (null, err ?? new ErrorResponse("UNKNOWN", "حدث خطأ غير متوقع"));
    }

    public async Task<(AuthResponse? Data, ErrorResponse? Error)> LoginAsync(
        LoginRequest req, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/v1/auth/login", req, ct);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<AuthResponse>(ct), null);
        var err = await response.Content.ReadFromJsonAsync<ErrorResponse>(ct);
        return (null, err ?? new ErrorResponse("UNKNOWN", "حدث خطأ غير متوقع"));
    }

    public async Task<ErrorResponse?> SendOtpAsync(SendOtpRequest req, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/v1/auth/otp/send", req, ct);
        if (response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ErrorResponse>(ct)
               ?? new ErrorResponse("UNKNOWN", "فشل إرسال الرمز");
    }

    public async Task<ErrorResponse?> VerifyOtpAsync(VerifyOtpRequest req, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/v1/auth/otp/verify", req, ct);
        if (response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ErrorResponse>(ct)
               ?? new ErrorResponse("UNKNOWN", "فشل التحقق");
    }

    public async Task<(AuthResponse? Data, ErrorResponse? Error)> RegisterParentAsync(
        RegisterParentRequest req, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/v1/auth/register/parent", req, ct);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<AuthResponse>(ct), null);
        return (null, await response.Content.ReadFromJsonAsync<ErrorResponse>(ct)
                  ?? new ErrorResponse("UNKNOWN", "خطأ غير متوقع"));
    }

    public async Task<(AuthResponse? Data, ErrorResponse? Error)> RegisterTeacherAsync(
        RegisterTeacherRequest req, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/v1/auth/register/teacher", req, ct);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<AuthResponse>(ct), null);
        return (null, await response.Content.ReadFromJsonAsync<ErrorResponse>(ct)
                  ?? new ErrorResponse("UNKNOWN", "خطأ غير متوقع"));
    }

    public async Task<List<SubjectDto>> GetSubjectsAsync(int? stageId = null, CancellationToken ct = default)
    {
        var url = stageId.HasValue ? $"api/v1/reference/subjects?stageId={stageId}" : "api/v1/reference/subjects";
        return await _http.GetFromJsonAsync<List<SubjectDto>>(url, ct) ?? new();
    }

    // ─── Admin ────────────────────────────────────────────
    public async Task<List<PendingTeacherDto>> GetPendingTeachersAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<List<PendingTeacherDto>>("api/v1/admin/teachers/pending", ct);
        return list ?? new();
    }

    public async Task<bool> ApproveTeacherAsync(Guid id, string? notes, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/v1/admin/teachers/{id}/approve",
            new ApproveTeacherRequest(notes), ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RejectTeacherAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/v1/admin/teachers/{id}/reject",
            new RejectTeacherRequest(reason), ct);
        return response.IsSuccessStatusCode;
    }

    // ─── Parent Zone ──────────────────────────────────────
    public async Task<List<ChildSummaryDto>> GetMyChildrenAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<List<ChildSummaryDto>>("api/v1/parent/children", ct);
        return list ?? new();
    }

    public async Task<(bool Ok, ErrorResponse? Error)> LinkChildAsync(string phone, string relation, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/v1/parent/children/link",
            new LinkChildRequest(phone, relation), ct);
        if (response.IsSuccessStatusCode) return (true, null);
        var err = await response.Content.ReadFromJsonAsync<ErrorResponse>(ct);
        return (false, err ?? new ErrorResponse("UNKNOWN", "خطأ غير متوقع"));
    }

    // ─── Subscriptions ──────────────────────────────────
    public async Task<PricingResponse?> GetPricingAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<PricingResponse>("api/v1/subscriptions/pricing", ct);

    public async Task<(SubscribeResponse? Data, ErrorResponse? Error)> SubscribeAsync(
        SubscribeRequest req, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/v1/subscriptions/subscribe", req, ct);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<SubscribeResponse>(ct), null);
        var err = await response.Content.ReadFromJsonAsync<ErrorResponse>(ct);
        return (null, err ?? new ErrorResponse("UNKNOWN", "فشل الاشتراك"));
    }

    public async Task<List<MySubscriptionDto>> GetMySubscriptionsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<MySubscriptionDto>>("api/v1/subscriptions/my", ct) ?? new();

    // ─── Courses ──────────────────────────────────────
    public async Task<List<CourseListItemDto>> BrowseCoursesAsync(int? stageId = null, int? subjectId = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (stageId.HasValue) qs.Add($"stageId={stageId}");
        if (subjectId.HasValue) qs.Add($"subjectId={subjectId}");
        var url = "api/v1/courses" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        return await _http.GetFromJsonAsync<List<CourseListItemDto>>(url, ct) ?? new();
    }

    public async Task<CourseDetailDto?> GetCourseDetailsAsync(Guid id, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<CourseDetailDto>($"api/v1/courses/{id}", ct);

    public async Task<LessonPlaybackDto?> PlayLessonAsync(Guid lessonId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<LessonPlaybackDto>($"api/v1/courses/lessons/{lessonId}/play", ct);

    public async Task<List<CourseListItemDto>> GetMyTeacherCoursesAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<CourseListItemDto>>("api/v1/teacher/courses/my", ct) ?? new();

    public async Task<(Guid? Id, ErrorResponse? Error)> CreateCourseAsync(CreateCourseRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/teacher/courses", req, ct);
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<Guid>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<(Guid? Id, ErrorResponse? Error)> AddChapterAsync(Guid courseId, AddChapterRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/teacher/courses/{courseId}/chapters", req, ct);
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<Guid>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<(Guid? Id, ErrorResponse? Error)> AddLessonAsync(Guid chapterId, AddLessonRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/teacher/courses/chapters/{chapterId}/lessons", req, ct);
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<Guid>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<ErrorResponse?> SubmitCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/teacher/courses/{courseId}/submit", null, ct);
        if (resp.IsSuccessStatusCode) return null;
        return await ReadErrorAsync(resp, ct);
    }

    public async Task<(Guid? newCourseId, ErrorResponse? error)> DuplicateCourseAsync(Guid courseId, string? newTitle = null, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/teacher/courses/{courseId}/duplicate",
            new { NewTitle = newTitle }, ct);
        if (resp.IsSuccessStatusCode)
        {
            try { return (await resp.Content.ReadFromJsonAsync<Guid>(ct), null); }
            catch { return (Guid.Empty, null); }
        }
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<CourseInsightsDto?> GetCourseInsightsAsync(Guid courseId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<CourseInsightsDto>($"api/v1/teacher/courses/{courseId}/insights", ct); }
        catch { return null; }
    }

    public async Task<CourseRosterDto?> GetCourseRosterAsync(Guid courseId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<CourseRosterDto>($"api/v1/teacher/courses/{courseId}/roster", ct); }
        catch { return null; }
    }

    public async Task<(int? recipients, ErrorResponse? error)> SendCourseMessageAsync(
        Guid courseId, Guid[] studentIds, string title, string body, string? url = null, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/teacher/courses/{courseId}/message",
            new SendCourseMessageRequest(studentIds, title, body, url), ct);
        if (resp.IsSuccessStatusCode)
        {
            try
            {
                var dto = await resp.Content.ReadFromJsonAsync<SendCourseMessageResponse>(cancellationToken: ct);
                return (dto?.Recipients ?? 0, null);
            }
            catch { return (0, null); }
        }
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<List<TeacherActivityItemDto>> GetTeacherActivityFeedAsync(int take = 30, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<TeacherActivityItemDto>>($"api/v1/teacher/courses/activity-feed?take={take}", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<ErrorResponse?> DeleteChapterAsync(Guid chapterId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/v1/teacher/courses/chapters/{chapterId}", ct);
        if (resp.IsSuccessStatusCode) return null;
        return await ReadErrorAsync(resp, ct);
    }

    public async Task<ErrorResponse?> DeleteLessonAsync(Guid lessonId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/v1/teacher/courses/lessons/{lessonId}", ct);
        if (resp.IsSuccessStatusCode) return null;
        return await ReadErrorAsync(resp, ct);
    }

    // ─── Dashboards ───────────────────────────────────
    public async Task<StudentDashboardDto?> GetStudentDashboardAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<StudentDashboardDto>("api/v1/dashboard/student", ct);

    public async Task<TeacherDashboardDto?> GetTeacherDashboardAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<TeacherDashboardDto>("api/v1/dashboard/teacher", ct);

    public async Task<ParentDashboardDto?> GetParentDashboardAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<ParentDashboardDto>("api/v1/dashboard/parent", ct);

    public async Task<ChildDetailDto?> GetChildDetailAsync(Guid studentId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<ChildDetailDto>($"api/v1/dashboard/parent/child/{studentId}", ct); }
        catch { return null; }
    }

    // ─── Assessments ────────────────────────────────────
    public async Task<List<AssessmentListItemDto>> GetCourseAssessmentsAsync(Guid courseId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<AssessmentListItemDto>>($"api/v1/courses/{courseId}/assessments", ct) ?? new();

    public async Task<AssessmentDetailDto?> GetAssessmentDetailsAsync(Guid id, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<AssessmentDetailDto>($"api/v1/assessments/{id}", ct);

    public async Task<(Guid? Id, ErrorResponse? Error)> CreateAssessmentAsync(CreateAssessmentRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/teacher/assessments", req, ct);
        if (resp.IsSuccessStatusCode) return (await resp.Content.ReadFromJsonAsync<Guid>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<(Guid? Id, ErrorResponse? Error)> AddQuestionAsync(Guid assessmentId, AddQuestionRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/teacher/assessments/{assessmentId}/questions", req, ct);
        if (resp.IsSuccessStatusCode) return (await resp.Content.ReadFromJsonAsync<Guid>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<ErrorResponse?> RemoveQuestionAsync(Guid assessmentId, Guid questionId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/v1/teacher/assessments/{assessmentId}/questions/{questionId}", ct);
        if (resp.IsSuccessStatusCode) return null;
        return await ReadErrorAsync(resp, ct);
    }

    public async Task<(StartAttemptResponse? Data, ErrorResponse? Error)> StartAttemptAsync(Guid assessmentId, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/student/assessments/{assessmentId}/start", null, ct);
        if (resp.IsSuccessStatusCode) return (await resp.Content.ReadFromJsonAsync<StartAttemptResponse>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<(AttemptResultDto? Data, ErrorResponse? Error)> SubmitAttemptAsync(Guid attemptId, SubmitAttemptRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/student/attempts/{attemptId}/submit", req, ct);
        if (resp.IsSuccessStatusCode) return (await resp.Content.ReadFromJsonAsync<AttemptResultDto>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<AttemptResultDto?> GetAttemptResultAsync(Guid attemptId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<AttemptResultDto>($"api/v1/student/attempts/{attemptId}/result", ct);

    // ─── Payments (manual) ─────────────────────────────
    public async Task<PaymentInstructionsDto?> GetPaymentInstructionsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<PaymentInstructionsDto>("api/v1/payments/instructions", ct);

    public async Task<(SubscribeWithPaymentResponse? Data, ErrorResponse? Error)> SubscribeWithPaymentAsync(
        SubscribeWithPaymentRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/payments/subscribe", req, ct);
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<SubscribeWithPaymentResponse>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<ValidateCouponResponse?> ValidateCouponAsync(string code, decimal amount, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/v1/payments/validate-coupon",
                new ValidateCouponRequest(code, amount), ct);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<ValidateCouponResponse>(cancellationToken: ct);
        }
        catch { return null; }
    }

    public async Task<(string? Url, ErrorResponse? Error)> UploadReceiptAsync(
        Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", fileName);

        var resp = await _http.PostAsync("api/v1/payments/upload-receipt", content, ct);
        if (resp.IsSuccessStatusCode)
        {
            var result = await resp.Content.ReadFromJsonAsync<UploadReceiptResponse>(ct);
            return (result?.Url, null);
        }
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<List<PendingPaymentDto>> GetPendingPaymentsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<PendingPaymentDto>>("api/v1/admin/payments/pending", ct) ?? new();

    public async Task<ErrorResponse?> ApprovePaymentAsync(Guid id, string? note, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/admin/payments/{id}/approve",
            new ApprovePaymentRequest(note), ct);
        if (resp.IsSuccessStatusCode) return null;
        return await ReadErrorAsync(resp, ct);
    }

    public async Task<ErrorResponse?> RejectPaymentAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/admin/payments/{id}/reject",
            new RejectPaymentRequest(reason), ct);
        if (resp.IsSuccessStatusCode) return null;
        return await ReadErrorAsync(resp, ct);
    }

    // ─── Tickets (Ask Teacher) ─────────────────────────
    public async Task<List<TicketListItemDto>> GetMyTicketsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<TicketListItemDto>>("api/v1/student/tickets/my", ct) ?? new();

    public async Task<List<TicketListItemDto>> GetStaffInboxAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<TicketListItemDto>>("api/v1/staff/tickets/inbox", ct) ?? new();

    public async Task<TicketDetailDto?> GetTicketDetailsAsync(Guid id, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<TicketDetailDto>($"api/v1/tickets/{id}", ct);

    public async Task<(Guid? Id, ErrorResponse? Error)> CreateTicketAsync(CreateTicketRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/student/tickets", req, ct);
        if (resp.IsSuccessStatusCode) return (await resp.Content.ReadFromJsonAsync<Guid>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<ErrorResponse?> ReplyToTicketAsync(Guid id, ReplyToTicketRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/tickets/{id}/reply", req, ct);
        if (resp.IsSuccessStatusCode) return null;
        return await ReadErrorAsync(resp, ct);
    }

    public async Task<ErrorResponse?> ResolveTicketAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/tickets/{id}/resolve", null, ct);
        if (resp.IsSuccessStatusCode) return null;
        return await ReadErrorAsync(resp, ct);
    }

    // ─── Notifications ─────────────────────────────────
    public async Task<List<NotificationDto>> GetMyNotificationsAsync(int take = 30, bool unreadOnly = false, CancellationToken ct = default)
    {
        var qs = $"take={take}&unreadOnly={(unreadOnly ? "true" : "false")}";
        return await _http.GetFromJsonAsync<List<NotificationDto>>($"api/v1/notifications/my?{qs}", ct) ?? new();
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken ct = default)
    {
        try
        {
            var dto = await _http.GetFromJsonAsync<UnreadCountDto>("api/v1/notifications/unread-count", ct);
            return dto?.Count ?? 0;
        }
        catch { return 0; }
    }

    public async Task<bool> MarkNotificationReadAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/notifications/{id}/read", null, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> MarkAllNotificationsReadAsync(CancellationToken ct = default)
    {
        var resp = await _http.PostAsync("api/v1/notifications/read-all", null, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<FSEdu.Shared.Contracts.Auth.NotificationPreferenceDto>> GetNotificationPreferencesAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<FSEdu.Shared.Contracts.Auth.NotificationPreferenceDto>>("api/v1/notifications/preferences", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<bool> SetNotificationPreferenceAsync(string category, bool enabled, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync("api/v1/notifications/preferences",
            new FSEdu.Shared.Contracts.Auth.UpdateNotificationPreferenceRequest(category, enabled), ct);
        return resp.IsSuccessStatusCode;
    }

    // ─── Live Classroom ────────────────────────────────
    public async Task<List<LiveSessionListItemDto>> GetMyLiveSessionsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<LiveSessionListItemDto>>("api/v1/teacher/live/my", ct) ?? new();

    public async Task<List<LiveSessionListItemDto>> GetUpcomingLiveSessionsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<LiveSessionListItemDto>>("api/v1/student/live/upcoming", ct) ?? new();

    public async Task<List<LiveSessionListItemDto>> GetRecordedLiveSessionsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<LiveSessionListItemDto>>("api/v1/student/live/recorded", ct) ?? new();

    public async Task<StudentProgressDto?> GetMyProgressAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<StudentProgressDto>("api/v1/student/progress", ct);

    public async Task<(CourseCertificateDto? Data, ErrorResponse? Error)> GetCourseCertificateAsync(
        Guid courseId, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"api/v1/student/courses/{courseId}/certificate-data", ct);
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<CourseCertificateDto>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<List<MyCertificateRowDto>> GetMyCertificatesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<MyCertificateRowDto>>("api/v1/student/certificates", ct) ?? new();
        }
        catch { return new(); }
    }

    public async Task<byte[]?> DownloadCertificatePdfAsync(Guid courseId, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync($"api/v1/student/courses/{courseId}/certificate.pdf", ct);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsByteArrayAsync(ct);
        }
        catch { return null; }
    }

    public async Task<CourseReviewsResponse?> GetCourseReviewsAsync(Guid courseId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<CourseReviewsResponse>($"api/v1/courses/{courseId}/reviews", ct);

    public async Task<ErrorResponse?> SubmitCourseReviewAsync(Guid courseId, SubmitReviewRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/courses/{courseId}/review", req, ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    public async Task<LessonQuestionsResponse?> GetLessonQuestionsAsync(Guid lessonId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<LessonQuestionsResponse>($"api/v1/courses/lessons/{lessonId}/questions", ct);

    public async Task<ErrorResponse?> AskLessonQuestionAsync(Guid lessonId, string body, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/courses/lessons/{lessonId}/questions",
            new AskLessonQuestionRequest(body), ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    public async Task<ErrorResponse?> AnswerLessonQuestionAsync(Guid questionId, string body, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/courses/questions/{questionId}/answer",
            new AnswerLessonQuestionRequest(body), ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    public async Task<VoteQuestionResponse?> ToggleQuestionVoteAsync(Guid questionId, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/courses/questions/{questionId}/vote", content: null, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<VoteQuestionResponse>(cancellationToken: ct);
    }

    public async Task<TeacherInboxResponse?> GetTeacherQaInboxAsync(bool unansweredOnly = true, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<TeacherInboxResponse>($"api/v1/teacher/courses/qa-inbox?unansweredOnly={unansweredOnly.ToString().ToLowerInvariant()}", ct); }
        catch { return null; }
    }

    public async Task<LessonNoteDto?> GetLessonNoteAsync(Guid lessonId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<LessonNoteDto>($"api/v1/courses/lessons/{lessonId}/note", ct); }
        catch { return null; }
    }

    public async Task<bool> SaveLessonNoteAsync(Guid lessonId, string body, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"api/v1/courses/lessons/{lessonId}/note",
            new SaveLessonNoteRequest(body), ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<MyNotesResponse?> GetMyNotesAsync(string? search = null, Guid? courseId = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        if (courseId.HasValue) qs.Add($"courseId={courseId}");
        var url = "api/v1/student/notes" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        try { return await _http.GetFromJsonAsync<MyNotesResponse>(url, ct); }
        catch { return null; }
    }

    public async Task<CourseAnnouncementsResponse?> GetCourseAnnouncementsAsync(Guid courseId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<CourseAnnouncementsResponse>($"api/v1/courses/{courseId}/announcements", ct); }
        catch { return null; }
    }

    public async Task<ErrorResponse?> PostAnnouncementAsync(Guid courseId, PostAnnouncementRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/courses/{courseId}/announcements", req, ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    public async Task<bool> DeleteAnnouncementAsync(Guid announcementId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/v1/courses/announcements/{announcementId}", ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<LessonAttachmentDto>> GetLessonAttachmentsAsync(Guid lessonId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<LessonAttachmentDto>>($"api/v1/courses/lessons/{lessonId}/attachments", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<UploadAttachmentResponse?> UploadLessonAttachmentFileAsync(System.IO.Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
        content.Add(streamContent, "file", fileName);
        var resp = await _http.PostAsync("api/v1/teacher/courses/lessons/upload-attachment", content, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<UploadAttachmentResponse>(cancellationToken: ct);
    }

    public async Task<bool> AddLessonAttachmentAsync(Guid lessonId, AddLessonAttachmentRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/courses/lessons/{lessonId}/attachments", req, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteLessonAttachmentAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/v1/courses/attachments/{attachmentId}", ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<LessonMarkerDto>> GetLessonMarkersAsync(Guid lessonId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<LessonMarkerDto>>($"api/v1/courses/lessons/{lessonId}/markers", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<Guid?> AddLessonMarkerAsync(Guid lessonId, int positionSec, string? label, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/courses/lessons/{lessonId}/markers",
            new AddLessonMarkerRequest(positionSec, label), ct);
        if (!resp.IsSuccessStatusCode) return null;
        try { return await resp.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct); }
        catch { return Guid.NewGuid(); }
    }

    public async Task<bool> DeleteLessonMarkerAsync(Guid markerId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/v1/courses/markers/{markerId}", ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<LessonReactionsDto?> GetLessonReactionsAsync(Guid lessonId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<LessonReactionsDto>($"api/v1/courses/lessons/{lessonId}/reactions", ct); }
        catch { return null; }
    }

    public async Task<ToggleReactionResponse?> ToggleLessonReactionAsync(Guid lessonId, string type, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/courses/lessons/{lessonId}/reactions",
            new ToggleReactionRequest(type), ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<ToggleReactionResponse>(cancellationToken: ct);
    }

    // ─── Course Discussions ────────────────────────
    public async Task<DiscussionsResponse?> GetCourseDiscussionsAsync(Guid courseId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<DiscussionsResponse>($"api/v1/courses/{courseId}/discussions", ct); }
        catch { return null; }
    }

    public async Task<DiscussionDetailDto?> GetDiscussionAsync(Guid discussionId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<DiscussionDetailDto>($"api/v1/courses/discussions/{discussionId}", ct); }
        catch { return null; }
    }

    public async Task<(Guid? id, ErrorResponse? error)> CreateDiscussionAsync(Guid courseId, string title, string body, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/courses/{courseId}/discussions",
            new CreateDiscussionRequest(title, body), ct);
        if (resp.IsSuccessStatusCode)
        {
            try { return (await resp.Content.ReadFromJsonAsync<Guid>(ct), null); }
            catch { return (Guid.Empty, null); }
        }
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<ErrorResponse?> PostDiscussionReplyAsync(Guid discussionId, string body, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/courses/discussions/{discussionId}/replies",
            new PostReplyRequest(body), ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    public async Task<bool> DeleteDiscussionAsync(Guid discussionId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/v1/courses/discussions/{discussionId}", ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteDiscussionReplyAsync(Guid replyId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/v1/courses/discussions/replies/{replyId}", ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleDiscussionPinAsync(Guid discussionId, bool pinned, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/courses/discussions/{discussionId}/pin",
            new { Value = pinned }, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleDiscussionLockAsync(Guid discussionId, bool locked, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/courses/discussions/{discussionId}/lock",
            new { Value = locked }, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<BookmarkedCourseDto>> GetMyBookmarksAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<BookmarkedCourseDto>>("api/v1/student/bookmarks", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<bool> IsCourseBookmarkedAsync(Guid courseId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<bool>($"api/v1/student/bookmarks/{courseId}/state", ct); }
        catch { return false; }
    }

    public async Task<bool?> ToggleCourseBookmarkAsync(Guid courseId, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/student/bookmarks/{courseId}", content: null, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var dto = await resp.Content.ReadFromJsonAsync<ToggleBookmarkResponse>(cancellationToken: ct);
        return dto?.IsBookmarked;
    }

    public async Task<FSEdu.Shared.Contracts.Auth.StudentProfileDto?> GetMyStudentProfileAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<FSEdu.Shared.Contracts.Auth.StudentProfileDto>("api/v1/student/profile", ct); }
        catch { return null; }
    }

    public async Task<(bool ok, ErrorResponse? error)> UpdateStudentProfileAsync(
        string fullName, string? email, string? phone = null, string? whatsAppNumber = null,
        CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync("api/v1/student/profile",
            new FSEdu.Shared.Contracts.Auth.UpdateStudentProfileRequest(fullName, email, phone, whatsAppNumber), ct);
        if (resp.IsSuccessStatusCode) return (true, null);
        return (false, await ReadErrorAsync(resp, ct));
    }

    // ─── Parent Weekly Digest ──────────────────────
    public async Task<(bool enabled, DateTime? lastSentUtc)> GetParentDigestStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync("api/v1/parent/digest/status", ct);
            if (!resp.IsSuccessStatusCode) return (false, null);
            using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var enabled = doc.RootElement.TryGetProperty("weeklyDigestEnabled", out var e) && (e.ValueKind == System.Text.Json.JsonValueKind.True || (e.ValueKind == System.Text.Json.JsonValueKind.Null ? false : e.GetBoolean()));
            DateTime? last = null;
            if (doc.RootElement.TryGetProperty("lastWeeklyDigestAtUtc", out var l) && l.ValueKind == System.Text.Json.JsonValueKind.String && l.TryGetDateTime(out var dt)) last = dt;
            return (enabled, last);
        }
        catch { return (false, null); }
    }

    public async Task<bool> SendParentDigestNowAsync(CancellationToken ct = default)
    {
        var resp = await _http.PostAsync("api/v1/parent/digest/send-now", null, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleParentDigestAsync(bool enabled, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/parent/digest/toggle", new { enabled }, ct);
        return resp.IsSuccessStatusCode;
    }

    // ─── Daily Challenge ───────────────────────────
    public async Task<FSEdu.Shared.Contracts.Gamification.DailyChallengeStatusDto?> GetDailyChallengeAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<FSEdu.Shared.Contracts.Gamification.DailyChallengeStatusDto>("api/v1/student/daily-challenge", ct); }
        catch { return null; }
    }

    // ─── Homework (Teacher) ────────────────────────
    public async Task<List<FSEdu.Shared.Contracts.Courses.TeacherHomeworkRowDto>> ListTeacherHomeworkAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<FSEdu.Shared.Contracts.Courses.TeacherHomeworkRowDto>>("api/v1/teacher/homework", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<FSEdu.Shared.Contracts.Courses.TeacherHomeworkDetailDto?> GetTeacherHomeworkDetailAsync(Guid id, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<FSEdu.Shared.Contracts.Courses.TeacherHomeworkDetailDto>($"api/v1/teacher/homework/{id}", ct); }
        catch { return null; }
    }

    public async Task<(Guid? Id, ErrorResponse? Error)> CreateHomeworkAsync(FSEdu.Shared.Contracts.Courses.CreateHomeworkRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/teacher/homework", req, ct);
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<Guid>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<(bool ok, ErrorResponse? error)> UpdateHomeworkAsync(Guid id, FSEdu.Shared.Contracts.Courses.UpdateHomeworkRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"api/v1/teacher/homework/{id}", req, ct);
        if (resp.IsSuccessStatusCode) return (true, null);
        return (false, await ReadErrorAsync(resp, ct));
    }

    public async Task<bool> ToggleHomeworkPublishAsync(Guid id, bool publish, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/teacher/homework/{id}/{(publish ? "publish" : "unpublish")}", null, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteHomeworkAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/v1/teacher/homework/{id}", ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<(bool ok, ErrorResponse? error)> GradeHomeworkSubmissionAsync(Guid submissionId, decimal score, string? feedback, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/teacher/homework/submissions/{submissionId}/grade",
            new FSEdu.Shared.Contracts.Courses.GradeHomeworkSubmissionRequest(score, feedback), ct);
        if (resp.IsSuccessStatusCode) return (true, null);
        return (false, await ReadErrorAsync(resp, ct));
    }

    // ─── Homework (Student) ────────────────────────
    public async Task<List<FSEdu.Shared.Contracts.Courses.StudentHomeworkRowDto>> ListMyHomeworkAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<FSEdu.Shared.Contracts.Courses.StudentHomeworkRowDto>>("api/v1/student/homework", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<FSEdu.Shared.Contracts.Courses.StudentHomeworkDetailDto?> GetMyHomeworkDetailAsync(Guid id, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<FSEdu.Shared.Contracts.Courses.StudentHomeworkDetailDto>($"api/v1/student/homework/{id}", ct); }
        catch { return null; }
    }

    public async Task<(bool ok, ErrorResponse? error)> SubmitMyHomeworkAsync(Guid id, string? body, string? attachmentUrl, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/student/homework/{id}/submit",
            new FSEdu.Shared.Contracts.Courses.SubmitHomeworkRequest(body, attachmentUrl), ct);
        if (resp.IsSuccessStatusCode) return (true, null);
        return (false, await ReadErrorAsync(resp, ct));
    }

    // ─── Past Papers ───────────────────────────────
    public async Task<List<FSEdu.Shared.Contracts.Assessments.PastPaperListItemDto>> BrowsePastPapersAsync(
        int? subjectId = null, int? stageId = null, int? year = null, string? term = null, string? examType = null,
        CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (subjectId.HasValue) qs.Add($"subjectId={subjectId.Value}");
        if (stageId.HasValue) qs.Add($"stageId={stageId.Value}");
        if (year.HasValue) qs.Add($"year={year.Value}");
        if (!string.IsNullOrEmpty(term)) qs.Add($"term={Uri.EscapeDataString(term)}");
        if (!string.IsNullOrEmpty(examType)) qs.Add($"examType={Uri.EscapeDataString(examType)}");
        var query = qs.Count > 0 ? "?" + string.Join("&", qs) : "";
        try { return await _http.GetFromJsonAsync<List<FSEdu.Shared.Contracts.Assessments.PastPaperListItemDto>>("api/v1/past-papers" + query, ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<FSEdu.Shared.Contracts.Assessments.PastPaperBrowseFiltersDto?> GetPastPaperFiltersAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<FSEdu.Shared.Contracts.Assessments.PastPaperBrowseFiltersDto>("api/v1/past-papers/filters", ct); }
        catch { return null; }
    }

    public async Task<(FSEdu.Shared.Contracts.Assessments.StartPastPaperAttemptResponse? Data, ErrorResponse? Error)> StartPastPaperAttemptAsync(Guid paperId, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/past-papers/{paperId}/attempts", null, ct);
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<FSEdu.Shared.Contracts.Assessments.StartPastPaperAttemptResponse>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<(FSEdu.Shared.Contracts.Assessments.PastPaperAttemptResultDto? Data, ErrorResponse? Error)> SubmitPastPaperAttemptAsync(
        FSEdu.Shared.Contracts.Assessments.SubmitPastPaperAttemptRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/past-papers/attempts/submit", req, ct);
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<FSEdu.Shared.Contracts.Assessments.PastPaperAttemptResultDto>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<FSEdu.Shared.Contracts.Assessments.PastPaperAttemptResultDto?> GetPastPaperAttemptAsync(Guid attemptId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<FSEdu.Shared.Contracts.Assessments.PastPaperAttemptResultDto>($"api/v1/past-papers/attempts/{attemptId}", ct); }
        catch { return null; }
    }

    // ─── TOTP 2FA ──────────────────────────────────
    public async Task<FSEdu.Shared.Contracts.Security.TotpStatusDto?> GetTotpStatusAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<FSEdu.Shared.Contracts.Security.TotpStatusDto>("api/v1/security/totp/status", ct); }
        catch { return null; }
    }

    public async Task<(FSEdu.Shared.Contracts.Security.TotpSetupDto? Data, ErrorResponse? Error)> BeginTotpSetupAsync(CancellationToken ct = default)
    {
        var resp = await _http.PostAsync("api/v1/security/totp/setup", null, ct);
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<FSEdu.Shared.Contracts.Security.TotpSetupDto>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<(FSEdu.Shared.Contracts.Security.TotpEnabledDto? Data, ErrorResponse? Error)> ConfirmTotpSetupAsync(string code, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/security/totp/confirm",
            new FSEdu.Shared.Contracts.Security.TotpVerifyRequest(code), ct);
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<FSEdu.Shared.Contracts.Security.TotpEnabledDto>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<(bool ok, ErrorResponse? error)> DisableTotpAsync(string code, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/security/totp/disable",
            new FSEdu.Shared.Contracts.Security.TotpVerifyRequest(code), ct);
        if (resp.IsSuccessStatusCode) return (true, null);
        return (false, await ReadErrorAsync(resp, ct));
    }

    public async Task<(FSEdu.Shared.Contracts.Security.TotpEnabledDto? Data, ErrorResponse? Error)> RegenerateTotpRecoveryAsync(string code, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/security/totp/recovery/regenerate",
            new FSEdu.Shared.Contracts.Security.TotpVerifyRequest(code), ct);
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<FSEdu.Shared.Contracts.Security.TotpEnabledDto>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    // ─── Referral ──────────────────────────────────
    public async Task<FSEdu.Shared.Contracts.Students.MyReferralInfoDto?> GetMyReferralInfoAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<FSEdu.Shared.Contracts.Students.MyReferralInfoDto>("api/v1/student/referral", ct); }
        catch { return null; }
    }

    public async Task<(bool ok, ErrorResponse? error)> RedeemReferralCodeAsync(string code, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/student/referral/redeem",
            new FSEdu.Shared.Contracts.Students.RedeemReferralCodeRequest(code), ct);
        if (resp.IsSuccessStatusCode) return (true, null);
        return (false, await ReadErrorAsync(resp, ct));
    }

    // ─── Avatar ────────────────────────────────────
    public async Task<(string? url, ErrorResponse? error)> UploadAvatarAsync(System.IO.Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
        content.Add(streamContent, "file", fileName);
        var resp = await _http.PostAsync("api/v1/me/avatar/upload", content, ct);
        if (!resp.IsSuccessStatusCode) return (null, await ReadErrorAsync(resp, ct));
        var dto = await resp.Content.ReadFromJsonAsync<UploadAvatarResponse>(cancellationToken: ct);
        return (dto?.Url, null);
    }

    public async Task<bool> SetMyAvatarAsync(string? url, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync("api/v1/me/avatar", new { Url = url }, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveMyAvatarAsync(CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync("api/v1/me/avatar", ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<ContinueWatchingItemDto>> GetContinueWatchingAsync(int take = 6, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<ContinueWatchingItemDto>>($"api/v1/student/continue-watching?take={take}", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<List<CourseListItemDto>> GetCourseRecommendationsAsync(int take = 6, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<CourseListItemDto>>($"api/v1/student/recommendations?take={take}", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<StudentScheduleResponse?> GetMyScheduleAsync(int days = 14, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<StudentScheduleResponse>($"api/v1/student/schedule?days={days}", ct); }
        catch { return null; }
    }

    public async Task<List<ActivityItemDto>> GetMyActivityFeedAsync(int take = 30, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<ActivityItemDto>>($"api/v1/student/activity?take={take}", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<StudentDetailedStatsDto?> GetMyDetailedStatsAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<StudentDetailedStatsDto>("api/v1/student/detailed-stats", ct); }
        catch { return null; }
    }

    public async Task<DailyMissionDto?> GetDailyMissionAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<DailyMissionDto>("api/v1/student/daily-mission", ct); }
        catch { return null; }
    }

    public async Task<List<SuggestionDto>> GetMySuggestionsAsync(int take = 4, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<SuggestionDto>>($"api/v1/student/suggestions?take={take}", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<FSEdu.Shared.Contracts.Teachers.TeacherProfileDto?> GetTeacherProfileAsync(Guid teacherId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<FSEdu.Shared.Contracts.Teachers.TeacherProfileDto>($"api/v1/teachers/{teacherId}", ct);

    public async Task<bool> UpdateTeacherProfileAsync(string? bio, int years, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync("api/v1/teacher/profile",
            new FSEdu.Shared.Contracts.Teachers.UpdateTeacherProfileRequest(bio, years), ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<long?> AddTeacherQualificationAsync(string title, string institution, int year, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/teacher/profile/qualifications",
            new FSEdu.Shared.Contracts.Teachers.AddTeacherQualificationRequest(title, institution, year), ct);
        if (!resp.IsSuccessStatusCode) return null;
        try { return await resp.Content.ReadFromJsonAsync<long>(cancellationToken: ct); }
        catch { return 0L; }
    }

    public async Task<bool> DeleteTeacherQualificationAsync(long qualId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"api/v1/teacher/profile/qualifications/{qualId}", ct);
        return resp.IsSuccessStatusCode;
    }

    // ─── Admin overview ─────────────────────────────────
    public async Task<AdminOverviewDto?> GetAdminOverviewAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<AdminOverviewDto>("api/v1/admin/overview", ct);

    public async Task<(int? recipients, ErrorResponse? error)> BroadcastNotificationAsync(
        BroadcastNotificationRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/admin/notifications/broadcast", req, ct);
        if (resp.IsSuccessStatusCode)
        {
            try { return (await resp.Content.ReadFromJsonAsync<int>(cancellationToken: ct), null); }
            catch { return (0, null); }
        }
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<List<CouponDto>> GetCouponsListAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<CouponDto>>("api/v1/admin/coupons", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<(int? id, ErrorResponse? error)> CreateCouponAsync(CreateCouponRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/admin/coupons", req, ct);
        if (resp.IsSuccessStatusCode)
        {
            try { return (await resp.Content.ReadFromJsonAsync<int>(cancellationToken: ct), null); }
            catch { return (0, null); }
        }
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<bool> DeactivateCouponAsync(int couponId, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/admin/coupons/{couponId}/deactivate", content: null, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<SalesCampaignDto>> GetCampaignsAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<SalesCampaignDto>>("api/v1/admin/campaigns", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<(int? id, ErrorResponse? error)> CreateCampaignAsync(CreateCampaignRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/admin/campaigns", req, ct);
        if (resp.IsSuccessStatusCode)
        {
            try { return (await resp.Content.ReadFromJsonAsync<int>(cancellationToken: ct), null); }
            catch { return (0, null); }
        }
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<bool> SetCampaignActiveAsync(int campaignId, bool active, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/admin/campaigns/{campaignId}/set-active",
            new { Active = active }, ct);
        return resp.IsSuccessStatusCode;
    }

    // ─── Discount Rules (admin) ────────────────────────
    public async Task<List<DiscountRuleDto>> GetDiscountRulesAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<DiscountRuleDto>>("api/v1/admin/discount-rules", ct) ?? new(); }
        catch { return new(); }
    }

    public async Task<(int? id, ErrorResponse? error)> CreateDiscountRuleAsync(CreateDiscountRuleRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/admin/discount-rules", req, ct);
        if (resp.IsSuccessStatusCode)
        {
            try { return (await resp.Content.ReadFromJsonAsync<int>(cancellationToken: ct), null); }
            catch { return (0, null); }
        }
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<bool> SetDiscountRuleActiveAsync(int ruleId, bool active, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/admin/discount-rules/{ruleId}/set-active",
            new { Active = active }, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<AuditLogResponse?> GetAuditLogAsync(string? action = null, Guid? actor = null,
        DateTime? fromUtc = null, DateTime? toUtc = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(action)) qs.Add($"action={Uri.EscapeDataString(action)}");
        if (actor.HasValue) qs.Add($"actor={actor}");
        if (fromUtc.HasValue) qs.Add($"from={fromUtc.Value:O}");
        if (toUtc.HasValue) qs.Add($"to={toUtc.Value:O}");
        try { return await _http.GetFromJsonAsync<AuditLogResponse>("api/v1/admin/audit-log?" + string.Join("&", qs), ct); }
        catch { return null; }
    }

    // ─── Universal search ───────────────────────────────
    public async Task<FSEdu.Shared.Contracts.Search.SearchResponseDto?> SearchAsync(
        string q, int per = 10, CancellationToken ct = default)
    {
        var url = $"api/v1/search?q={Uri.EscapeDataString(q)}&per={per}";
        return await _http.GetFromJsonAsync<FSEdu.Shared.Contracts.Search.SearchResponseDto>(url, ct);
    }

    // ─── Web Push ───────────────────────────────────────
    public sealed record VapidKeyResponse(string PublicKey, bool Enabled);
    public async Task<VapidKeyResponse?> GetVapidKeyAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<VapidKeyResponse>("api/v1/push/vapid-key", ct);

    public async Task<(Guid? Id, ErrorResponse? Error)> ScheduleLiveSessionAsync(ScheduleLiveSessionRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/teacher/live", req, ct);
        if (resp.IsSuccessStatusCode) return (await resp.Content.ReadFromJsonAsync<Guid>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<ErrorResponse?> StartLiveSessionAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/teacher/live/{id}/start", null, ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    public async Task<ErrorResponse?> EndLiveSessionAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/teacher/live/{id}/end", null, ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    public async Task<ErrorResponse?> CancelLiveSessionAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/teacher/live/{id}/cancel", null, ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    public async Task<ErrorResponse?> EditLiveSessionAsync(Guid id, EditLiveSessionRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"api/v1/teacher/live/{id}", req, ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    public async Task<(Guid? newId, ErrorResponse? error)> RescheduleLiveSessionAsync(
        Guid id, DateTime newScheduledAtUtc, int? newDurationMinutes = null, string? titleOverride = null, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/teacher/live/{id}/reschedule",
            new { NewScheduledAtUtc = newScheduledAtUtc, NewDurationMinutes = newDurationMinutes, TitleOverride = titleOverride }, ct);
        if (resp.IsSuccessStatusCode)
        {
            try { return (await resp.Content.ReadFromJsonAsync<Guid>(ct), null); }
            catch { return (Guid.Empty, null); }
        }
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<(Guid[]? ids, ErrorResponse? error)> BulkScheduleLiveSessionsAsync(
        int subjectId, int stageId, string titlePattern, string? description,
        DateTime firstStartUtc, int durationMinutes, int occurrences, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("api/v1/teacher/live/bulk", new
        {
            SubjectId = subjectId, StageId = stageId,
            TitlePattern = titlePattern, Description = description,
            FirstStartUtc = firstStartUtc, DurationMinutes = durationMinutes,
            Occurrences = occurrences
        }, ct);
        if (resp.IsSuccessStatusCode)
        {
            try { return (await resp.Content.ReadFromJsonAsync<Guid[]>(ct), null); }
            catch { return (Array.Empty<Guid>(), null); }
        }
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<(JoinLiveResponse? Data, ErrorResponse? Error)> JoinLiveSessionAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/live/{id}/join", null, ct);
        if (resp.IsSuccessStatusCode) return (await resp.Content.ReadFromJsonAsync<JoinLiveResponse>(ct), null);
        return (null, await ReadErrorAsync(resp, ct));
    }

    public async Task<ErrorResponse?> StartRecordingAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/teacher/live/{id}/record/start", null, ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    public async Task<ErrorResponse?> StopRecordingAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/teacher/live/{id}/record/stop", null, ct);
        return resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
    }

    public string? GetCurrentToken() =>
        _http.DefaultRequestHeaders.Authorization?.Parameter;

    // ─── Gamification ──────────────────────────────────
    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int? stageId = null, int take = 50, CancellationToken ct = default)
    {
        var qs = $"take={take}" + (stageId.HasValue ? $"&stageId={stageId}" : "");
        return await _http.GetFromJsonAsync<List<LeaderboardEntryDto>>($"api/v1/leaderboard?{qs}", ct) ?? new();
    }

    public async Task<MyAchievementsDto?> GetMyAchievementsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<MyAchievementsDto>("api/v1/student/achievements", ct);

    public async Task<bool> TrackLessonViewAsync(Guid lessonId, int positionSec, decimal watchedPct, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/v1/student/lessons/{lessonId}/track",
            new { lastPositionSec = positionSec, watchedPct }, ct);
        return resp.IsSuccessStatusCode;
    }

    private static async Task<ErrorResponse> ReadErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return new ErrorResponse("AUTH.REQUIRED", "يجب تسجيل الدخول — الجلسة منتهية");
        if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return new ErrorResponse("AUTH.FORBIDDEN", "ليست لديك صلاحية للوصول");

        try
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(body))
                return new ErrorResponse("HTTP." + (int)resp.StatusCode, $"خطأ من الخادم ({(int)resp.StatusCode})");

            try
            {
                var err = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(body,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (err is not null && !string.IsNullOrEmpty(err.Message)) return err;
            }
            catch { }

            return new ErrorResponse("HTTP." + (int)resp.StatusCode, body.Length > 200 ? body[..200] : body);
        }
        catch
        {
            return new ErrorResponse("UNKNOWN", "خطأ غير متوقع");
        }
    }

    public async Task<List<CourseListItemDto>> GetPendingCoursesAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<CourseListItemDto>>("api/v1/admin/courses/pending", ct) ?? new();

    public async Task<bool> ApproveCourseAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/admin/courses/{id}/approve", null, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> RejectCourseAsync(Guid id, string? reason, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"api/v1/admin/courses/{id}/reject?reason={Uri.EscapeDataString(reason ?? "")}", null, ct);
        return resp.IsSuccessStatusCode;
    }
}
