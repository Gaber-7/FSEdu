# 🎥 استراتيجية استضافة الفيديو

## الوضع الحالي (MVP / Development)

- **YouTube (Unlisted/Public)** — للاختبار والعرض فقط
- الرابط يُخزّن في `Lesson.VideoUrl` كنص مباشر
- مشغّل `<iframe>` للفيديوهات من YouTube
- CSS Watermark ديناميكي باسم الطالب ورقم الهاتف

### ⚠️ قيود الوضع الحالي
- لا حماية حقيقية للمحتوى (يمكن تحميله)
- قد تظهر إعلانات YouTube
- براندينج YouTube ظاهر
- لا يمكن بناء تحليلات مشاهدة دقيقة

━━━━━━━━━━━━━━━━━━━

## مسار الترقية (متى ولماذا)

### ⏱️ Phase 1 — MVP (الآن)
**YouTube** — للتطوير والعرض الأولي
- مجاني، سريع، يكفي للعرض التجريبي

### 🎯 Phase 2 — بداية الإنتاج (100-500 طالب)
**الانتقال إلى: Bunny Stream** ([bunny.net/stream](https://bunny.net/stream))

**لماذا Bunny:**
- الأرخص في السوق: ~$1 لكل 1000 دقيقة مشاهدة
- HLS مشفّر + Signed URLs
- لا إعلانات، لا براندينج
- واجهة API بسيطة

**التكامل المطلوب:**
```
1. إضافة حزمة: BunnyStream.NET (أو REST API مباشر)
2. Upload Endpoint في المدرس:
   - المدرس يرفع → Backend يرسل للـ Bunny
   - نحفظ videoId + duration
3. Playback:
   - GET /lessons/{id}/play → نطلب Signed URL من Bunny
   - URL صالح 2 ساعة، مرتبط بـ IP
4. Player:
   - HLS.js لدعم كل المتصفحات
   - Watermark CSS فوق الفيديو
```

### 🛡️ Phase 3 — النمو (500+ طالب)
**الانتقال إلى: VdoCipher** ([vdocipher.com](https://vdocipher.com))

**لماذا VdoCipher:**
- مخصّص لمنصات EdTech
- Widevine L1 DRM حقيقي
- Watermark ديناميكي داخل الفيديو نفسه
- منع التحميل على مستوى OS (Android/iOS)
- أدوات تحليل مشاهدة متقدمة

**التكلفة:** $0.90 لكل GB مشاهد (أعلى لكن DRM قوي جدًا)

━━━━━━━━━━━━━━━━━━━

## 🔧 التغييرات المطلوبة عند الانتقال

### في Domain
لا تغييرات — `Lesson.VideoUrl` + `Lesson.VideoDrmKeyId` جاهزة.

### في Application
إضافة:
```csharp
public interface IVideoService
{
    Task<string> GetUploadUrlAsync(Guid lessonId);
    Task<string> GetSignedPlaybackUrlAsync(Guid lessonId, Guid userId, TimeSpan validFor);
    Task<VideoMetadata> GetMetadataAsync(string videoId);
}
```

### في Infrastructure
بديل محدد:
- `BunnyStreamVideoService : IVideoService`
- `VdoCipherVideoService : IVideoService`
- `YouTubeVideoService : IVideoService` (الحالي)

### في WatchLessonHandler
- استدعاء `IVideoService.GetSignedPlaybackUrlAsync(...)` بدل إرجاع URL مباشر

### في LessonWatch.razor
- استخدام HLS.js بدل iframe
- التحقق من انتهاء صلاحية الـ URL وإعادة الطلب

━━━━━━━━━━━━━━━━━━━

## 📊 معايير الانتقال (متى ننتقل؟)

ننتقل من YouTube عند **أي** من:

| المؤشر | العتبة |
|--------|--------|
| عدد الطلاب النشطين | 50+ طالب دافع |
| الإيرادات الشهرية | 5,000 ج.م+ |
| طلب حماية المحتوى | أي شكوى من مدرس أو تسريب |
| تجربة التحميل من YouTube | تمت من أحد المستخدمين |
| الإطلاق الرسمي | قبل أي تسويق |

━━━━━━━━━━━━━━━━━━━

## 💰 تقدير التكلفة الشهرية

### سيناريو: 500 طالب، 15 درس/شهر، متوسط الدرس 25 دقيقة

| الخدمة | التكلفة الشهرية |
|-------|----------------|
| YouTube | **0 ج.م** (لكن بدون حماية) |
| Bunny Stream | ~$187 (500 × 15 × 25 × $1/1000) ≈ **9,000 ج.م** |
| Cloudflare Stream | ~$937 ≈ **45,000 ج.م** |
| VdoCipher | ~$300-500 ≈ **15,000-25,000 ج.م** |
| Self-hosted (AWS) | ~$1,500+ ≈ **75,000 ج.م+** |

**الاستنتاج:** Bunny Stream هو الأمثل سعرًا/ميزة للبداية.
