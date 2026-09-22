-- ==============================================================================================
-- FSEdu - Real Production-Grade Seed Data Script
-- Target Database: FSEdu_Dev
-- Schemas: edu, auth
-- ==============================================================================================

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

    PRINT N'==> [1/14] بدء إدخال المعلمين وحساباتهم (Teachers)...';

    DECLARE @TeacherRole uniqueidentifier = '5daba3d1-44d1-4ef5-57a5-08dea22b6ddf';
    DECLARE @StudentRole uniqueidentifier = '79ab2f98-bd62-4da7-57a3-08dea22b6ddf';
    DECLARE @ParentRole  uniqueidentifier = 'a312caed-f8bf-4565-57a4-08dea22b6ddf';

    -- Password hash for '0115155691'
    DECLARE @DefaultPasswordHash nvarchar(max) = N'AQAAAAIAAYagAAAAECChr2svUo7fyK+RiZsIcUVBJ/eRWN+croOM34bkpygnFJbv09j4uZMlFStvZ4Udsg==';

    -- Teacher 1: Dr. Hossam Ibrahim (Science)
    DECLARE @T1_Id uniqueidentifier = '11111111-1111-1111-1111-111111111101';
    IF NOT EXISTS (SELECT 1 FROM auth.Users WHERE Id = @T1_Id)
    BEGIN
        INSERT INTO auth.Users (Id, FullName, DomainUserId, CreatedAtUtc, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@T1_Id, N'أ.د. حسام إبراهيم', @T1_Id, GETUTCDATE(), N'+201011112221', N'+201011112221', N'dr.hossam.science@fsedu.com', N'DR.HOSSAM.SCIENCE@FSEDU.COM', 1, @DefaultPasswordHash, NEWID(), NEWID(), N'+201011112221', 1, 0, 1, 0);

        INSERT INTO auth.UserRoles (UserId, RoleId) VALUES (@T1_Id, @TeacherRole);
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.Users WHERE Id = @T1_Id)
    BEGIN
        INSERT INTO edu.Users (Id, FullName, Email, Phone, PhoneVerified, EmailVerified, Locale, Timezone, AvatarUrl, Status, CreatedAtUtc, UserType, Bio, YearsOfExperience, RatingAvg, RatingsCount, Verified, VerifiedAtUtc, StreakShields, WeeklyDigestEnabled)
        VALUES (@T1_Id, N'أ.د. حسام إبراهيم', N'dr.hossam.science@fsedu.com', N'+201011112221', 1, 1, N'ar-EG', N'Africa/Cairo', N'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&q=80&w=250', 1, GETUTCDATE(), N'Teacher', N'كبير معلمي مادة العلوم والكيمياء، دكتوراه المناهج وطرق التدريس من جامعة عين شمس، خبرة 15 عاماً في تبسيط العلوم والتجارب المعملية.', 15, 4.95, 68, 1, GETUTCDATE(), 0, 1);

        INSERT INTO edu.TeacherQualifications (TeacherId, Title, Institution, Year, DocumentUrl)
        VALUES (@T1_Id, N'دكتوراه المناهج وطرق تدريس العلوم', N'جامعة عين شمس', 2014, N'/docs/phd_hossam.pdf');

        INSERT INTO edu.TeacherSubjects (TeacherId, SubjectId) VALUES (@T1_Id, 62), (@T1_Id, 54), (@T1_Id, 46), (@T1_Id, 39);
        INSERT INTO edu.TeacherRegions (TeacherId, RegionId) VALUES (@T1_Id, 1), (@T1_Id, 2), (@T1_Id, 4), (@T1_Id, 18);
    END;

    -- Teacher 2: Mr. Reda El-Farouk (Arabic)
    DECLARE @T2_Id uniqueidentifier = '11111111-1111-1111-1111-111111111102';
    IF NOT EXISTS (SELECT 1 FROM auth.Users WHERE Id = @T2_Id)
    BEGIN
        INSERT INTO auth.Users (Id, FullName, DomainUserId, CreatedAtUtc, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@T2_Id, N'أ. رضا الفاروق', @T2_Id, GETUTCDATE(), N'+201011112222', N'+201011112222', N'mr.reda.arabic@fsedu.com', N'MR.REDA.ARABIC@FSEDU.COM', 1, @DefaultPasswordHash, NEWID(), NEWID(), N'+201011112222', 1, 0, 1, 0);

        INSERT INTO auth.UserRoles (UserId, RoleId) VALUES (@T2_Id, @TeacherRole);
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.Users WHERE Id = @T2_Id)
    BEGIN
        INSERT INTO edu.Users (Id, FullName, Email, Phone, PhoneVerified, EmailVerified, Locale, Timezone, AvatarUrl, Status, CreatedAtUtc, UserType, Bio, YearsOfExperience, RatingAvg, RatingsCount, Verified, VerifiedAtUtc, StreakShields, WeeklyDigestEnabled)
        VALUES (@T2_Id, N'أ. رضا الفاروق', N'mr.reda.arabic@fsedu.com', N'+201011112222', 1, 1, N'ar-EG', N'Africa/Cairo', N'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&q=80&w=250', 1, GETUTCDATE(), N'Teacher', N'معلم خبير لغة عربية وبلاغة ونصوص، ليسانس لغة عربية وآدابها جامعة القاهرة، مؤلف سلسلة التميز في النحو العربي وخبرة 18 عاماً.', 18, 4.98, 94, 1, GETUTCDATE(), 0, 1);

        INSERT INTO edu.TeacherQualifications (TeacherId, Title, Institution, Year, DocumentUrl)
        VALUES (@T2_Id, N'ليسانس آداب ولغة عربية ودبلوم تربوي', N'جامعة القاهرة', 2008, N'/docs/cert_reda.pdf');

        INSERT INTO edu.TeacherSubjects (TeacherId, SubjectId) VALUES (@T2_Id, 59), (@T2_Id, 51), (@T2_Id, 43);
        INSERT INTO edu.TeacherRegions (TeacherId, RegionId) VALUES (@T2_Id, 1), (@T2_Id, 2), (@T2_Id, 6), (@T2_Id, 18);
    END;

    -- Teacher 3: Eng. Walid El-Shennawy (Math)
    DECLARE @T3_Id uniqueidentifier = '11111111-1111-1111-1111-111111111103';
    IF NOT EXISTS (SELECT 1 FROM auth.Users WHERE Id = @T3_Id)
    BEGIN
        INSERT INTO auth.Users (Id, FullName, DomainUserId, CreatedAtUtc, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@T3_Id, N'م. وليد الشناوي', @T3_Id, GETUTCDATE(), N'+201011112223', N'+201011112223', N'eng.walid.math@fsedu.com', N'ENG.WALID.MATH@FSEDU.COM', 1, @DefaultPasswordHash, NEWID(), NEWID(), N'+201011112223', 1, 0, 1, 0);

        INSERT INTO auth.UserRoles (UserId, RoleId) VALUES (@T3_Id, @TeacherRole);
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.Users WHERE Id = @T3_Id)
    BEGIN
        INSERT INTO edu.Users (Id, FullName, Email, Phone, PhoneVerified, EmailVerified, Locale, Timezone, AvatarUrl, Status, CreatedAtUtc, UserType, Bio, YearsOfExperience, RatingAvg, RatingsCount, Verified, VerifiedAtUtc, StreakShields, WeeklyDigestEnabled)
        VALUES (@T3_Id, N'م. وليد الشناوي', N'eng.walid.math@fsedu.com', N'+201011112223', 1, 1, N'ar-EG', N'Africa/Cairo', N'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&q=80&w=250', 1, GETUTCDATE(), N'Teacher', N'مهندس ومعلم أول الرياضيات للمرحلة الإعدادية، مبتكر طريقة الخرائط الذهنية في الجبر والهندسة، مدرب أولمبياد الرياضيات وخبرة 12 عاماً.', 12, 4.91, 76, 1, GETUTCDATE(), 0, 1);

        INSERT INTO edu.TeacherQualifications (TeacherId, Title, Institution, Year, DocumentUrl)
        VALUES (@T3_Id, N'بكالوريوس هندسة ودبلومة عامة في التربية', N'جامعة عين شمس', 2012, N'/docs/eng_walid.pdf');

        INSERT INTO edu.TeacherSubjects (TeacherId, SubjectId) VALUES (@T3_Id, 60), (@T3_Id, 52), (@T3_Id, 44);
        INSERT INTO edu.TeacherRegions (TeacherId, RegionId) VALUES (@T3_Id, 1), (@T3_Id, 2), (@T3_Id, 18);
    END;

    -- Teacher 4: Mr. Michael Samir (English)
    DECLARE @T4_Id uniqueidentifier = '11111111-1111-1111-1111-111111111104';
    IF NOT EXISTS (SELECT 1 FROM auth.Users WHERE Id = @T4_Id)
    BEGIN
        INSERT INTO auth.Users (Id, FullName, DomainUserId, CreatedAtUtc, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@T4_Id, N'Mr. Michael Samir', @T4_Id, GETUTCDATE(), N'+201011112224', N'+201011112224', N'mr.michael.english@fsedu.com', N'MR.MICHAEL.ENGLISH@FSEDU.COM', 1, @DefaultPasswordHash, NEWID(), NEWID(), N'+201011112224', 1, 0, 1, 0);

        INSERT INTO auth.UserRoles (UserId, RoleId) VALUES (@T4_Id, @TeacherRole);
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.Users WHERE Id = @T4_Id)
    BEGIN
        INSERT INTO edu.Users (Id, FullName, Email, Phone, PhoneVerified, EmailVerified, Locale, Timezone, AvatarUrl, Status, CreatedAtUtc, UserType, Bio, YearsOfExperience, RatingAvg, RatingsCount, Verified, VerifiedAtUtc, StreakShields, WeeklyDigestEnabled)
        VALUES (@T4_Id, N'Mr. Michael Samir', N'mr.michael.english@fsedu.com', N'+201011112224', 1, 1, N'ar-EG', N'Africa/Cairo', N'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?auto=format&fit=crop&q=80&w=250', 1, GETUTCDATE(), N'Teacher', N'Senior English Instructor with 11 years experience in Egyptian prep & international schools. Cambridge CELTA certified.', 11, 4.93, 52, 1, GETUTCDATE(), 0, 1);

        INSERT INTO edu.TeacherQualifications (TeacherId, Title, Institution, Year, DocumentUrl)
        VALUES (@T4_Id, N'Cambridge CELTA Certification', N'University of Cambridge', 2016, N'/docs/celta_michael.pdf');

        INSERT INTO edu.TeacherSubjects (TeacherId, SubjectId) VALUES (@T4_Id, 61), (@T4_Id, 53), (@T4_Id, 45);
        INSERT INTO edu.TeacherRegions (TeacherId, RegionId) VALUES (@T4_Id, 1), (@T4_Id, 2), (@T4_Id, 4);
    END;

    -- Teacher 5: Mr. Sameh Abdel-Fattah (Social Studies)
    DECLARE @T5_Id uniqueidentifier = '11111111-1111-1111-1111-111111111105';
    IF NOT EXISTS (SELECT 1 FROM auth.Users WHERE Id = @T5_Id)
    BEGIN
        INSERT INTO auth.Users (Id, FullName, DomainUserId, CreatedAtUtc, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@T5_Id, N'أ. سامح عبد الفتاح', @T5_Id, GETUTCDATE(), N'+201011112225', N'+201011112225', N'mr.sameh.social@fsedu.com', N'MR.SAMEH.SOCIAL@FSEDU.COM', 1, @DefaultPasswordHash, NEWID(), NEWID(), N'+201011112225', 1, 0, 1, 0);

        INSERT INTO auth.UserRoles (UserId, RoleId) VALUES (@T5_Id, @TeacherRole);
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.Users WHERE Id = @T5_Id)
    BEGIN
        INSERT INTO edu.Users (Id, FullName, Email, Phone, PhoneVerified, EmailVerified, Locale, Timezone, AvatarUrl, Status, CreatedAtUtc, UserType, Bio, YearsOfExperience, RatingAvg, RatingsCount, Verified, VerifiedAtUtc, StreakShields, WeeklyDigestEnabled)
        VALUES (@T5_Id, N'أ. سامح عبد الفتاح', N'mr.sameh.social@fsedu.com', N'+201011112225', 1, 1, N'ar-EG', N'Africa/Cairo', N'https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?auto=format&fit=crop&q=80&w=250', 1, GETUTCDATE(), N'Teacher', N'كبير معلمي الدراسات الاجتماعية والتاريخ، مؤلف أطلس الخرائط التفاعلية للشهادة الإعدادية، خبرة 16 عاماً في وزارة التربية والتعليم.', 16, 4.89, 44, 1, GETUTCDATE(), 0, 1);

        INSERT INTO edu.TeacherQualifications (TeacherId, Title, Institution, Year, DocumentUrl)
        VALUES (@T5_Id, N'ليسانس آداب وتربية قسم جغرافيا وتاريخ', N'جامعة الإسكندرية', 2009, N'/docs/sameh_degree.pdf');

        INSERT INTO edu.TeacherSubjects (TeacherId, SubjectId) VALUES (@T5_Id, 63), (@T5_Id, 55);
        INSERT INTO edu.TeacherRegions (TeacherId, RegionId) VALUES (@T5_Id, 1), (@T5_Id, 2), (@T5_Id, 4), (@T5_Id, 18);
    END;


    PRINT N'==> [2/14] إدخال الطلاب وأولياء الأمور وروابط القرابة (Students & Parents & Links)...';

    -- Student 1: Maryam Ibrahim (3 Prep)
    DECLARE @S1_Id uniqueidentifier = '22222222-2222-2222-2222-222222222201';
    IF NOT EXISTS (SELECT 1 FROM auth.Users WHERE Id = @S1_Id)
    BEGIN
        INSERT INTO auth.Users (Id, FullName, DomainUserId, CreatedAtUtc, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@S1_Id, N'مريم إبراهيم حسن', @S1_Id, GETUTCDATE(), N'+201022223331', N'+201022223331', N'mariam.student@fsedu.com', N'MARIAM.STUDENT@FSEDU.COM', 1, @DefaultPasswordHash, NEWID(), NEWID(), N'+201022223331', 1, 0, 1, 0);

        INSERT INTO auth.UserRoles (UserId, RoleId) VALUES (@S1_Id, @StudentRole);
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.Users WHERE Id = @S1_Id)
    BEGIN
        INSERT INTO edu.Users (Id, FullName, Email, Phone, PhoneVerified, EmailVerified, Locale, Timezone, AvatarUrl, Status, CreatedAtUtc, UserType, StageId, RegionId, SchoolId, XpPoints, CurrentStreakDays, LongestStreakDays, StreakShields, WeeklyDigestEnabled)
        VALUES (@S1_Id, N'مريم إبراهيم حسن', N'mariam.student@fsedu.com', N'+201022223331', 1, 1, N'ar-EG', N'Africa/Cairo', N'https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&q=80&w=250', 1, GETUTCDATE(), N'Student', 11, 2, 4, 3420, 18, 25, 2, 1);
    END;

    -- Student 2: Omar Khaled (3 Prep)
    DECLARE @S2_Id uniqueidentifier = '22222222-2222-2222-2222-222222222202';
    IF NOT EXISTS (SELECT 1 FROM auth.Users WHERE Id = @S2_Id)
    BEGIN
        INSERT INTO auth.Users (Id, FullName, DomainUserId, CreatedAtUtc, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@S2_Id, N'عمر خالد الشريف', @S2_Id, GETUTCDATE(), N'+201022223332', N'+201022223332', N'omar.student@fsedu.com', N'OMAR.STUDENT@FSEDU.COM', 1, @DefaultPasswordHash, NEWID(), NEWID(), N'+201022223332', 1, 0, 1, 0);

        INSERT INTO auth.UserRoles (UserId, RoleId) VALUES (@S2_Id, @StudentRole);
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.Users WHERE Id = @S2_Id)
    BEGIN
        INSERT INTO edu.Users (Id, FullName, Email, Phone, PhoneVerified, EmailVerified, Locale, Timezone, AvatarUrl, Status, CreatedAtUtc, UserType, StageId, RegionId, SchoolId, XpPoints, CurrentStreakDays, LongestStreakDays, StreakShields, WeeklyDigestEnabled)
        VALUES (@S2_Id, N'عمر خالد الشريف', N'omar.student@fsedu.com', N'+201022223332', 1, 1, N'ar-EG', N'Africa/Cairo', N'https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?auto=format&fit=crop&q=80&w=250', 1, GETUTCDATE(), N'Student', 11, 1, 1, 2150, 9, 14, 1, 1);
    END;

    -- Parent 1: Dr. Ibrahim Hassan (Father of Maryam)
    DECLARE @P1_Id uniqueidentifier = '33333333-3333-3333-3333-333333333301';
    IF NOT EXISTS (SELECT 1 FROM auth.Users WHERE Id = @P1_Id)
    BEGIN
        INSERT INTO auth.Users (Id, FullName, DomainUserId, CreatedAtUtc, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@P1_Id, N'د. إبراهيم حسن الشربيني', @P1_Id, GETUTCDATE(), N'+201033334441', N'+201033334441', N'dr.ibrahim.parent@fsedu.com', N'DR.IBRAHIM.PARENT@FSEDU.COM', 1, @DefaultPasswordHash, NEWID(), NEWID(), N'+201033334441', 1, 0, 1, 0);

        INSERT INTO auth.UserRoles (UserId, RoleId) VALUES (@P1_Id, @ParentRole);
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.Users WHERE Id = @P1_Id)
    BEGIN
        INSERT INTO edu.Users (Id, FullName, Email, Phone, PhoneVerified, EmailVerified, Locale, Timezone, AvatarUrl, Status, CreatedAtUtc, UserType, NationalId, Occupation, StreakShields, WeeklyDigestEnabled)
        VALUES (@P1_Id, N'د. إبراهيم حسن الشربيني', N'dr.ibrahim.parent@fsedu.com', N'+201033334441', 1, 1, N'ar-EG', N'Africa/Cairo', N'https://images.unsplash.com/photo-1492562080023-ab3db95bfbce?auto=format&fit=crop&q=80&w=250', 1, GETUTCDATE(), N'Parent', N'27805121401234', N'طبيب استشاري باطنة', 0, 1);
    END;

    -- Link Dr. Ibrahim to Maryam
    IF NOT EXISTS (SELECT 1 FROM edu.ParentStudentLinks WHERE ParentId = @P1_Id AND StudentId = @S1_Id)
    BEGIN
        INSERT INTO edu.ParentStudentLinks (ParentId, StudentId, Relation, IsPrimary)
        VALUES (@P1_Id, @S1_Id, 1, 1);
    END;

    -- Link Dev Parent to Gaber Anwar & Dev Student
    DECLARE @DevParentId uniqueidentifier = 'b8e3e01f-bea9-4dbc-8019-b7e3bb0706aa';
    DECLARE @GaberStudentId uniqueidentifier = '855c96a3-29e1-4cd3-ae4c-abe027198351';
    DECLARE @DevStudentId uniqueidentifier = '2be5c036-0958-4257-b5f9-cadbf729271e';

    IF EXISTS (SELECT 1 FROM edu.Users WHERE Id = @DevParentId)
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM edu.ParentStudentLinks WHERE ParentId = @DevParentId AND StudentId = @GaberStudentId)
            INSERT INTO edu.ParentStudentLinks (ParentId, StudentId, Relation, IsPrimary) VALUES (@DevParentId, @GaberStudentId, 1, 1);

        IF NOT EXISTS (SELECT 1 FROM edu.ParentStudentLinks WHERE ParentId = @DevParentId AND StudentId = @DevStudentId)
            INSERT INTO edu.ParentStudentLinks (ParentId, StudentId, Relation, IsPrimary) VALUES (@DevParentId, @DevStudentId, 1, 1);
    END;

    -- Update Gaber Anwar's gamification points & active status
    UPDATE edu.Users
    SET XpPoints = 2850, CurrentStreakDays = 14, LongestStreakDays = 21, StreakShields = 2,
        AvatarUrl = COALESCE(AvatarUrl, N'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&q=80&w=250')
    WHERE Id = @GaberStudentId;


    PRINT N'==> [3/14] إدخال الدورات التعليمية الشاملة للشهادة الإعدادية (Courses)...';

    -- Course 1: Math (Algebra & Statistics - Prep 3)
    DECLARE @C1_Id uniqueidentifier = '44444444-4444-4444-4444-444444444401';
    IF NOT EXISTS (SELECT 1 FROM edu.Courses WHERE Id = @C1_Id)
    BEGIN
        INSERT INTO edu.Courses (Id, TeacherId, SubjectId, StageId, Title, Description, ThumbnailUrl, Price, Currency, Status, Term, EnrollmentCount, RatingAvg, RatingsCount, CreatedAtUtc, CreatedBy, PreviewVideoUrl)
        VALUES (@C1_Id, @T3_Id, 60, 11, N'الرياضيات: الجبر والإحصاء — الصف الثالث الإعدادي (الترم الأول)',
                N'دورة احترافية شاملة في الجبر والإحصاء مع المهندس وليد الشناوي. تغطي شرحاً دقيقاً لحاصل الضرب الديكارتي، الدوال وكثيرات الحدود، النسبة والتناسب المتسلسل، والتغير الطردي والعكسي مع حل جميع أفكار امتحانات المحافظات السابقة وخرائط ذهنية لكل درس.',
                N'https://images.unsplash.com/photo-1635070041078-e363dbe005cb?auto=format&fit=crop&q=80&w=600',
                350.00, N'EGP', 2, 1, 148, 4.92, 45, GETUTCDATE(), N'Admin', N'https://www.youtube.com/watch?v=4xqAo4XBq9c');
    END;

    -- Course 2: Math (Geometry & Trigonometry - Prep 3)
    DECLARE @C2_Id uniqueidentifier = '44444444-4444-4444-4444-444444444402';
    IF NOT EXISTS (SELECT 1 FROM edu.Courses WHERE Id = @C2_Id)
    BEGIN
        INSERT INTO edu.Courses (Id, TeacherId, SubjectId, StageId, Title, Description, ThumbnailUrl, Price, Currency, Status, Term, EnrollmentCount, RatingAvg, RatingsCount, CreatedAtUtc, CreatedBy, PreviewVideoUrl)
        VALUES (@C2_Id, @T3_Id, 60, 11, N'الرياضيات: الهندسة وحساب المثلثات — الصف الثالث الإعدادي (الترم الأول)',
                N'كورس الهندسة التحليلية وحساب المثلثات مع م. وليد الشناوي. تأسيس كامل في النسب المثلثية للزوايا الحادة والخاصة، وقوانين البعد بين نقطتين، المنتصف، ميل المستقيم ومعادلة الخط المستقيم مع تطبيقات برمجية وهندسية.',
                N'https://images.unsplash.com/photo-1509228468518-180dd4864904?auto=format&fit=crop&q=80&w=600',
                350.00, N'EGP', 2, 1, 132, 4.88, 38, GETUTCDATE(), N'Admin', N'https://www.youtube.com/watch?v=w2Qzb_zG3gI');
    END;

    -- Course 3: Science (Prep 3)
    DECLARE @C3_Id uniqueidentifier = '44444444-4444-4444-4444-444444444403';
    IF NOT EXISTS (SELECT 1 FROM edu.Courses WHERE Id = @C3_Id)
    BEGIN
        INSERT INTO edu.Courses (Id, TeacherId, SubjectId, StageId, Title, Description, ThumbnailUrl, Price, Currency, Status, Term, EnrollmentCount, RatingAvg, RatingsCount, CreatedAtUtc, CreatedBy, PreviewVideoUrl)
        VALUES (@C3_Id, @T1_Id, 62, 11, N'العلوم المتكاملة — الصف الثالث الإعدادي (الترم الأول)',
                N'رحلة شائقة في عالم الفيزياء والفلك والأحياء مع أ.د. حسام إبراهيم. يشمل شرح القوى والحركة والسرعة النسبية، والمرايا والعدسات وعيوب الإبصار بالمحاكاة ثلاثية الأبعاد، وتاريخ نشأة الكون، والانقسام الخلوي والتكاثر.',
                N'https://images.unsplash.com/photo-1532094349884-543bc11b234d?auto=format&fit=crop&q=80&w=600',
                400.00, N'EGP', 2, 1, 185, 4.96, 62, GETUTCDATE(), N'Admin', N'https://www.youtube.com/watch?v=BELlZKpi1Zs');
    END;

    -- Course 4: Arabic (Prep 3)
    DECLARE @C4_Id uniqueidentifier = '44444444-4444-4444-4444-444444444404';
    IF NOT EXISTS (SELECT 1 FROM edu.Courses WHERE Id = @C4_Id)
    BEGIN
        INSERT INTO edu.Courses (Id, TeacherId, SubjectId, StageId, Title, Description, ThumbnailUrl, Price, Currency, Status, Term, EnrollmentCount, RatingAvg, RatingsCount, CreatedAtUtc, CreatedBy, PreviewVideoUrl)
        VALUES (@C4_Id, @T2_Id, 59, 11, N'اللغة العربية الشاملة (قراءة ونصوص ونحو وقصة) — الصف الثالث الإعدادي',
                N'أقوى كورس لغة عربية مع أ. رضا الفاروق. تأسيس فريد في النحو وقواعد الإعراب (المنادى، البدل، المدح والذم، والممنوع من الصرف)، مع تذوق جمالي للنصوص الأدبية وتحليل فصول قصة طموح جارية وحل قطع امتحانات النحو الصعبة.',
                N'https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?auto=format&fit=crop&q=80&w=600',
                400.00, N'EGP', 2, 1, 210, 4.98, 88, GETUTCDATE(), N'Admin', N'https://www.youtube.com/watch?v=QXeEoD0pB3E');
    END;

    -- Course 5: English (Prep 3 - New Hello!)
    DECLARE @C5_Id uniqueidentifier = '44444444-4444-4444-4444-444444444405';
    IF NOT EXISTS (SELECT 1 FROM edu.Courses WHERE Id = @C5_Id)
    BEGIN
        INSERT INTO edu.Courses (Id, TeacherId, SubjectId, StageId, Title, Description, ThumbnailUrl, Price, Currency, Status, Term, EnrollmentCount, RatingAvg, RatingsCount, CreatedAtUtc, CreatedBy, PreviewVideoUrl)
        VALUES (@C5_Id, @T4_Id, 61, 11, N'English Prep 3 (New Hello!) — First Term Mastery',
                N'Master prep 3 English with Mr. Michael Samir. Complete vocabulary acquisition, active grammar explanation (tenses, conditionals, passives), reading skills, listening drills, dialogue completion practice, and writing high-scoring paragraphs.',
                N'https://images.unsplash.com/photo-1457369804613-52c61a468e7d?auto=format&fit=crop&q=80&w=600',
                380.00, N'EGP', 2, 1, 140, 4.91, 48, GETUTCDATE(), N'Admin', N'https://www.youtube.com/watch?v=BELlZKpi1Zs');
    END;

    -- Course 6: Social Studies (Prep 3)
    DECLARE @C6_Id uniqueidentifier = '44444444-4444-4444-4444-444444444406';
    IF NOT EXISTS (SELECT 1 FROM edu.Courses WHERE Id = @C6_Id)
    BEGIN
        INSERT INTO edu.Courses (Id, TeacherId, SubjectId, StageId, Title, Description, ThumbnailUrl, Price, Currency, Status, Term, EnrollmentCount, RatingAvg, RatingsCount, CreatedAtUtc, CreatedBy, PreviewVideoUrl)
        VALUES (@C6_Id, @T5_Id, 63, 11, N'الدراسات الاجتماعية: جغرافيا العالم وتاريخ مصر الحديث — 3 إعدادي',
                N'شرح شيق ومبسط لجميع أجزاء الجغرافيا والتاريخ مع أ. سامح عبد الفتاح. قراءة الخرائط الصماء، دراسة المناخ والتضاريس وسكان العالم، وتحليل أحداث مصر الحديثة من الحملة الفرنسية وحتى بناء دولة محمد علي وثورة الشعب.',
                N'https://images.unsplash.com/photo-1447069387593-a5de0862481e?auto=format&fit=crop&q=80&w=600',
                320.00, N'EGP', 2, 1, 115, 4.87, 34, GETUTCDATE(), N'Admin', N'https://www.youtube.com/watch?v=w2Qzb_zG3gI');
    END;


    PRINT N'==> [4/14] إدخال الوحدات والدروس التعليمية (Chapters & Lessons)...';

    -- Chapters for Course 1: Algebra
    DECLARE @Ch1_1 uniqueidentifier = '55555555-1111-1111-1111-111111111101';
    DECLARE @Ch1_2 uniqueidentifier = '55555555-1111-1111-1111-111111111102';
    DECLARE @Ch1_3 uniqueidentifier = '55555555-1111-1111-1111-111111111103';

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch1_1)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch1_1, @C1_Id, N'الوحدة الأولى: العلاقات والدوال', 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch1_2)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch1_2, @C1_Id, N'الوحدة الثانية: النسبة والتناسب', 2);

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch1_3)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch1_3, @C1_Id, N'الوحدة الثالثة: الإحصاء ومقاييس التشتت', 3);

    -- Lessons for Algebra Ch1
    DECLARE @L1_1 uniqueidentifier = '66666666-1111-1111-1111-111111111101';
    DECLARE @L1_2 uniqueidentifier = '66666666-1111-1111-1111-111111111102';
    DECLARE @L1_3 uniqueidentifier = '66666666-1111-1111-1111-111111111103';

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L1_1)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L1_1, @Ch1_1, N'الدرس 1: حاصل الضرب الديكارتي للمجموعات المنتهية وغير المنتهية', 1, 1450, N'https://www.youtube.com/watch?v=4xqAo4XBq9c', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L1_2)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L1_2, @Ch1_1, N'الدرس 2: العلاقات والدالة وكيفية استنتاج بيان الدالة ومداها', 1, 1720, N'https://www.youtube.com/watch?v=w2Qzb_zG3gI', 0, 2);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L1_3)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L1_3, @Ch1_1, N'الدرس 3: دوال كثيرات الحدود (الدالة الثابتة، الخطية، التربيعية)', 1, 1980, N'https://www.youtube.com/watch?v=BELlZKpi1Zs', 0, 3);

    -- Lessons for Algebra Ch2
    DECLARE @L1_4 uniqueidentifier = '66666666-1111-1111-1111-111111111104';
    DECLARE @L1_5 uniqueidentifier = '66666666-1111-1111-1111-111111111105';

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L1_4)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L1_4, @Ch1_2, N'الدرس 4: النسبة والتناسب والخواص الخمسة للتناسب', 1, 1620, N'https://www.youtube.com/watch?v=QXeEoD0pB3E', 0, 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L1_5)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L1_5, @Ch1_2, N'الدرس 5: التغير الطردي والتغير العكسي والتطبيقات الحياتية', 1, 1540, N'https://www.youtube.com/watch?v=4xqAo4XBq9c', 0, 2);

    -- Chapters for Course 3: Science
    DECLARE @Ch3_1 uniqueidentifier = '55555555-3333-3333-3333-333333333301';
    DECLARE @Ch3_2 uniqueidentifier = '55555555-3333-3333-3333-333333333302';
    DECLARE @Ch3_3 uniqueidentifier = '55555555-3333-3333-3333-333333333303';
    DECLARE @Ch3_4 uniqueidentifier = '55555555-3333-3333-3333-333333333304';

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch3_1)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch3_1, @C3_Id, N'الوحدة الأولى: القوى والحركة', 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch3_2)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch3_2, @C3_Id, N'الوحدة الثانية: الطاقة الضوئية', 2);

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch3_3)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch3_3, @C3_Id, N'الوحدة الثالثة: الكون والنظام الشمسي', 3);

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch3_4)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch3_4, @C3_Id, N'الوحدة الرابعة: التكاثر واستمرار النوع', 4);

    -- Lessons for Science Ch1
    DECLARE @L3_1 uniqueidentifier = '66666666-3333-3333-3333-333333333301';
    DECLARE @L3_2 uniqueidentifier = '66666666-3333-3333-3333-333333333302';
    DECLARE @L3_3 uniqueidentifier = '66666666-3333-3333-3333-333333333303';

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L3_1)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L3_1, @Ch3_1, N'الدرس 1: الحركة في اتجاه واحد والسرعة المنتظمة والنسبية', 1, 1500, N'https://www.youtube.com/watch?v=BELlZKpi1Zs', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L3_2)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L3_2, @Ch3_1, N'الدرس 2: التمثيل البياني للحركة في خط مستقيم والعجلة المنتظمة', 1, 1680, N'https://www.youtube.com/watch?v=w2Qzb_zG3gI', 0, 2);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L3_3)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L3_3, @Ch3_1, N'الدرس 3: الكميات الفيزيائية القياسية والمتجهة والإزاحة', 1, 1420, N'https://www.youtube.com/watch?v=4xqAo4XBq9c', 0, 3);

    -- Chapters for Course 4: Arabic
    DECLARE @Ch4_1 uniqueidentifier = '55555555-4444-4444-4444-444444444401';
    DECLARE @Ch4_2 uniqueidentifier = '55555555-4444-4444-4444-444444444402';

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch4_1)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch4_1, @C4_Id, N'الوحدة الأولى: النصوص والقراءة وتذوق البلاغة', 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch4_2)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch4_2, @C4_Id, N'الوحدة الثانية: النحو التطبيقي والقواعد', 2);

    -- Lessons for Arabic
    DECLARE @L4_1 uniqueidentifier = '66666666-4444-4444-4444-444444444401';
    DECLARE @L4_2 uniqueidentifier = '66666666-4444-4444-4444-444444444402';
    DECLARE @L4_3 uniqueidentifier = '66666666-4444-4444-4444-444444444403';

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L4_1)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L4_1, @Ch4_1, N'الدرس 1: نص عباد الرحمن (قرآن كريم - سورة الفرقان) تحليل وبلاغة', 1, 1600, N'https://www.youtube.com/watch?v=QXeEoD0pB3E', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L4_2)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L4_2, @Ch4_2, N'الدرس 2: المنادى وأقسامه (المعرب والمبني) وأسرار الإعراب', 1, 1950, N'https://www.youtube.com/watch?v=BELlZKpi1Zs', 0, 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L4_3)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L4_3, @Ch4_2, N'الدرس 3: البدل وأنواعه (المطابق وبعض من كل والاشتمال)', 1, 1750, N'https://www.youtube.com/watch?v=w2Qzb_zG3gI', 0, 2);

    -- Chapters for Course 2: Geometry & Trigonometry
    DECLARE @Ch2_1 uniqueidentifier = '55555555-2222-2222-2222-222222222201';
    DECLARE @Ch2_2 uniqueidentifier = '55555555-2222-2222-2222-222222222202';

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch2_1)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch2_1, @C2_Id, N'الوحدة الأولى: حساب المثلثات', 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch2_2)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch2_2, @C2_Id, N'الوحدة الثانية: الهندسة التحليلية', 2);

    -- Lessons for Geometry
    DECLARE @L2_1 uniqueidentifier = '66666666-2222-2222-2222-222222222201';
    DECLARE @L2_2 uniqueidentifier = '66666666-2222-2222-2222-222222222202';
    DECLARE @L2_3 uniqueidentifier = '66666666-2222-2222-2222-222222222203';
    DECLARE @L2_4 uniqueidentifier = '66666666-2222-2222-2222-222222222204';
    DECLARE @L2_5 uniqueidentifier = '66666666-2222-2222-2222-222222222205';

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L2_1)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L2_1, @Ch2_1, N'الدرس 1: النسب المثلثية الأساسية للزاوية الحادة (جا، جتا، ظا)', 1, 1650, N'https://www.youtube.com/watch?v=w2Qzb_zG3gI', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L2_2)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L2_2, @Ch2_1, N'الدرس 2: النسب المثلثية للزوايا الخاصة 30° و 45° و 60°', 1, 1800, N'https://www.youtube.com/watch?v=4xqAo4XBq9c', 0, 2);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L2_3)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L2_3, @Ch2_2, N'الدرس 3: البعد بين نقطتين في المستوى الإحداثي', 1, 1490, N'https://www.youtube.com/watch?v=BELlZKpi1Zs', 0, 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L2_4)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L2_4, @Ch2_2, N'الدرس 4: ميل الخط المستقيم وحالات التوازي والتعامد', 1, 1750, N'https://www.youtube.com/watch?v=QXeEoD0pB3E', 0, 2);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L2_5)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L2_5, @Ch2_2, N'الدرس 5: معادلة الخط المستقيم بمعلومية ميله والجزء المقطوع', 1, 1920, N'https://www.youtube.com/watch?v=4xqAo4XBq9c', 0, 3);

    -- Chapters for Course 5: English
    DECLARE @Ch5_1 uniqueidentifier = '55555555-5555-5555-5555-555555555501';
    DECLARE @Ch5_2 uniqueidentifier = '55555555-5555-5555-5555-555555555502';

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch5_1)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch5_1, @C5_Id, N'Unit 1: Around Town', 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch5_2)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch5_2, @C5_Id, N'Unit 2: Let''s Go Shopping', 2);

    -- Lessons for English
    DECLARE @L5_1 uniqueidentifier = '66666666-5555-5555-5555-555555555501';
    DECLARE @L5_2 uniqueidentifier = '66666666-5555-5555-5555-555555555502';
    DECLARE @L5_3 uniqueidentifier = '66666666-5555-5555-5555-555555555503';

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L5_1)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L5_1, @Ch5_1, N'Lesson 1 & 2: Reading & Key Vocabulary — City Landmarks & Transport', 1, 1520, N'https://www.youtube.com/watch?v=BELlZKpi1Zs', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L5_2)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L5_2, @Ch5_1, N'Lesson 3 & 4: Grammar — Prepositions of Time & Present Simple Timetables', 1, 1650, N'https://www.youtube.com/watch?v=w2Qzb_zG3gI', 0, 2);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L5_3)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L5_3, @Ch5_2, N'Lesson 1 & 2: Vocabulary & Grammar — Comparatives & Deals', 1, 1400, N'https://www.youtube.com/watch?v=QXeEoD0pB3E', 0, 1);

    -- Chapters for Course 6: Social Studies
    DECLARE @Ch6_1 uniqueidentifier = '55555555-6666-6666-6666-666666666601';
    DECLARE @Ch6_2 uniqueidentifier = '55555555-6666-6666-6666-666666666602';

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch6_1)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch6_1, @C6_Id, N'الجغرافيا: قارات العالم وتضاريسها ومناخها', 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Chapters WHERE Id = @Ch6_2)
        INSERT INTO edu.Chapters (Id, CourseId, Title, OrderNum) VALUES (@Ch6_2, @C6_Id, N'التاريخ: مصر تحت الحكم العثماني وبناء الدولة الحديثة', 2);

    -- Lessons for Social Studies
    DECLARE @L6_1 uniqueidentifier = '66666666-6666-6666-6666-666666666601';
    DECLARE @L6_2 uniqueidentifier = '66666666-6666-6666-6666-666666666602';
    DECLARE @L6_3 uniqueidentifier = '66666666-6666-6666-6666-666666666603';

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L6_1)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L6_1, @Ch6_1, N'الدرس 1: قارات العالم (الموقع الجغرافي والمساحة والحدود)', 1, 1650, N'https://www.youtube.com/watch?v=w2Qzb_zG3gI', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L6_2)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L6_2, @Ch6_1, N'الدرس 2: تضاريس قارات العالم (الجبال والهضاب والسهول الفيضية)', 1, 1780, N'https://www.youtube.com/watch?v=4xqAo4XBq9c', 0, 2);

    IF NOT EXISTS (SELECT 1 FROM edu.Lessons WHERE Id = @L6_3)
        INSERT INTO edu.Lessons (Id, ChapterId, Title, Type, DurationSeconds, VideoUrl, IsFreePreview, OrderNum)
        VALUES (@L6_3, @Ch6_2, N'الدرس 3: الحملة الفرنسية على مصر ومقاومة الشعب المصري الباسلة', 1, 1700, N'https://www.youtube.com/watch?v=BELlZKpi1Zs', 0, 1);


    PRINT N'==> [5/14] إدخال مرفقات الدروس والملاحظات والعلامات (Attachments & Notes & Markers)...';

    -- Lesson Attachments (PDFs)
    IF NOT EXISTS (SELECT 1 FROM edu.LessonAttachments WHERE LessonId = @L1_1)
    BEGIN
        INSERT INTO edu.LessonAttachments (Id, LessonId, Title, FileUrl, FileType, SizeBytes, DownloadAllowed)
        VALUES 
        (NEWID(), @L1_1, N'مذكرة شرح حاصل الضرب الديكارتي PDF', N'https://fsedu.local/storage/notes/algebra_cartesian_product.pdf', N'application/pdf', 2450000, 1),
        (NEWID(), @L1_1, N'شيت الواجب والتدريبات المنزلية', N'https://fsedu.local/storage/homework/sheet_cartesian.pdf', N'application/pdf', 1120000, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.LessonAttachments WHERE LessonId = @L3_1)
    BEGIN
        INSERT INTO edu.LessonAttachments (Id, LessonId, Title, FileUrl, FileType, SizeBytes, DownloadAllowed)
        VALUES 
        (NEWID(), @L3_1, N'ملخص قوانين الحركة والسرعة في صفحة واحدة', N'https://fsedu.local/storage/notes/science_speed_summary.pdf', N'application/pdf', 850000, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.LessonAttachments WHERE LessonId = @L4_2)
    BEGIN
        INSERT INTO edu.LessonAttachments (Id, LessonId, Title, FileUrl, FileType, SizeBytes, DownloadAllowed)
        VALUES 
        (NEWID(), @L4_2, N'جدول إعراب المنادى وحالاته الخمسة مع 100 مثال', N'https://fsedu.local/storage/notes/arabic_monada_table.pdf', N'application/pdf', 1780000, 1);
    END;

    -- Lesson Markers (Key video bookmarks)
    IF NOT EXISTS (SELECT 1 FROM edu.LessonMarkers WHERE LessonId = @L1_1)
    BEGIN
        INSERT INTO edu.LessonMarkers (Id, StudentId, LessonId, PositionSec, Label, CreatedAtUtc)
        VALUES 
        (NEWID(), @GaberStudentId, @L1_1, 185, N'الفرق بين الزوج المرتب والمجموعة', GETUTCDATE()),
        (NEWID(), @GaberStudentId, @L1_1, 540, N'خاصية تساوي زوجين مرتبين ومسائل الامتحان', GETUTCDATE()),
        (NEWID(), @GaberStudentId, @L1_1, 920, N'التمثيل بمخطط سهمي ومخطط بياني', GETUTCDATE());
    END;

    -- Lesson Notes
    IF NOT EXISTS (SELECT 1 FROM edu.LessonNotes WHERE StudentId = @GaberStudentId AND LessonId = @L1_1)
    BEGIN
        INSERT INTO edu.LessonNotes (StudentId, LessonId, Body, CreatedAtUtc, UpdatedAtUtc)
        VALUES (@GaberStudentId, @L1_1, N'ملحوظة هامة: إذا كان س × ص = ص × س فإن س = ص أو أحدهما المجموعة الخالية فاي.', GETUTCDATE(), GETUTCDATE());
    END;


    PRINT N'==> [6/14] إدخال بنك الأسئلة الشامل (Question Bank)...';

    DECLARE @Q1 uniqueidentifier = '77777777-1111-1111-1111-111111111101';
    DECLARE @Q2 uniqueidentifier = '77777777-1111-1111-1111-111111111102';
    DECLARE @Q3 uniqueidentifier = '77777777-1111-1111-1111-111111111103';
    DECLARE @Q4 uniqueidentifier = '77777777-1111-1111-1111-111111111104';
    DECLARE @Q5 uniqueidentifier = '77777777-1111-1111-1111-111111111105';
    DECLARE @Q6 uniqueidentifier = '77777777-1111-1111-1111-111111111106';

    -- Math Q1
    IF NOT EXISTS (SELECT 1 FROM edu.Questions WHERE Id = @Q1)
    BEGIN
        INSERT INTO edu.Questions (Id, TeacherId, SubjectId, StageId, Type, Difficulty, QuestionJson, CorrectAnswerJson, Explanation, TagsCsv, CreatedAtUtc, CreatedBy)
        VALUES (@Q1, @T3_Id, 60, 11, 1, 1,
                N'{"text":"إذا كان (س - 1 ، 11) = (8 ، ص + 3) فإن جذر (س + 2ص) يساوي:","options":["5","6","7","8"]}',
                N'{"correct":0}',
                N'بمساواة المسقط الأول بالأول: س - 1 = 8 إذن س = 9. وبمساواة المسقط الثاني: ص + 3 = 11 إذن ص = 8. فيكون جذر (9 + 2×8) = جذر(25) = 5.',
                N'جبر,ضرب ديكارتي,إعدادية', GETUTCDATE(), N'م. وليد الشناوي');
    END;

    -- Math Q2
    IF NOT EXISTS (SELECT 1 FROM edu.Questions WHERE Id = @Q2)
    BEGIN
        INSERT INTO edu.Questions (Id, TeacherId, SubjectId, StageId, Type, Difficulty, QuestionJson, CorrectAnswerJson, Explanation, TagsCsv, CreatedAtUtc, CreatedBy)
        VALUES (@Q2, @T3_Id, 60, 11, 1, 2,
                N'{"text":"إذا كان ن(س) = 3 ، ن(س × ص) = 12 فإن ن(ص²) يساوي:","options":["4","9","16","36"]}',
                N'{"correct":2}',
                N'ن(ص) = ن(س × ص) ÷ ن(س) = 12 ÷ 3 = 4. إذن ن(ص²) = 4² = 16.',
                N'جبر,علاقات ودوال', GETUTCDATE(), N'م. وليد الشناوي');
    END;

    -- Science Q3
    IF NOT EXISTS (SELECT 1 FROM edu.Questions WHERE Id = @Q3)
    BEGIN
        INSERT INTO edu.Questions (Id, TeacherId, SubjectId, StageId, Type, Difficulty, QuestionJson, CorrectAnswerJson, Explanation, TagsCsv, CreatedAtUtc, CreatedBy)
        VALUES (@Q3, @T1_Id, 62, 11, 1, 2,
                N'{"text":"سيارة تتحرك بسرعة 72 كم/ساعة، تكون سرعتها بوحدة متر/ثانية مساوية لـ:","options":["20 م/ث","15 م/ث","25 م/ث","30 م/ث"]}',
                N'{"correct":0}',
                N'للتحويل من كم/س إلى م/ث نضرب في (5 ÷ 18): 72 × (5/18) = 4 × 5 = 20 م/ث.',
                N'علوم,حركة,سرعة', GETUTCDATE(), N'أ.د. حسام إبراهيم');
    END;

    -- Science Q4
    IF NOT EXISTS (SELECT 1 FROM edu.Questions WHERE Id = @Q4)
    BEGIN
        INSERT INTO edu.Questions (Id, TeacherId, SubjectId, StageId, Type, Difficulty, QuestionJson, CorrectAnswerJson, Explanation, TagsCsv, CreatedAtUtc, CreatedBy)
        VALUES (@Q4, @T1_Id, 62, 11, 2, 1,
                N'{"text":"السرعة النسبية لجسم متحرك في نفس اتجاه حركة المراقب وبنفس سرعته تساوي صفر."}',
                N'{"correct":true}',
                N'صواب؛ لأن السرعة النسبية في نفس الاتجاه = السرعة الفعلية - سرعة المراقب = 0.',
                N'علوم,سرعة نسبية', GETUTCDATE(), N'أ.د. حسام إبراهيم');
    END;

    -- Arabic Q5
    IF NOT EXISTS (SELECT 1 FROM edu.Questions WHERE Id = @Q5)
    BEGIN
        INSERT INTO edu.Questions (Id, TeacherId, SubjectId, StageId, Type, Difficulty, QuestionJson, CorrectAnswerJson, Explanation, TagsCsv, CreatedAtUtc, CreatedBy)
        VALUES (@Q5, @T2_Id, 59, 11, 1, 2,
                N'{"text":"«يا طالباً، اجتهد في دراستك». نوع المنادى في الجملة السابقة هو:","options":["نكرة غير مقصودة","نكرة مقصودة","منادى مضاف","شبيه بالمضاف"]}',
                N'{"correct":0}',
                N'جاءت كلمة طالباً كلمة واحدة منصوبة ومنونة ولا يوجد بعدها ما يكمل معناها، فهو منادى نكرة غير مقصودة منصوب بالفتحة.',
                N'عربي,نحو,منادى', GETUTCDATE(), N'أ. رضا الفاروق');
    END;

    -- Arabic Q6
    IF NOT EXISTS (SELECT 1 FROM edu.Questions WHERE Id = @Q6)
    BEGIN
        INSERT INTO edu.Questions (Id, TeacherId, SubjectId, StageId, Type, Difficulty, QuestionJson, CorrectAnswerJson, Explanation, TagsCsv, CreatedAtUtc, CreatedBy)
        VALUES (@Q6, @T2_Id, 59, 11, 1, 3,
                N'{"text":"«أعجبني الطالبُ خلقُه». إعراب كلمة (خلقُه) هو:","options":["بدل اشتمال مرفوع بالضمة","مفعول به ثانٍ منصوب","نعت مرفوع بالضمة","فاعل ثانٍ"]}',
                N'{"correct":0}',
                N'الخلق مما يشتمل عليه الطالب وهو أمر معنوي ويشتمل على ضمير يعود على المبدل منه، فيعرب بدل اشتمال مرفوع لأن الطالب فاعل مرفوع.',
                N'عربي,نحو,بدل', GETUTCDATE(), N'أ. رضا الفاروق');
    END;


    PRINT N'==> [7/14] إدخال الاختبارات الدورية ومحاولات الطلاب (Assessments & Attempts)...';

    DECLARE @Ass1 uniqueidentifier = '88888888-1111-1111-1111-111111111101';
    DECLARE @Ass2 uniqueidentifier = '88888888-2222-2222-2222-222222222202';

    IF NOT EXISTS (SELECT 1 FROM edu.Assessments WHERE Id = @Ass1)
    BEGIN
        INSERT INTO edu.Assessments (Id, CourseId, TeacherId, Type, Title, Description, TimeLimitMinutes, TotalMarks, PassingMarks, AttemptsAllowed, ShuffleQuestions, ShowResults, CreatedAtUtc, CreatedBy)
        VALUES (@Ass1, @C1_Id, @T3_Id, 2, N'اختبار قصير 1: حاصل الضرب الديكارتي والعلاقات',
                N'كويز سريع لقياس فهم درس حاصل الضرب الديكارتي وخصائص تساوي الأزواج المرتبة، 5 أسئلة اختيار من متعدد.',
                30, 20.0, 12.0, 2, 1, N'Immediately', GETUTCDATE(), N'م. وليد الشناوي');

        INSERT INTO edu.AssessmentQuestions (AssessmentId, QuestionId, Marks, OrderNum)
        VALUES (@Ass1, @Q1, 10.0, 1), (@Ass1, @Q2, 10.0, 2);
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.Assessments WHERE Id = @Ass2)
    BEGIN
        INSERT INTO edu.Assessments (Id, CourseId, TeacherId, Type, Title, Description, TimeLimitMinutes, TotalMarks, PassingMarks, AttemptsAllowed, ShuffleQuestions, ShowResults, CreatedAtUtc, CreatedBy)
        VALUES (@Ass2, @C3_Id, @T1_Id, 2, N'اختبار تدريبي: القوى والسرعة والعجلة',
                N'اختبار تدريبي على مفاهيم الحركة والسرعة النسبية والتمثيل البياني.',
                25, 20.0, 10.0, 3, 1, N'Immediately', GETUTCDATE(), N'أ.د. حسام إبراهيم');

        INSERT INTO edu.AssessmentQuestions (AssessmentId, QuestionId, Marks, OrderNum)
        VALUES (@Ass2, @Q3, 10.0, 1), (@Ass2, @Q4, 10.0, 2);
    END;

    -- Attempts by Gaber Anwar
    IF NOT EXISTS (SELECT 1 FROM edu.AssessmentAttempts WHERE AssessmentId = @Ass1 AND StudentId = @GaberStudentId)
    BEGIN
        INSERT INTO edu.AssessmentAttempts (Id, AssessmentId, StudentId, StartedAtUtc, SubmittedAtUtc, Score, AutoGraded, AnswersJson, Status)
        VALUES (NEWID(), @Ass1, @GaberStudentId, DATEADD(hour, -5, GETUTCDATE()), DATEADD(minute, -280, GETUTCDATE()), 20.0, 1,
                N'[{"questionId":"77777777-1111-1111-1111-111111111101","answerJson":"0"},{"questionId":"77777777-1111-1111-1111-111111111102","answerJson":"2"}]', 3);
    END;

    -- Attempt by Maryam Ibrahim
    IF NOT EXISTS (SELECT 1 FROM edu.AssessmentAttempts WHERE AssessmentId = @Ass1 AND StudentId = @S1_Id)
    BEGIN
        INSERT INTO edu.AssessmentAttempts (Id, AssessmentId, StudentId, StartedAtUtc, SubmittedAtUtc, Score, AutoGraded, AnswersJson, Status)
        VALUES (NEWID(), @Ass1, @S1_Id, DATEADD(day, -1, GETUTCDATE()), DATEADD(minute, -1415, GETUTCDATE()), 20.0, 1,
                N'[{"questionId":"77777777-1111-1111-1111-111111111101","answerJson":"0"},{"questionId":"77777777-1111-1111-1111-111111111102","answerJson":"2"}]', 3);
    END;


    PRINT N'==> [8/14] إدخال امتحانات المحافظات الرسمية والسنوات السابقة (Past Papers)...';

    -- Past Paper 1: Cairo Governorate 2024 (Algebra)
    DECLARE @PP1_Id uniqueidentifier = '99999999-1111-1111-1111-111111111101';
    IF NOT EXISTS (SELECT 1 FROM edu.PastPapers WHERE Id = @PP1_Id)
    BEGIN
        INSERT INTO edu.PastPapers (Id, SubjectId, StageId, Year, Term, ExamType, EducationalAdministration, Title, Description, DurationMinutes, TotalMarks, Published, CreatedAtUtc, CreatedBy)
        VALUES (@PP1_Id, 60, 11, 2024, 1, 3, N'مديرية التربية والتعليم بمحافظة القاهرة',
                N'امتحان الجبر والإحصاء — الشهادة الإعدادية محافظة القاهرة 2024 (الترم الأول)',
                N'الامتحان الرسمي المعتمد لمحافظة القاهرة مع نموذج الإجابة وتوزيع الدرجات كاملاً لجميع الأسئلة المقالية والاختيارية.',
                120, 30.0, 1, GETUTCDATE(), N'Admin');

        -- Add questions to Past Paper 1
        INSERT INTO edu.PastPaperQuestions (Id, PastPaperId, Body, Type, OptionsJson, CorrectAnswerJson, Marks, OrderNum, Explanation)
        VALUES
        (NEWID(), @PP1_Id, N'النقطة (-3 ، 4) تقع في الربع:', 1, N'["الأول","الثاني","الثالث","الرابع"]', N'الثاني', 2.0, 1, N'س سالب و ص موجب إذن تقع في الربع الثاني.'),
        (NEWID(), @PP1_Id, N'إذا كان 2 ، س ، 4 ، 8 كميات متناسبة فإن س تساوي:', 1, N'["1","4","8","16"]', N'4', 2.0, 2, N'2 / س = 4 / 8 => 4س = 16 => س = 4.'),
        (NEWID(), @PP1_Id, N'المدى لمجموعة القيم 7 ، 3 ، 6 ، 9 ، 5 هو:', 1, N'["3","4","6","9"]', N'6', 2.0, 3, N'المدى = أكبر قيمة - أصغر قيمة = 9 - 3 = 6.'),
        (NEWID(), @PP1_Id, N'الدالة د(س) = س² (س - 3)² هي دالة كثيرة حدود من الدرجة:', 1, N'["الثانية","الثالثة","الرابعة","الخامسة"]', N'الرابعة', 2.0, 4, N'س² × س² = س⁴ فهي من الدرجة الرابعة.'),
        (NEWID(), @PP1_Id, N'إذا كانت س = {1 ، 2} ، ص = {3 ، 4} فإن (2 ، 4) تنتمي إلى:', 1, N'["س × ص","ص × س","س²","ص²"]', N'س × ص', 2.0, 5, N'المسقط الأول 2 ينتمي لـ س، والمسقط الثاني 4 ينتمي لـ ص إذن تنتمي لـ (س × ص).');
    END;

    -- Past Paper 2: Giza Governorate 2024 (Science)
    DECLARE @PP2_Id uniqueidentifier = '99999999-2222-2222-2222-222222222202';
    IF NOT EXISTS (SELECT 1 FROM edu.PastPapers WHERE Id = @PP2_Id)
    BEGIN
        INSERT INTO edu.PastPapers (Id, SubjectId, StageId, Year, Term, ExamType, EducationalAdministration, Title, Description, DurationMinutes, TotalMarks, Published, CreatedAtUtc, CreatedBy)
        VALUES (@PP2_Id, 62, 11, 2024, 1, 3, N'مديرية التربية والتعليم بمحافظة الجيزة',
                N'امتحان مادة العلوم — الشهادة الإعدادية محافظة الجيزة 2024 (الترم الأول)',
                N'امتحان العلوم الرسمي لمحافظة الجيزة شاملاً أسئلة السرعة والمرايا والعدسات ونشأة الكون والانقسام الميتوزي والميوزي.',
                120, 40.0, 1, GETUTCDATE(), N'Admin');

        INSERT INTO edu.PastPaperQuestions (Id, PastPaperId, Body, Type, OptionsJson, CorrectAnswerJson, Marks, OrderNum, Explanation)
        VALUES
        (NEWID(), @PP2_Id, N'تعتبر القوة كمية فيزيائية:', 1, N'["قياسية فقط","متجهة يلزم لمعرفتها المقدار والاتجاه","أساسية","لا شيء مما سبق"]', N'متجهة يلزم لمعرفتها المقدار والاتجاه', 4.0, 1, N'القوة كمية متجهة يلزم لمعرفتها تحديد مقدارها واتجاهها ونقطة تأثيرها.'),
        (NEWID(), @PP2_Id, N'قطعة ضوئية سميكة عند منتصفها ورقيقة عند طرفيها تسمى:', 1, N'["عدسة محدبة","عدسة مقعرة","مرآة مقعرة","مرآة مستوية"]', N'عدسة محدبة', 4.0, 2, N'العدسة المحدبة مجمعة للأشعة وسميكة في المركز.'),
        (NEWID(), @PP2_Id, N'تتكاثر الهيدرا لا جنسياً عن طريق:', 1, N'["الانشطار الثنائي","التبرعم","التجدد","الأبواغ والجراثيم"]', N'التبرعم', 4.0, 3, N'التبرعم في الهيدرا وفطر الخميرة.'),
        (NEWID(), @PP2_Id, N'مؤسس نظرية النجم العابر حول نشأة المجموعة الشمسية هما العالمان:', 1, N'["لابلاس","فريد هويل","تشامبرلن ومولتن","نيوتن"]', N'تشامبرلن ومولتن', 4.0, 4, N'العالمان تشامبرلن ومولتن هما واضعا نظرية النجم العابر.');
    END;

    -- Past Paper 3: Beni Suef Governorate 2024 (Arabic)
    DECLARE @PP3_Id uniqueidentifier = '99999999-3333-3333-3333-333333333303';
    IF NOT EXISTS (SELECT 1 FROM edu.PastPapers WHERE Id = @PP3_Id)
    BEGIN
        INSERT INTO edu.PastPapers (Id, SubjectId, StageId, Year, Term, ExamType, EducationalAdministration, Title, Description, DurationMinutes, TotalMarks, Published, CreatedAtUtc, CreatedBy)
        VALUES (@PP3_Id, 59, 11, 2024, 1, 3, N'مديرية التربية والتعليم بمحافظة بني سويف',
                N'امتحان اللغة العربية — الشهادة الإعدادية محافظة بني سويف 2024 (الترم الأول)',
                N'امتحان المحافظة الرسمي المتميز في التعبير والقراءة والنصوص وقواعد النحو والخط العربي والإملاء.',
                150, 80.0, 1, GETUTCDATE(), N'Admin');

        INSERT INTO edu.PastPaperQuestions (Id, PastPaperId, Body, Type, OptionsJson, CorrectAnswerJson, Marks, OrderNum, Explanation)
        VALUES
        (NEWID(), @PP3_Id, N'مرادف كلمة (هوناً) في قوله تعالى: ﴿يمشون على الأرض هوناً﴾ هو:', 1, N'["سكينة وتواضعاً","تكبراً وفخراً","خوفاً وضعفاً","سرعة وعجلة"]', N'سكينة وتواضعاً', 5.0, 1, N'الهون هو السكينة والوقار والتواضع بغير استكبار.'),
        (NEWID(), @PP3_Id, N'«يا أبناءَ مصر، اعملوا بإخلاص». كلمة (أبناء) منادى نوعه:', 1, N'["مضاف منصوب","شبيه بالمضاف","نكرة مقصودة","علم مفرد"]', N'مضاف منصوب', 5.0, 2, N'أبناء أضيفت إلى كلمة مصر فهو منادى مضاف منصوب وعلامة نصبه الفتحة.'),
        (NEWID(), @PP3_Id, N'«نعم العمل الصدق». فاعل نعم في الجملة السابقة هو:', 1, N'["العمل","الصدق","ضمير مستتر","محذوف"]', N'العمل', 5.0, 3, N'العمل معرف بأل وهو فاعل نعم مرفوع بالضمة.');
    END;

    -- Past Paper Attempt for Gaber Anwar (Beni Suef Exam!)
    IF NOT EXISTS (SELECT 1 FROM edu.PastPaperAttempts WHERE PastPaperId = @PP3_Id AND StudentId = @GaberStudentId)
    BEGIN
        INSERT INTO edu.PastPaperAttempts (Id, PastPaperId, StudentId, StartedAtUtc, SubmittedAtUtc, Score, MaxScore, Status, AnswersJson)
        VALUES (NEWID(), @PP3_Id, @GaberStudentId, DATEADD(day, -2, GETUTCDATE()), DATEADD(minute, -2760, GETUTCDATE()), 80.0, 80.0, 3,
                N'{"answers":[{"questionId":"1","answer":"سكينة وتواضعاً"},{"questionId":"2","answer":"مضاف منصوب"},{"questionId":"3","answer":"العمل"}]}');
    END;


    PRINT N'==> [9/14] إدخال الواجبات المدرسية والتسليمات والتصحيح (Homeworks & Submissions)...';

    DECLARE @HW1_Id uniqueidentifier = 'AAAAAAAA-1111-1111-1111-111111111101';
    DECLARE @HW2_Id uniqueidentifier = 'AAAAAAAA-2222-2222-2222-222222222202';

    IF NOT EXISTS (SELECT 1 FROM edu.Homeworks WHERE Id = @HW1_Id)
    BEGIN
        INSERT INTO edu.Homeworks (Id, CourseId, TeacherId, Title, Description, AttachmentUrl, DueDateUtc, MaxScore, Published, CreatedAtUtc, CreatedBy)
        VALUES (@HW1_Id, @C1_Id, @T3_Id, N'الواجب الأول: تمارين حاصل الضرب الديكارتي ومسائل المتفوقين',
                N'حل التمارين من ص 12 إلى ص 14 في المذكرة، مع رسم المخطط السهمي والبياني للعلاقة بدقة وتوضيح خطوات الحل.',
                N'https://fsedu.local/storage/homework/hw1_cartesian.pdf',
                DATEADD(day, 7, GETUTCDATE()), 20.0, 1, GETUTCDATE(), N'م. وليد الشناوي');
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.Homeworks WHERE Id = @HW2_Id)
    BEGIN
        INSERT INTO edu.Homeworks (Id, CourseId, TeacherId, Title, Description, AttachmentUrl, DueDateUtc, MaxScore, Published, CreatedAtUtc, CreatedBy)
        VALUES (@HW2_Id, @C3_Id, @T1_Id, N'واجب العلوم: مسائل العجلة المنتظمة والسرعة النسبية',
                N'حل مسائل الرسم البياني للحركة وحساب السرعة المتوسطة والعجلة مع بيان نوعها (تزايدية / تناقصية).',
                N'https://fsedu.local/storage/homework/hw_science_speed.pdf',
                DATEADD(day, 5, GETUTCDATE()), 20.0, 1, GETUTCDATE(), N'أ.د. حسام إبراهيم');
    END;

    -- Submissions by Gaber Anwar
    IF NOT EXISTS (SELECT 1 FROM edu.HomeworkSubmissions WHERE HomeworkId = @HW1_Id AND StudentId = @GaberStudentId)
    BEGIN
        INSERT INTO edu.HomeworkSubmissions (Id, HomeworkId, StudentId, SubmittedAtUtc, Body, AttachmentUrl, Late, GradedAtUtc, GradedByUserId, Score, FeedbackBody, CreatedAtUtc, CreatedBy)
        VALUES (NEWID(), @HW1_Id, @GaberStudentId, DATEADD(day, -1, GETUTCDATE()),
                N'مرفق لحضرتك مستر وليد حل تمارين حاصل الضرب الديكارتي كاملاً مع رسم المخططات السهمية والبيانية والمسائل الإضافية.',
                N'https://fsedu.local/storage/submissions/gaber_hw1_math.pdf', 0,
                DATEADD(hour, -4, GETUTCDATE()), @T3_Id, 19.5,
                N'ممتاز جداً يا جابر! إجابات نموذجية ورسم هندسي دقيق، ملحوظتك في السؤال الثالث ممتازة. استمر بنفس هذا المستوى يا بطل.',
                GETUTCDATE(), N'Student');
    END;


    PRINT N'==> [10/14] إدخال الاشتراكات والالتحاق بالدورات (Subscriptions & Enrollments)...';

    -- Enroll Gaber Anwar in all Prep 3 courses
    IF NOT EXISTS (SELECT 1 FROM edu.Enrollments WHERE StudentId = @GaberStudentId AND CourseId = @C1_Id)
        INSERT INTO edu.Enrollments (Id, StudentId, CourseId, EnrolledAtUtc, ProgressPct, CompletedAtUtc) VALUES (NEWID(), @GaberStudentId, @C1_Id, DATEADD(month, -1, GETUTCDATE()), 75.0, NULL);

    IF NOT EXISTS (SELECT 1 FROM edu.Enrollments WHERE StudentId = @GaberStudentId AND CourseId = @C2_Id)
        INSERT INTO edu.Enrollments (Id, StudentId, CourseId, EnrolledAtUtc, ProgressPct, CompletedAtUtc) VALUES (NEWID(), @GaberStudentId, @C2_Id, DATEADD(month, -1, GETUTCDATE()), 60.0, NULL);

    IF NOT EXISTS (SELECT 1 FROM edu.Enrollments WHERE StudentId = @GaberStudentId AND CourseId = @C3_Id)
        INSERT INTO edu.Enrollments (Id, StudentId, CourseId, EnrolledAtUtc, ProgressPct, CompletedAtUtc) VALUES (NEWID(), @GaberStudentId, @C3_Id, DATEADD(month, -1, GETUTCDATE()), 85.0, NULL);

    IF NOT EXISTS (SELECT 1 FROM edu.Enrollments WHERE StudentId = @GaberStudentId AND CourseId = @C4_Id)
        INSERT INTO edu.Enrollments (Id, StudentId, CourseId, EnrolledAtUtc, ProgressPct, CompletedAtUtc) VALUES (NEWID(), @GaberStudentId, @C4_Id, DATEADD(month, -1, GETUTCDATE()), 80.0, NULL);

    IF NOT EXISTS (SELECT 1 FROM edu.Enrollments WHERE StudentId = @GaberStudentId AND CourseId = @C5_Id)
        INSERT INTO edu.Enrollments (Id, StudentId, CourseId, EnrolledAtUtc, ProgressPct, CompletedAtUtc) VALUES (NEWID(), @GaberStudentId, @C5_Id, DATEADD(month, -1, GETUTCDATE()), 55.0, NULL);

    IF NOT EXISTS (SELECT 1 FROM edu.Enrollments WHERE StudentId = @GaberStudentId AND CourseId = @C6_Id)
        INSERT INTO edu.Enrollments (Id, StudentId, CourseId, EnrolledAtUtc, ProgressPct, CompletedAtUtc) VALUES (NEWID(), @GaberStudentId, @C6_Id, DATEADD(month, -1, GETUTCDATE()), 70.0, NULL);

    -- Enroll Maryam in Algebra & Science
    IF NOT EXISTS (SELECT 1 FROM edu.Enrollments WHERE StudentId = @S1_Id AND CourseId = @C1_Id)
        INSERT INTO edu.Enrollments (Id, StudentId, CourseId, EnrolledAtUtc, ProgressPct, CompletedAtUtc) VALUES (NEWID(), @S1_Id, @C1_Id, DATEADD(day, -20, GETUTCDATE()), 90.0, NULL);

    IF NOT EXISTS (SELECT 1 FROM edu.Enrollments WHERE StudentId = @S1_Id AND CourseId = @C3_Id)
        INSERT INTO edu.Enrollments (Id, StudentId, CourseId, EnrolledAtUtc, ProgressPct, CompletedAtUtc) VALUES (NEWID(), @S1_Id, @C3_Id, DATEADD(day, -20, GETUTCDATE()), 95.0, NULL);

    -- Active StageFullTerm Subscription for Gaber Anwar
    IF NOT EXISTS (SELECT 1 FROM edu.Subscriptions WHERE UserId = @GaberStudentId AND Type = 3 AND Status = 2)
    BEGIN
        INSERT INTO edu.Subscriptions (Id, UserId, Type, SubjectId, StageId, Term, StartsAtUtc, EndsAtUtc, AutoRenew, AmountPaid, Currency, Status, PaymentProvider, ProviderReference, CreatedAtUtc, CreatedBy)
        VALUES (NEWID(), @GaberStudentId, 3, NULL, 11, 1, DATEADD(month, -1, GETUTCDATE()), DATEADD(month, 5, GETUTCDATE()), 0, 1900.00, N'EGP', 2, 99, N'SUB-PREP3-FULLTERM', GETUTCDATE(), N'Admin');
    END;

    -- Active StageFullTerm Subscription for Maryam
    IF NOT EXISTS (SELECT 1 FROM edu.Subscriptions WHERE UserId = @S1_Id AND Type = 3 AND Status = 2)
    BEGIN
        INSERT INTO edu.Subscriptions (Id, UserId, Type, SubjectId, StageId, Term, StartsAtUtc, EndsAtUtc, AutoRenew, AmountPaid, Currency, Status, PaymentProvider, ProviderReference, CreatedAtUtc, CreatedBy)
        VALUES (NEWID(), @S1_Id, 3, NULL, 11, 1, DATEADD(day, -20, GETUTCDATE()), DATEADD(month, 5, GETUTCDATE()), 0, 1900.00, N'EGP', 2, 1, N'PAYMOB-INSTAPAY-29381', GETUTCDATE(), N'Student');
    END;


    PRINT N'==> [11/14] إدخال تقييمات وآراء الطلاب الحقيقية (Course Reviews)...';

    IF NOT EXISTS (SELECT 1 FROM edu.CourseReviews WHERE CourseId = @C1_Id AND StudentId = @GaberStudentId)
        INSERT INTO edu.CourseReviews (CourseId, StudentId, Rating, Comment, CreatedAtUtc)
        VALUES (@C1_Id, @GaberStudentId, 5, N'شرح المهندس وليد الشناوي من أروع ما يكون، بجد بيبسط المفاهيم والمسائل الصعبة بطريقة سلسة جداً والتمارين بتغطي كل أفكار الامتحانات.', GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM edu.CourseReviews WHERE CourseId = @C1_Id AND StudentId = @S1_Id)
        INSERT INTO edu.CourseReviews (CourseId, StudentId, Rating, Comment, CreatedAtUtc)
        VALUES (@C1_Id, @S1_Id, 5, N'أفضل مدرس رياضيات أتابع معاه على الإطلاق، الخرائط الذهنية سهلت علي حفظ القوانين والتطبيق المباشر عليها.', DATEADD(day, -3, GETUTCDATE()));

    IF NOT EXISTS (SELECT 1 FROM edu.CourseReviews WHERE CourseId = @C3_Id AND StudentId = @GaberStudentId)
        INSERT INTO edu.CourseReviews (CourseId, StudentId, Rating, Comment, CreatedAtUtc)
        VALUES (@C3_Id, @GaberStudentId, 5, N'دكتور حسام إبراهيم معلم قدير ومتميز، الشرح والتجارب المعملية سهلت فهم درس المرايا والعدسات جداً.', DATEADD(day, -5, GETUTCDATE()));

    IF NOT EXISTS (SELECT 1 FROM edu.CourseReviews WHERE CourseId = @C4_Id AND StudentId = @S2_Id)
        INSERT INTO edu.CourseReviews (CourseId, StudentId, Rating, Comment, CreatedAtUtc)
        VALUES (@C4_Id, @S2_Id, 5, N'مستر رضا الفاروق أستاذ النحو بدون منازع، لأول مرة أفهم درس المنادى وأفرّق بين النكرة المقصودة وغير المقصودة بسهولة.', DATEADD(day, -2, GETUTCDATE()));


    PRINT N'==> [12/14] إدخال الإعلانات والمناقشات المجتمعية (Announcements & Discussions)...';

    -- Announcements
    IF NOT EXISTS (SELECT 1 FROM edu.CourseAnnouncements WHERE CourseId = @C1_Id)
    BEGIN
        INSERT INTO edu.CourseAnnouncements (Id, CourseId, AuthorUserId, AuthorName, Title, Body, Pinned, CreatedAtUtc)
        VALUES 
        (NEWID(), @C1_Id, @T3_Id, N'م. وليد الشناوي', N'📢 موعد الحصة التفاعلية المباشرة ليلة الخميس القادم',
         N'أبنائي وبناتي طلاب الشهادة الإعدادية، سنلتقي بإذن الله يوم الخميس الساعة 7:00 مساءً في حصة لايف مخصصة لحل أصعب مسائل حاصل الضرب الديكارتي والإجابة عن جميع استفساراتكم.', 1, GETUTCDATE()),
        (NEWID(), @C1_Id, @T3_Id, N'م. وليد الشناوي', N'📁 تم رفع مذكرة مراجعة القوانين والتمارين المحلولة PDF',
         N'تم رفع ملف ملخص قوانين الجبر وحساب المثلثات في خانة المرفقات للدرس الثالث، يمكنكم تحميلها وطباعتها للمذاكرة.', 0, DATEADD(day, -4, GETUTCDATE()));
    END;

    -- Discussions
    DECLARE @Disc1_Id uniqueidentifier = 'BBBBBBBB-1111-1111-1111-111111111101';
    IF NOT EXISTS (SELECT 1 FROM edu.CourseDiscussions WHERE Id = @Disc1_Id)
    BEGIN
        INSERT INTO edu.CourseDiscussions (Id, CourseId, AuthorUserId, AuthorName, Title, Body, CreatedAtUtc, Pinned, Locked, RepliesCount, LastActivityAtUtc)
        VALUES (@Disc1_Id, @C1_Id, @GaberStudentId, N'جابر أنور', N'سؤال بخصوص حاصل الضرب الديكارتي للمجموعات الخالية',
                N'أستاذنا الفاضل، هل س × فاي = فاي × س = فاي دائماً حتى لو كانت س مجموعة غير خالية؟',
                DATEADD(day, -2, GETUTCDATE()), 0, 0, 1, DATEADD(day, -1, GETUTCDATE()));

        INSERT INTO edu.CourseDiscussionReplies (Id, DiscussionId, AuthorUserId, AuthorName, Body, CreatedAtUtc)
        VALUES (NEWID(), @Disc1_Id, @T3_Id, N'م. وليد الشناوي',
                N'أهلاً بك يا جابر، نعم تماماً؛ حاصل ضرب أي مجموعة في المجموعة الخالية ∅ ينتج عنه المجموعة الخالية ∅، لأن الزوج المرتب يتطلب وجود عنصر من كلتا المجموعتين.',
                DATEADD(day, -1, GETUTCDATE()));
    END;

    -- Study Communities
    DECLARE @Comm1_Id uniqueidentifier = 'CCCCCCCC-1111-1111-1111-111111111101';
    DECLARE @Comm2_Id uniqueidentifier = 'CCCCCCCC-2222-2222-2222-222222222202';

    IF NOT EXISTS (SELECT 1 FROM edu.Communities WHERE Id = @Comm1_Id)
    BEGIN
        INSERT INTO edu.Communities (Id, StageId, Name, MaxMembers, SupervisorId)
        VALUES (@Comm1_Id, 11, N'مجتمع أوائل الشهادة الإعدادية 2025', 500, @T1_Id);

        INSERT INTO edu.CommunityMembers (CommunityId, UserId, Role, JoinedAtUtc)
        VALUES 
        (@Comm1_Id, @T1_Id, 3, GETUTCDATE()),
        (@Comm1_Id, @T3_Id, 2, GETUTCDATE()),
        (@Comm1_Id, @GaberStudentId, 1, GETUTCDATE()),
        (@Comm1_Id, @S1_Id, 1, GETUTCDATE()),
        (@Comm1_Id, @S2_Id, 1, GETUTCDATE());
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.Communities WHERE Id = @Comm2_Id)
    BEGIN
        INSERT INTO edu.Communities (Id, StageId, Name, MaxMembers, SupervisorId)
        VALUES (@Comm2_Id, 11, N'نادي عباقرة الرياضيات والعلوم', 300, @T3_Id);

        INSERT INTO edu.CommunityMembers (CommunityId, UserId, Role, JoinedAtUtc)
        VALUES 
        (@Comm2_Id, @T3_Id, 3, GETUTCDATE()),
        (@Comm2_Id, @GaberStudentId, 1, GETUTCDATE()),
        (@Comm2_Id, @S1_Id, 1, GETUTCDATE());
    END;


    PRINT N'==> [13/14] إدخال الحصص المباشرة وسجل الحضور (Live Sessions & Attendance)...';

    DECLARE @Live1_Id uniqueidentifier = 'DDDDDDDD-1111-1111-1111-111111111101';
    DECLARE @Live2_Id uniqueidentifier = 'DDDDDDDD-2222-2222-2222-222222222202';

    IF NOT EXISTS (SELECT 1 FROM edu.LiveSessions WHERE Id = @Live1_Id)
    BEGIN
        INSERT INTO edu.LiveSessions (Id, LessonId, TeacherId, RoomId, Status, ScheduledAtUtc, StartedAtUtc, EndedAtUtc, RecordingUrl, RecordingStatus, MaxParticipants, Description, DurationMinutes, StageId, SubjectId, Title)
        VALUES (@Live1_Id, NULL, @T3_Id, N'room_prep3_math_rev', 1, DATEADD(day, 3, GETUTCDATE()), NULL, NULL, NULL, NULL, 200,
                N'مراجعة شاملة ليلة الامتحان على وحدة العلاقات والدوال وحل مسائل امتحانات المحافظات الصعبة.', 60, 11, 60,
                N'حصة مباشرة: المراجعة الشاملة على العلاقات والدوال 3 إعدادي');
    END;

    IF NOT EXISTS (SELECT 1 FROM edu.LiveSessions WHERE Id = @Live2_Id)
    BEGIN
        INSERT INTO edu.LiveSessions (Id, LessonId, TeacherId, RoomId, Status, ScheduledAtUtc, StartedAtUtc, EndedAtUtc, RecordingUrl, RecordingStatus, MaxParticipants, Description, DurationMinutes, StageId, SubjectId, Title)
        VALUES (@Live2_Id, NULL, @T1_Id, N'room_prep3_sci_ended', 3, DATEADD(day, -2, GETUTCDATE()), DATEADD(day, -2, GETUTCDATE()), DATEADD(minute, -2820, GETUTCDATE()),
                N'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4', N'Ready', 200,
                N'تسجيل حصة البث المباشر لشرح تجارب المرايا والعدسات المعملية.', 60, 11, 62,
                N'حصة مسجلة: ورشة العمل المعملية في الضوء والمرايا');

        INSERT INTO edu.LiveAttendance (SessionId, StudentId, JoinedAtUtc, LeftAtUtc, TotalDurationSec, AttentionScore)
        VALUES 
        (@Live2_Id, @GaberStudentId, DATEADD(day, -2, GETUTCDATE()), DATEADD(minute, -2820, GETUTCDATE()), 3540, 96.5),
        (@Live2_Id, @S1_Id, DATEADD(day, -2, GETUTCDATE()), DATEADD(minute, -2825, GETUTCDATE()), 3480, 98.0);
    END;


    PRINT N'==> [14/14] إدخال الكوبونات والحملات والشهادات والشارات (Coupons, Campaigns, Badges, Certs)...';

    -- Coupons
    IF NOT EXISTS (SELECT 1 FROM edu.Coupons WHERE Code = N'EXAM2025')
        INSERT INTO edu.Coupons (Code, DiscountPct, DiscountFixed, ValidUntilUtc, MaxUses, UsedCount, Active)
        VALUES (N'EXAM2025', 25.0, NULL, DATEADD(month, 6, GETUTCDATE()), 500, 14, 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Coupons WHERE Code = N'WELCOME50')
        INSERT INTO edu.Coupons (Code, DiscountPct, DiscountFixed, ValidUntilUtc, MaxUses, UsedCount, Active)
        VALUES (N'WELCOME50', 50.0, NULL, DATEADD(month, 3, GETUTCDATE()), 200, 32, 1);

    IF NOT EXISTS (SELECT 1 FROM edu.Coupons WHERE Code = N'TOPSTUDENT')
        INSERT INTO edu.Coupons (Code, DiscountPct, DiscountFixed, ValidUntilUtc, MaxUses, UsedCount, Active)
        VALUES (N'TOPSTUDENT', NULL, 100.0, DATEADD(month, 12, GETUTCDATE()), 1000, 5, 1);

    -- Sales Campaign
    IF NOT EXISTS (SELECT 1 FROM edu.SalesCampaigns WHERE Title LIKE N'%أوائل%')
    BEGIN
        INSERT INTO edu.SalesCampaigns (Title, DiscountPct, Scope, ScopeId, ValidFromUtc, ValidUntilUtc, Active, CreatedAtUtc)
        VALUES (N'عرض التفوق للشهادة الإعدادية — خصم 20% على باقة الترم الكاملة', 20.0, 2, 11, DATEADD(day, -5, GETUTCDATE()), DATEADD(month, 2, GETUTCDATE()), 1, GETUTCDATE());
    END;

    -- Certificate of Completion for Gaber Anwar
    IF NOT EXISTS (SELECT 1 FROM edu.Certificates WHERE StudentId = @GaberStudentId AND CourseId = @C1_Id)
    BEGIN
        INSERT INTO edu.Certificates (Id, StudentId, CourseId, CertificateNumber, IssuedAtUtc, FinalGrade)
        VALUES (NEWID(), @GaberStudentId, @C1_Id, N'FSEDU-CERT-2025-08491', DATEADD(day, -3, GETUTCDATE()), 97.5);
    END;

    -- Badges for Gaber Anwar
    IF NOT EXISTS (SELECT 1 FROM edu.StudentBadges WHERE StudentId = @GaberStudentId AND BadgeId = 1)
        INSERT INTO edu.StudentBadges (StudentId, BadgeId, AwardedAtUtc) VALUES (@GaberStudentId, 1, DATEADD(day, -20, GETUTCDATE()));

    IF NOT EXISTS (SELECT 1 FROM edu.StudentBadges WHERE StudentId = @GaberStudentId AND BadgeId = 2)
        INSERT INTO edu.StudentBadges (StudentId, BadgeId, AwardedAtUtc) VALUES (@GaberStudentId, 2, DATEADD(day, -10, GETUTCDATE()));

    IF NOT EXISTS (SELECT 1 FROM edu.StudentBadges WHERE StudentId = @GaberStudentId AND BadgeId = 4)
        INSERT INTO edu.StudentBadges (StudentId, BadgeId, AwardedAtUtc) VALUES (@GaberStudentId, 4, DATEADD(day, -5, GETUTCDATE()));


    COMMIT TRANSACTION;
    PRINT N'';
    PRINT N'====================================================================';
    PRINT N'✅ تم إدخال جميع البيانات الواقعية في قاعدة البيانات FSEdu_Dev بنجاح!';
    PRINT N'====================================================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT N'❌ حدث خطأ أثناء تنفيذ السكربت:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;
