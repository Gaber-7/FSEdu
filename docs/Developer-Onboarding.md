# FSEdu — دليل المبرمج الشامل (Developer Onboarding)

> **الجمهور المستهدف:** مبرمج جديد ينضم للفريق
> **المدة المتوقّعة للقراءة:** 45–60 دقيقة
> **متطلبات مسبقة:** خبرة بـ .NET, EF Core, Blazor, SQL Server

---

## المحتويات

1. [نظرة عامة على المشروع](#1-نظرة-عامة)
2. [البنية المعمارية (Clean Architecture)](#2-البنية-المعمارية)
3. [إعداد بيئة التطوير](#3-إعداد-بيئة-التطوير)
4. [هيكل قاعدة البيانات](#4-قاعدة-البيانات)
5. [نظام الـ Migrations](#5-نظام-الـ-migrations)
6. [الميزات المُنجزة (ما تمّ)](#6-الميزات-المنجزة)
7. [الميزات المتبقّية (ما لم يُنجز)](#7-الميزات-المتبقية)
8. [أنماط الكود الرئيسية](#8-أنماط-الكود)
9. [البث المباشر (LiveKit)](#9-البث-المباشر-livekit)
10. [السبورة التفاعلية (Interactive Whiteboard)](#10-السبورة-التفاعلية)
11. [الاشتراكات والإعدادات الخارجية](#11-الاشتراكات-والإعدادات-الخارجية)
12. [أوامر يومية شائعة](#12-أوامر-يومية-شائعة)
13. [مواقع مهمة فى الكود (Cheatsheet)](#13-مواقع-مهمة)

---

## 1. نظرة عامة

**FSEdu** منصّة تعليمية عربية للطلبة فى المرحلتين الابتدائية والإعدادية بمصر. المنصّة تدعم:

- **تسجيل + اشتراكات** بأنواع متعدّدة (شهرى، ترم، مادة كاملة، مرحلة كاملة، نقدى/إلكترونى)
- **دورات مسجّلة** بفصول ودروس مع تشغيل فيديو + سرعات مختلفة + ملاحظات + علامات وقتية
- **حصص مباشرة** (Live Sessions) عبر LiveKit مع سبورة تفاعلية، دردشة، رفع يد، تصويت
- **اختبارات وواجبات** مع تصحيح تلقائى للـ MCQ + تصحيح يدوى للمقالى
- **امتحانات سابقة** (Past Papers) — بنك للامتحانات السابقة كموارد استعداد للامتحانات
- **نظام gamification** (XP، Streaks، Shields، Badges، Leaderboard، Daily Challenges)
- **إشعارات** متعدّدة القنوات (in-app, Web Push, Email)
- **دعم متعدّد الأدوار**: Student, Parent, Teacher, Admin
- **مصادقة ثنائية (TOTP)** + استرداد بالرموز الاحتياطية
- **نظام إحالة (Refer-a-Friend)** برصيد + خصومات آلية

### التقنيات المستخدمة

| الطبقة | التقنية |
|--------|---------|
| Runtime | .NET 9 |
| API | ASP.NET Core Minimal APIs |
| Frontend | Blazor Server (Interactive) + Blazor WebAssembly Client |
| ORM | Entity Framework Core 9 (Code-First, Manual Migrations) |
| Database | SQL Server (default schema: `edu`, identity schema: `auth`) |
| Auth | ASP.NET Identity + JWT Bearer + Refresh Tokens |
| Real-time | SignalR (Classroom Hub) |
| Live Video | LiveKit Cloud (WebRTC SFU) + Server-side Egress للتسجيل |
| Notifications | Web Push (VAPID) + SMTP Email + In-App |
| CQRS | MediatR (ICommand/IQuery + Handlers) |
| Validation | FluentValidation |
| PDF | QuestPDF (Community License) |
| Result Pattern | Custom `Result<T>` (no exceptions for business rules) |

---

## 2. البنية المعمارية

المشروع مبنى على **Clean Architecture** مع فصل واضح بين الطبقات:

```
src/
├── BuildingBlocks/                    ← مكتبات مشتركة (Kernel + Contracts)
│   ├── FSEdu.Shared.Kernel/           ← Primitives, Result<T>, Time abstraction
│   └── FSEdu.Shared.Contracts/        ← DTOs (لا تعرف أى شيء عن الدومين)
│
├── Core/
│   ├── FSEdu.Domain/                  ← الأغنى بالمعنى: entities, value objects, enums
│   │   ├── Academic/     (Subject, Stage, School, Region)
│   │   ├── Assessments/  (Question, Assessment, PastPaper)
│   │   ├── Audit/        (AuditEntry)
│   │   ├── Billing/      (Plan, Subscription, Payment, Coupon, DiscountRule)
│   │   ├── Common/       (Enums, IAuditable, ISoftDelete, ValueObjects)
│   │   ├── Courses/      (Course, Chapter, Lesson, Enrollment, Homework, Certificate, ...)
│   │   ├── Engagement/   (Badge, WeeklyChallenge, DailyChallenge, Community, ...)
│   │   ├── LiveClassroom/(LiveSession, LiveAttendance)
│   │   ├── Notifications/(Notification, PushSubscription, NotificationPreference)
│   │   ├── Support/      (AskTicket, TicketReply)
│   │   └── Users/        (AppUser, Student, Teacher, Parent, Referral, ...)
│   │
│   └── FSEdu.Application/             ← Use cases + abstractions (لا تعتمد على Infra)
│       ├── Abstractions/  (interfaces فقط: IApplicationDbContext, ICurrentUser, ...)
│       └── Features/      (Handlers مقسّمة حسب الـ feature)
│
├── Infrastructure/
│   ├── FSEdu.Persistence/             ← EF Core: DbContext, Configurations, Migrations
│   ├── FSEdu.Identity/                ← ASP.NET Identity + JWT
│   └── FSEdu.Infrastructure/          ← Services (Push, Email, Notifications, Gamification)
│
└── Presentation/
    ├── FSEdu.Api/                     ← ASP.NET Core Minimal APIs + SignalR Hubs + LiveKit
    ├── FSEdu.Web/                     ← Blazor Server (Interactive)
    ├── FSEdu.Web.Client/              ← Blazor WebAssembly (client-only components)
    └── FSEdu.Web.Shared/              ← مكوّنات مشتركة بين Server + WASM
```

### القواعد الذهبية
1. **Domain لا يعرف أى شيء عن Infrastructure** — لا EF, لا HttpClient, لا Logger
2. **Application** يعرّف الـ interfaces فقط، وInfrastructure يطبّقها
3. **Contracts** خالى من الاعتماد على Domain (يستخدم primitives + Guid + DateTime)
4. **Presentation** لا يستدعى Domain مباشرة؛ يستخدم MediatR → Handlers → Domain
5. **Result Pattern** فى كل مكان — لا نرمى exceptions للأخطاء التجارية (validation, not found, forbidden)

---

## 3. إعداد بيئة التطوير

### المتطلبات
- **.NET 9 SDK**
- **SQL Server** (Express أو Developer Edition) — الـ dev connection string يشير إلى `.\MSSQLSERVER2` وقاعدة `FSEdu_Dev`
- **Visual Studio 2022 / Rider / VS Code**
- **dotnet-ef** أداة CLI:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

### خطوات التشغيل الأولى
```bash
# 1. استعادة الحزم
dotnet restore

# 2. تطبيق كل الـ migrations (يُنشئ قاعدة البيانات إذا لم تكن موجودة)
dotnet ef database update \
    --project src/Infrastructure/FSEdu.Persistence/FSEdu.Persistence.csproj \
    --startup-project src/Presentation/FSEdu.Api/FSEdu.Api.csproj \
    --context ApplicationDbContext

dotnet ef database update \
    --project src/Infrastructure/FSEdu.Identity/FSEdu.Identity.csproj \
    --startup-project src/Presentation/FSEdu.Api/FSEdu.Api.csproj \
    --context IdentityDbContext

# 3. تشغيل الـ API (سيُنفّذ الـ Seeders تلقائيًا فى Dev)
dotnet run --project src/Presentation/FSEdu.Api/FSEdu.Api.csproj

# 4. تشغيل الـ Web فى تيرمينال ثانى
dotnet run --project src/Presentation/FSEdu.Web/FSEdu.Web.csproj
```

### الـ Seed Data الجاهزة (Dev فقط)
- **مراحل دراسية** (ابتدائى 1–6، إعدادى 1–3)
- **مواد** (عربى، رياضيات، إنجليزى، علوم، دراسات، ...)
- **مناطق** (المحافظات المصرية)
- **خطط اشتراك** + أسعار
- **شارات (Badges)**
- **مستخدمين تجريبيين** (طالب، معلّم، ولى أمر، أدمن)
- **محتوى تجريبى** (دورات، دروس، اختبارات)

---

## 4. قاعدة البيانات

### الـ Schemas
- **`edu`** — الـ business tables (Users, Courses, Subscriptions, ...)
- **`auth`** — ASP.NET Identity tables + RefreshTokens

### الجداول الرئيسية (نظرة سريعة)

#### 👥 المستخدمين
| جدول | ملاحظات |
|------|---------|
| `Users` | TPH (Table-Per-Hierarchy) لـ AppUser: Student, Teacher, Parent مع discriminator `UserType`. يحتوى TOTP + Referral fields أيضًا |
| `ParentStudentLinks` | ربط ولى الأمر بالطلبة (many-to-many مع Relation type) |
| `TeacherSubjects`, `TeacherRegions`, `TeacherQualifications` | ملفّ المعلّم |
| `Referrals` | كل عملية إحالة (referrer, referred, rewardEgp, triggerSubId) |

#### 🎓 الأكاديمى
| جدول | ملاحظات |
|------|---------|
| `Stages`, `Subjects`, `Schools`, `Regions` | reference data |
| `Courses` | دورات المعلّمين (soft-delete, published/draft) |
| `Chapters`, `Lessons` | فصول ودروس + Type (Recorded, Live, Document, Quiz) |
| `LessonAttachments` | مرفقات الدرس |
| `Enrollments` | تسجيل الطالب فى الدورة |
| `LessonProgress` | تتبّع المشاهدة (PositionSec, WatchedPct, Completed) |
| `CourseReviews`, `LessonQuestions`, `LessonQuestionVotes`, `LessonReactions` | تفاعل |
| `LessonNotes`, `LessonMarkers` | ملاحظات وعلامات وقتية |
| `CourseBookmarks`, `CourseAnnouncements`, `CourseDiscussions`, `CourseDiscussionReplies` | تفاعل مجتمعى |
| `Homeworks`, `HomeworkSubmissions` | نظام الواجبات |
| `Certificates` | شهادات إكمال الدورة (مع رقم شهادة ثابت) |

#### 📝 الاختبارات
| جدول | ملاحظات |
|------|---------|
| `Questions` | بنك أسئلة قابل لإعادة الاستخدام |
| `Assessments`, `AssessmentQuestions`, `AssessmentAttempts` | اختبارات المعلّمين |
| `PastPapers`, `PastPaperQuestions`, `PastPaperAttempts` | بنك امتحانات سابقة (منفصل عن Assessments) |

#### 💰 الفوترة
| جدول | ملاحظات |
|------|---------|
| `Plans` | خطط الاشتراك |
| `Subscriptions` | اشتراكات الطلبة (SubjectMonthly, SubjectTerm, StageFullTerm) |
| `Payments` | مدفوعات + Status (Pending/AwaitingReview/Succeeded/Failed) + PaymentMethod (InstaPay/VodafoneCash/BankTransfer/**Cash**) |
| `Coupons`, `SalesCampaigns`, `DiscountRules` | نظام الخصومات (3 طبقات: campaign → rule → coupon) |

#### 🎮 الـ Gamification
| جدول | ملاحظات |
|------|---------|
| `Badges`, `StudentBadges` | شارات |
| `WeeklyChallenges`, `DailyChallenges` | تحدّيات دورية |
| `Communities`, `CommunityMembers` | مجموعات دراسية |

#### 🎥 الحصص المباشرة
| جدول | ملاحظات |
|------|---------|
| `LiveSessions` | حصص (Scheduled, Live, Ended, Cancelled) + RoomId للـ LiveKit + RecordingUrl |
| `LiveAttendance` | حضور الطالب (JoinedAt, LeftAt, TotalDurationSec) — يُحتسب "معتمد" لو ≥ 50% من مدة الحصة |

#### 🔔 الإشعارات
| جدول | ملاحظات |
|------|---------|
| `Notifications` | إشعارات in-app |
| `PushSubscriptions` | Web Push endpoints (VAPID) |
| `NotificationPreferences` | تفضيلات المستخدم لكل قناة |

#### 🎫 الدعم
| جدول | ملاحظات |
|------|---------|
| `AskTickets`, `TicketReplies` | تذاكر الدعم "اسأل مدرّس" |

#### 📊 التدقيق
| جدول | ملاحظات |
|------|---------|
| `AuditEntries` | سجلّ كل العمليات الإدارية |

### الأنماط المشتركة
- **Soft Delete**: `DeletedAtUtc` + `HasQueryFilter(x => x.DeletedAtUtc == null)` على معظم الجداول
- **Auditing**: `CreatedAtUtc`, `CreatedBy`, `UpdatedAtUtc`, `UpdatedBy` تُملأ تلقائيًا عبر `AuditableEntitiesInterceptor`
- **Value Objects**: `Email`, `PhoneNumber` مستخدمة كـ EF Owned Types

---

## 5. نظام الـ Migrations

نستخدم **manual migrations** (نكتب ملف الـ migration بأيدينا) بدلًا من `dotnet ef migrations add`. السبب:
- الـ API يظل يعمل أثناء التطوير ويحمل ملفّات الـ DLL → أى محاولة `ef migrations add` تفشل بـ "file locked"
- الـ auto-generated snapshot ينكسر بسهولة عند التعديل اليدوى

### هيكل ملف الـ migration
كل migration يتكوّن من ملفّين:

1. **`YYYYMMDDHHMMSS_Name.cs`** — يحتوى على الـ `Up()` و `Down()` methods
2. **`YYYYMMDDHHMMSS_Name.Designer.cs`** — Designer minimal (لا نستخدم الـ ModelSnapshot)

### مثال إضافة عمود جديد
```csharp
// 20260615120000_AddNewColumn.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEdu.Persistence.Migrations
{
    public partial class AddNewColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewField",
                schema: "edu",
                table: "SomeTable",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "NewField", schema: "edu", table: "SomeTable");
        }
    }
}
```

```csharp
// 20260615120000_AddNewColumn.Designer.cs
// <auto-generated />
using FSEdu.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSEdu.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260615120000_AddNewColumn")]
    partial class AddNewColumn
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Hand-written migration.
        }
    }
}
```

### تطبيق الـ Migrations
```bash
# للـ ApplicationDbContext
dotnet ef database update \
    --project src/Infrastructure/FSEdu.Persistence/FSEdu.Persistence.csproj \
    --startup-project src/Presentation/FSEdu.Api/FSEdu.Api.csproj \
    --context ApplicationDbContext

# للـ IdentityDbContext (نادرًا ما نضيف حاجة هنا)
dotnet ef database update \
    --project src/Infrastructure/FSEdu.Identity/FSEdu.Identity.csproj \
    --startup-project src/Presentation/FSEdu.Api/FSEdu.Api.csproj \
    --context IdentityDbContext
```

فى الـ Dev، الـ API نفسه يستدعى `Database.MigrateAsync()` عند البدء (من `DataSeeder.SeedAsync`).

### ⚠️ فخّ شائع: `nvarchar` size
SQL Server يقبل `nvarchar` بحجم من 1 إلى **4000** فقط. لأى نصّ أكبر لازم `nvarchar(max)`.
```csharp
// ❌ خطأ — سيفشل بـ "The size (8000) exceeds the maximum allowed (4000)"
b.Property(x => x.Body).HasMaxLength(8000);

// ✅ صحيح
b.Property(x => x.Body).HasColumnType("nvarchar(max)");
```

---

## 6. الميزات المُنجزة

### ✅ الأساسيات
- [x] تسجيل + تسجيل دخول (Student, Teacher, Parent, Admin)
- [x] JWT + Refresh Tokens
- [x] OTP للتحقق من رقم الهاتف (SMS)
- [x] رفع Avatar
- [x] تعديل الملف الشخصى (بما فيه رقم الواتساب)

### ✅ الاشتراكات + الفوترة
- [x] 3 أنواع اشتراك (SubjectMonthly, SubjectTerm, StageFullTerm)
- [x] 4 طرق دفع: InstaPay, VodafoneCash, BankTransfer, **Cash**
- [x] رفع إيصال + مراجعة إدارية + تفعيل تلقائى بعد الموافقة
- [x] Coupons + SalesCampaigns + **DiscountRules** (6 أنواع: SiblingsCount, SubjectsCount, LongDuration, HighAchiever, ReturningStudent, ReferralReferee)
- [x] ترتيب الخصومات: Campaign → Rule → Coupon → ReferralCredit

### ✅ الدورات + التعلّم
- [x] المدرّس ينشئ دورة + فصول + دروس (Recorded/Live/Document/Quiz)
- [x] رفع Preview Video للدورة
- [x] تشغيل الدرس + تتبّع التقدّم + استئناف
- [x] **سرعات تشغيل** (0.5x → 2x)
- [x] **ملاحظات + علامات وقتية** على الدرس
- [x] **أسئلة على الدرس** (Q&A) + تصويت
- [x] Reactions + Bookmarks + Announcements + Discussions
- [x] **إحصائيات مفصّلة للطالب** (دقائق مشاهدة إجمالًا وتفصيلًا بالمادة)
- [x] **إحصائيات المعلّم** (بما فيه الحضور المعتمد ≥ 50% من مدة الحصة)
- [x] **شهادات إكمال** (persisted, printable + PDF export via QuestPDF)

### ✅ الحصص المباشرة (LiveKit) — التفاصيل فى القسم 9
- [x] جدولة حصص مباشرة
- [x] بثّ صوت + فيديو + مشاركة شاشة (WebRTC عبر LiveKit)
- [x] تسجيل الحصص (Server-side Egress)
- [x] Webhook لاستقبال الملف بعد انتهاء التسجيل
- [x] Recording composer فى المتصفّح (كخطة بديلة)

### ✅ السبورة التفاعلية — التفاصيل فى القسم 10
- [x] المعلّم يرسم على canvas
- [x] بثّ الرسم فورًا لكل الطلبة عبر SignalR
- [x] Late joiners يستقبلون snapshot من الرسم
- [x] أدوات: قلم بألوان وأحجام + ممحاة + مسح كامل

### ✅ الاختبارات + الواجبات + الامتحانات السابقة
- [x] بنك أسئلة قابل لإعادة الاستخدام (6 أنواع أسئلة)
- [x] اختبارات مع تصحيح تلقائى + محاولات متعدّدة
- [x] **نظام واجبات كامل** (المعلّم يعيّن → الطالب يسلّم → المعلّم يصحّح مع تعليق)
- [x] **بنك امتحانات سابقة** (فلترة بـ مرحلة/مادة/سنة/ترم/نوع؛ محاولة موقّتة؛ نتيجة + مراجعة)

### ✅ الـ Gamification
- [x] XP + Streaks + Longest Streak + **Streak Shields** (7 shields max)
- [x] Badges (Week Streak, Month Streak, ...)
- [x] **Daily Challenges** (WatchMinutes, CompleteLessons, SubmitHomework, SolveAssessment, SolvePastPaper)
- [x] Leaderboards
- [x] Weekly Challenges

### ✅ نظام الإحالة (Refer-a-Friend)
- [x] كل طالب له كود إحالة فريد (يُولّد بشكل حتمى من Id)
- [x] المدعو يُدخل الكود → عند أوّل دفعة معتمدة، الطرفان يحصلان على مكافأة
- [x] **رصيد جنيهات** (ReferralCredits) قابل للاستخدام فى الاشتراك التالى

### ✅ التقارير + الإشعارات
- [x] Web Push (VAPID)
- [x] Email notifications عبر SMTP (fanout من `NotificationService`)
- [x] **تقرير أسبوعى لولى الأمر** (Saturday 09–11 UTC)
- [x] تفضيلات إشعارات لكل فئة

### ✅ الأمان
- [x] **TOTP 2FA** (RFC 6238 self-contained implementation)
- [x] Recovery codes (10 codes, one-time-use, SHA256 hashed)
- [x] Login flow يدعم التحدّى فى خطوتين
- [x] شارة "🔐 2FA" على ملف المعلّم

### ✅ الإدارة
- [x] لوحة أدمن (dashboard)
- [x] مراجعة المدرّسين الجدد
- [x] مراجعة الدورات قبل النشر
- [x] مراجعة المدفوعات
- [x] إدارة Coupons + Campaigns + DiscountRules
- [x] بثّ إشعارات جماعية
- [x] سجلّ التدقيق (Audit Log)

### ✅ الدعم
- [x] تذاكر "اسأل مدرّس"
- [x] ردود + إسناد للمعلّم أو الأدمن

### ✅ i18n Infrastructure
- [x] `IStringLocalizer` + Resources folder
- [x] Cookie-based culture provider
- [x] زر تبديل اللغة (عربى/إنجليزى)
- [x] `dir="rtl/ltr"` تلقائى فى App.razor
- ⚠️ **الترجمة الفعلية لكل النصوص لم تتم بعد** — البنية موجودة فقط

### ✅ PWA
- [x] Service Worker + Manifest
- [x] "تثبيت التطبيق" prompt
- [x] Dark mode toggle

---

## 7. الميزات المتبقّية

### 🔴 عالية الأولوية
1. **ترجمة UI الكاملة للإنجليزية** — بنية i18n موجودة، لكن النصوص فى Blazor pages لا تزال مربوطة بالعربية hard-coded
2. **بوّابة دفع تلقائية** (Paymob / Fawry) — الحالى يدوى فقط
3. **AI Tutor / Solution Explainer** — يحتاج تكامل Claude API (Anthropic SDK)

### 🟡 متوسطة الأولوية
4. **مجموعات صفية (Class Groups)** — كود انضمام + ترتيب داخل الفصل
5. **Spaced Repetition Flashcards** — للحفظ (خوارزمية SM-2)
6. **Anti-cheating للاختبارات المؤقّتة** — كشف تبديل التابات، ترتيب عشوائى
7. **Homework Reminders** — إشعار للى ما سلّمش قبل الـ due date بيوم
8. **Class Leaderboard** (مرتبط بـ Class Groups)

### 🟢 منخفضة الأولوية / تحسينات
9. **Question Difficulty Heatmap** للمعلّم
10. **Engagement Decay Curve** — فى أى دقيقة الطلبة بيوقفوا المشاهدة
11. **Teacher Payout Dashboard** + Tax invoices
12. **Report Content Issues** — الطالب يبلّغ عن فيديو مكسور أو إجابة خطأ
13. **Data Export** (GDPR-style)
14. **Achievements/Milestones** ("First 100 XP", "First Certificate", ...)

### 📌 Known Warnings (لا تمنع البناء)
- EF Query Filter warnings على 12+ علاقة required (مثال: `Homework.Course` عنده soft-delete filter). حلّها: إما جعل الـ navigation optional أو إضافة نفس الـ filter على الـ dependent.

---

## 8. أنماط الكود

### 8.1 CQRS + MediatR
كل feature عبارة عن **Command أو Query + Handler**:

```csharp
// Query
public sealed record GetCourseDetailsQuery(Guid CourseId) : IQuery<CourseDetailDto>;

public sealed class GetCourseDetailsHandler : IQueryHandler<GetCourseDetailsQuery, CourseDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCourseDetailsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result<CourseDetailDto>> Handle(GetCourseDetailsQuery request, CancellationToken ct)
    {
        // 1. Auth check
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        // 2. Load + project
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Not found");

        // 3. Return
        return Result.Success(new CourseDetailDto(...));
    }
}
```

### 8.2 Result Pattern
لا نرمى exceptions للأخطاء التجارية:
```csharp
Result.Success(data)                       // 200
Error.NotFound(code, arMsg, enMsg)         // 404
Error.Forbidden(code, arMsg, enMsg)        // 403
Error.Unauthorized(code, arMsg, enMsg)     // 401
Error.Validation(code, arMsg, enMsg)       // 400
Error.Conflict(code, arMsg, enMsg)         // 409
```
كل Error له `code` ثابت للـ i18n + رسالتين (عربى + إنجليزى).

### 8.3 Endpoint Pattern (Minimal API)
```csharp
public static void MapPastPaperEndpoints(this IEndpointRouteBuilder app)
{
    var g = app.MapGroup("/api/v1/past-papers")
               .RequireAuthorization()
               .WithTags("PastPapers");

    g.MapGet("/", async (int? subjectId, ISender sender, CancellationToken ct) =>
        (await sender.Send(new BrowsePastPapersQuery(subjectId, ...), ct)).ToHttpResult());
}
```
كل endpoint يمرّر لـ MediatR → Handler، والـ `Result` يتحوّل لـ HTTP response عبر `.ToHttpResult()`.

### 8.4 Blazor Page Pattern
```razor
@page "/my/homework"
@rendermode InteractiveServer
@inject FSEdu.Web.Services.ApiClient Api
@inject FSEdu.Web.Services.AuthState Auth
@inject NavigationManager Nav

@code {
    private List<StudentHomeworkRowDto> _rows = new();
    private bool _loaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await Auth.RestoreAsync();
        if (!Auth.IsInRole("Student")) { Nav.NavigateTo("/login"); return; }
        _rows = await Api.ListMyHomeworkAsync();
        _loaded = true; StateHasChanged();
    }
}
```

### 8.5 EF Configuration Pattern
```csharp
public sealed class HomeworkConfiguration : IEntityTypeConfiguration<Homework>
{
    public void Configure(EntityTypeBuilder<Homework> b)
    {
        b.ToTable("Homeworks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000);
        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(x => x.DeletedAtUtc == null);  // soft delete
        b.HasIndex(x => new { x.CourseId, x.DueDateUtc });
    }
}
```

### 8.6 Domain Entity Pattern
```csharp
public sealed class Homework : AggregateRoot<Guid>, IAuditable, ISoftDelete
{
    // ─── Properties are private set ───
    public string Title { get; private set; } = default!;

    // ─── Auditable fields (interceptor fills these) ───
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }

    // ─── Constructor for EF ───
    private Homework() { }

    // ─── Public constructor for creation ───
    public Homework(Guid id, string title, ...) : base(id) { Title = title; ... }

    // ─── Behaviour ───
    public void UpdateDetails(string title, ...) { Title = title; ... }
    public bool IsOverdue(DateTime nowUtc) => nowUtc > DueDateUtc;
}
```
**قاعدة:** لا setter عام. كل تعديل عبر method واضحة الاسم.

---

## 9. البث المباشر (LiveKit)

### الاختيار
استخدمنا **LiveKit Cloud** كـ WebRTC SFU. البدائل التى تم استبعادها:
- Twilio Video → مُتوقّف
- Agora → غالى + محدود جغرافيًا
- Jitsi Self-hosted → عبء تشغيلى

### إعداد الحساب

1. سجّل على [livekit.io](https://livekit.io)
2. أنشئ Project → ستحصل على:
   - **URL** (مثال: `wss://your-project.livekit.cloud`)
   - **API Key** (مثال: `APIxxxxxx`)
   - **API Secret** (secret 32+ حرف)
3. أضفهم فى `appsettings.Development.json`:
   ```json
   "LiveKit": {
     "Url": "wss://fullscreen-xflzb6wv.livekit.cloud",
     "ApiKey": "APIxxxxxx",
     "ApiSecret": "your-secret-here"
   }
   ```

### تدفّق الطالب/المعلّم لدخول الحصة

```
┌────────────────┐    POST /api/v1/live/{id}/join       ┌──────────────┐
│  Blazor Web    │ ─────────────────────────────────►   │  API         │
│  (LiveRoom)    │                                       │              │
└────────────────┘                                       └──────┬───────┘
                                                                │
                                          ┌─────────────────────┴─┐
                                          │ 1. Access check       │
                                          │ 2. StudentJoined()    │
                                          │ 3. Generate JWT       │
                                          │    (via ILiveKitTokenService) │
                                          └─────────┬─────────────┘
                                                    │
                              { LiveKitUrl, LiveKitToken, RoomId, IsTeacher }
                                                    ▼
                                           ┌──────────────────┐
                                           │  Blazor connects │
                                           │  livekit-room.js │
                                           │  → wss://...     │
                                           └────────┬─────────┘
                                                    │ WebRTC
                                                    ▼
                                          ┌───────────────────┐
                                          │  LiveKit Cloud    │
                                          │  (SFU relays media)│
                                          └───────────────────┘
```

### الـ Token
كل مستخدم يحصل على JWT مخصّص للـ session:
```csharp
// من LiveKitTokenService.cs
var videoGrant = new
{
    room = roomName,          // ID الحصة الموحّد
    roomJoin = true,
    canPublish = isTeacher,   // فقط المعلّم يبثّ
    canSubscribe = true,
    canPublishData = true,    // للـ data channel (chat في الفيديو)
    canUpdateOwnMetadata = true
};
```

### التسجيل (Egress)
LiveKit يوفّر خدمة **Egress** لتسجيل الحصص server-side:
```
Teacher clicks "Start Recording"
   ↓
POST /twirp/livekit.Egress/StartRoomCompositeEgress
   ↓
LiveKit renders room grid layout → MP4 → uploads to /recordings/
   ↓
عند انتهاء الحصة → Webhook egress_ended → API يستقبل → يحدّث LiveSession.RecordingUrl
```

**Webhook endpoint:** `POST /api/v1/webhooks/livekit` (فى `LiveClassroomEndpoints.cs`)
**Verification:** JWT signed with same ApiSecret (فى `LiveKitWebhookVerifier.cs`)

### الملفّات الرئيسية

| ملف | الغرض |
|-----|-------|
| `src/Presentation/FSEdu.Api/LiveKit/LiveKitOptions.cs` | Config binding |
| `src/Presentation/FSEdu.Api/LiveKit/LiveKitTokenService.cs` | توليد JWT للمشاركين |
| `src/Presentation/FSEdu.Api/LiveKit/LiveKitEgressService.cs` | بدء/إيقاف التسجيل عبر Twirp |
| `src/Presentation/FSEdu.Api/LiveKit/LiveKitWebhookVerifier.cs` | التحقّق من توقيع Webhook |
| `src/Presentation/FSEdu.Web/wwwroot/js/livekit-room.js` | Client-side LiveKit SDK wrapper (UMD من CDN) |
| `src/Presentation/FSEdu.Web/Components/Pages/LiveRoom.razor` | صفحة الحصة الكاملة |
| `src/Core/FSEdu.Domain/LiveClassroom/LiveSession.cs` | Domain entity |

### احتساب الحضور
كل student join → `LiveSession.StudentJoined(studentId)` يضيف/يحدّث `LiveAttendance`. عند الـ disconnect (فى `ClassroomHub.OnDisconnectedAsync`) → `LiveSession.StudentLeft()` يحدّث `TotalDurationSec`.

**قاعدة الحضور المعتمد للمعلّم:** الطالب يُحسب إذا حضر ≥ 50% من مدة الحصة (`TotalDurationSec >= DurationMinutes * 60 / 2`).

---

## 10. السبورة التفاعلية

### المكوّنات
1. **Backend** (C#): `ClassroomHub` (SignalR Hub) — يستقبل strokes ويعيد بثّها + يحتفظ بـ snapshot
2. **Frontend** (JS): `whiteboard.js` — canvas drawing + pointer events
3. **UI** (Blazor): تكامل عبر `IJSRuntime` + `DotNetObjectReference`

### تدفّق الرسم

```
Teacher draws stroke
   ↓
whiteboard.js: pointerdown → pointermove → pointerup
   ↓
onPointerUp() → dotnetRef.invokeMethodAsync("OnStrokeFinished", stroke)
   ↓
Blazor page: [JSInvokable] OnStrokeFinished(stroke)
   ↓
_hubConnection.SendAsync("DrawStroke", sessionId, stroke)
   ↓
ClassroomHub.DrawStroke:
   1. Verify caller is teacher of this session
   2. Append to _whiteboards[sessionId] (cap 5000 strokes)
   3. Broadcast to Clients.OthersInGroup(sessionId).SendAsync("StrokeReceived", stroke)
   ↓
Students' whiteboard.js: applyRemoteStroke(stroke) → draws on canvas
```

### الـ Stroke Payload (opaque)
```javascript
{
    color: "#111111",
    width: 3,
    mode: "pen",       // أو "eraser"
    points: [[x1,y1], [x2,y2], [x3,y3], ...]
}
```
الخادم لا يفسّر البيانات — فقط يعيد توزيعها. المرن كافى لإضافة أشكال جديدة (خطوط، مستطيلات، …) بدون تعديل server.

### Late Joiners
عند `JoinRoom` فى `ClassroomHub`:
```csharp
if (_whiteboards.TryGetValue(sessionIdStr, out var strokes) && strokes.Count > 0)
{
    List<object> snapshot;
    lock (strokes) { snapshot = new List<object>(strokes); }
    await Clients.Caller.SendAsync("WhiteboardSnapshot", snapshot);
}
```
الطالب المتأخّر يستقبل `WhiteboardSnapshot` array → `whiteboard.js` يستدعى `applySnapshot(strokes)` لإعادة الرسم كامل.

### الميزات الحالية
- [x] قلم بألوان + أحجام قابلة للتغيير
- [x] ممحاة (يستخدم لون الخلفية)
- [x] مسح كامل (`ClearWhiteboard`)
- [x] Snapshot للـ late joiners
- [x] Cap 5000 stroke لكل session لمنع OOM
- [x] Composer اختيارى يدمج السبورة مع الفيديو للتسجيل

### قيود (Known Limitations)
- الحالة فى الذاكرة (`ConcurrentDictionary`) — إذا سقط الـ API server، الرسم يضيع. الحل المستقبلى: Redis أو DB.
- لا undo/redo
- لا collaboration بين طلبتين (المعلّم فقط يرسم)

### تكامل التسجيل
عند تسجيل الحصة عبر browser (وليس Egress)، `RecordingComposer` يدمج الصور:
```
priority: whiteboard > screen > camera
   + camera PiP في الزاوية
```
النتيجة MP4 واحد فيه كل شىء (رسم + فيديو + صوت).

---

## 11. الاشتراكات والإعدادات الخارجية

### 🔴 مطلوب (Production)

#### 1. **SQL Server**
- SQL Server 2019+ أو Azure SQL
- Connection string فى `ConnectionStrings:Default`

#### 2. **LiveKit Cloud**
- الاشتراك مطلوب للبثّ المباشر
- **الخطة الحرّة:** 50 دقيقة/شهر (كافى للتجربة)
- **الخطة المدفوعة:** تبدأ من ~$50/شهر
- الـ config فى `LiveKit:*`

#### 3. **VAPID Keys** (للـ Web Push)
- تُولّد تلقائيًا فى Dev عبر `VapidKeyBootstrapper`
- فى Prod: استخدم أداة مثل `web-push-cli` لتوليد public/private pair
- ضع القيم فى `WebPush:PublicKey` + `WebPush:PrivateKey` + `WebPush:Subject`

### 🟡 اختيارى (Best Effort)

#### 4. **SMTP** (للـ Email)
- إذا لم يُعرَّف `Email:Smtp:Host` → `SmtpEmailSender` يعود false silently (no-op)
- الحقول المطلوبة:
  ```json
  "Email": {
    "Smtp": {
      "Host": "smtp.sendgrid.net",
      "Port": 587,
      "EnableSsl": true,
      "FromAddress": "no-reply@fsedu.com",
      "FromName": "FSEdu",
      "Username": "apikey",
      "Password": "SG.xxxxx"
    }
  }
  ```
- الأنواع اللى تُرسَل بالإيميل: `PaymentApproved`, `PaymentRejected`, `SubscriptionExpiring`, `CourseApproved`, `CourseRejected`, `TeacherApproved`, `Welcome`, `referral.rewarded`

#### 5. **SMS Provider** (للـ OTP)
- الحالى: `ConsoleSmsSender` (يطبع الـ OTP فى الـ log — Dev فقط)
- للـ Prod: استبدله بمزوّد مصرى (Vonage, Twilio, MessageMedia، أو مزوّد محلى)
- Interface: `ISmsSender` فى `FSEdu.Identity`

### 🟢 مستقبلى (لم يُشترك بعد)

#### 6. **Paymob / Fawry** (بوّابة دفع)
- غير مُشترك حاليًا — الدفع يدوى بالكامل
- للتكامل: أضف `IPaymentGateway` abstraction + Paymob SDK
- Webhook endpoint للـ callback من البوّابة

#### 7. **Anthropic Claude API** (للـ AI Tutor مستقبلًا)
- غير مُشترك
- للاستخدام: NuGet `Anthropic.SDK` + config `Anthropic:ApiKey`

### 🔒 Secrets Management
- **Development:** `appsettings.Development.json` (لا يُلتزم فى git — تأكّد من `.gitignore`)
- **Production:** استخدم Azure Key Vault أو AWS Secrets Manager أو env vars
- **لا** تضع secrets فى `appsettings.json` الرئيسى

---

## 12. أوامر يومية شائعة

```bash
# Build كل الحل
dotnet build

# Build مشروع واحد بدون warnings تفصيلية
dotnet build src/Presentation/FSEdu.Api/FSEdu.Api.csproj --nologo -v q

# تشغيل API (يشغّل الـ seeders أوّل مرة)
dotnet run --project src/Presentation/FSEdu.Api/FSEdu.Api.csproj

# تشغيل Web
dotnet run --project src/Presentation/FSEdu.Web/FSEdu.Web.csproj

# تطبيق migrations
dotnet ef database update \
    --project src/Infrastructure/FSEdu.Persistence/FSEdu.Persistence.csproj \
    --startup-project src/Presentation/FSEdu.Api/FSEdu.Api.csproj \
    --context ApplicationDbContext

# عرض قائمة migrations + حالتها
dotnet ef migrations list \
    --project src/Infrastructure/FSEdu.Persistence/FSEdu.Persistence.csproj \
    --startup-project src/Presentation/FSEdu.Api/FSEdu.Api.csproj \
    --context ApplicationDbContext

# التراجع عن آخر migration (dev only!)
dotnet ef database update PreviousMigrationName \
    --project src/Infrastructure/FSEdu.Persistence/FSEdu.Persistence.csproj \
    --startup-project src/Presentation/FSEdu.Api/FSEdu.Api.csproj \
    --context ApplicationDbContext
```

---

## 13. مواقع مهمة

### أين تجد ماذا؟

| تحتاج تعدّل... | ابدأ من... |
|----------------|-----------|
| Endpoint جديد | `src/Presentation/FSEdu.Api/Endpoints/*.cs` + سجّله فى `Program.cs` |
| Handler جديد | `src/Core/FSEdu.Application/Features/{Feature}/` |
| DTO للـ contracts | `src/BuildingBlocks/FSEdu.Shared.Contracts/{Area}/` |
| Domain entity | `src/Core/FSEdu.Domain/{Area}/` |
| EF Config | `src/Infrastructure/FSEdu.Persistence/Configurations/*Configurations.cs` |
| Migration | `src/Infrastructure/FSEdu.Persistence/Migrations/` (اكتب يدويًا) |
| Blazor page | `src/Presentation/FSEdu.Web/Components/Pages/*.razor` |
| ApiClient method | `src/Presentation/FSEdu.Web/Services/ApiClient.cs` |
| قائمة (navigation) | `src/Presentation/FSEdu.Web/Components/Layout/UserMenu.razor` |
| Notification type جديد | `src/Core/FSEdu.Application/Abstractions/INotificationService.cs` (constants) + `NotificationCategories` |
| Background job | `src/Infrastructure/FSEdu.Infrastructure/**/HostedService.cs` + سجّله فى `DependencyInjection` |
| ملف Whiteboard/LiveKit JS | `src/Presentation/FSEdu.Web/wwwroot/js/` |

### الـ Interfaces الأساسية (تحفظها بظهر قلب)

| Interface | الغرض |
|-----------|-------|
| `IApplicationDbContext` | Access to all DbSets — استخدمه دائمًا فى Handlers بدلًا من `ApplicationDbContext` مباشرة |
| `ICurrentUser` | يعطيك `UserId` + `IsInRole()` من الـ JWT الحالى |
| `IAccessPolicy` | `CanAccessSubjectAsync`, `CanAccessLessonAsync` — فحص صلاحية الطالب على المحتوى |
| `INotificationService` | إرسال إشعار (in-app + push + email حسب النوع) |
| `IGamificationService` | AwardXp, TryAwardBadge, UpdateStreak |
| `IDailyChallengeService` | RecordProgress, PickToday, GetToday |
| `ITotpService` | 2FA (verify code, hash recovery codes) |
| `IEmailSender` | SMTP email (best-effort) |
| `IWebPushService` | Web Push notifications |
| `IAuditLog` | تسجيل عمليات الأدمن |
| `IIdentityService` | إنشاء مستخدمين + تسجيل دخول + refresh tokens |
| `ILiveKitTokenService` | JWT للـ LiveKit rooms |
| `IEgressService` | بدء/إيقاف تسجيل حصص LiveKit |
| `IWeeklyDigestService` | إرسال تقرير أسبوعى لولى الأمر |

### مراجع سريعة

- **دليل شامل للمنصّة:** `docs/FSEdu-PlatformOverview.md`
- **استراتيجية استضافة الفيديو:** `docs/VideoHostingStrategy.md`
- **LiveKit docs:** https://docs.livekit.io/
- **QuestPDF docs:** https://www.questpdf.com/documentation/api-reference

---

## 🚀 خطوات المبرمج فى أول أسبوع

### اليوم 1
- [ ] استنساخ الـ repo + تشغيل `dotnet restore`
- [ ] إعداد SQL Server + connection string
- [ ] تطبيق كل الـ migrations
- [ ] تشغيل API + Web + محاولة تسجيل دخول بمستخدم seed
- [ ] قراءة هذا الملف + `FSEdu-PlatformOverview.md`

### اليوم 2–3
- [ ] استعراض بنية Domain layer (كل الـ aggregate roots)
- [ ] قراءة 3 Handlers على الأقل: بسيط (Query) + متوسط (Command) + معقّد (multi-step Command)
- [ ] تعقّب طلب من UI → ApiClient → Endpoint → Handler → DB وبالعكس

### اليوم 4–5
- [ ] إنشاء feature بسيط تجريبى (مثال: زرّ "أضف لقائمة اهتماماتى" فى صفحة الدورة)
  - Domain entity? أو fields على Enrollment؟
  - EF config + migration
  - Contract DTO
  - Handler (Query + Command)
  - Endpoint
  - ApiClient method
  - UI change

### أسبوع 2+
- استلام issue من الـ backlog + تنفيذه بمراجعة senior

---

**آخر تحديث:** 2026-09-02
**المُلاّك الأصليّون:** فريق FullScreen Solutions
