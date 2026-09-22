# 🗄️ FSEdu Database

هذا المجلد يحتوي على ملفات قاعدة البيانات الفعلية للتطوير.

━━━━━━━━━━━━━━━━━━━

## 📂 الملفات

| الملف | الحجم | الوصف |
|-------|------|-------|
| `FSEdu_Dev.mdf` | 10 MB+ | ملف البيانات الرئيسي |
| `FSEdu_Dev_log.ldf` | 5 MB+ | Transaction Log |

> ⚠️ هذه الملفات **مستبعدة من Git** عبر `.gitignore` (ثقيلة وخاصة بكل مطوّر).

━━━━━━━━━━━━━━━━━━━

## 🔌 معلومات الاتصال

```
Server:   .\MSSQLSERVER2   (SQL Server 2022 instance)
Database: FSEdu_Dev
Auth:     Windows Authentication (Trusted Connection)
```

في [appsettings.Development.json](../src/Presentation/FSEdu.Api/appsettings.Development.json):
```json
"ConnectionStrings": {
  "Default": "Server=.\\MSSQLSERVER2;Database=FSEdu_Dev;..."
}
```

━━━━━━━━━━━━━━━━━━━

## 🆕 كيف يُنشَأ الـ DB على جهاز مطوّر جديد

### المتطلبات
- SQL Server 2022 مثبّت
- instance باسم `MSSQLSERVER2` (أو عدّل الـ connection string)

### الخطوات

**1. أنشئ القاعدة يدويًا في SQL Server 2022:**
```sql
CREATE DATABASE [FSEdu_Dev]
ON PRIMARY (
    NAME = N'FSEdu_Dev',
    FILENAME = N'D:\My Work\FullScreen Solutions\FSEdu\DataBase\FSEdu_Dev.mdf',
    SIZE = 10MB, FILEGROWTH = 10MB
)
LOG ON (
    NAME = N'FSEdu_Dev_log',
    FILENAME = N'D:\My Work\FullScreen Solutions\FSEdu\DataBase\FSEdu_Dev_log.ldf',
    SIZE = 5MB, FILEGROWTH = 10%
);
```

أو من PowerShell:
```powershell
sqlcmd -S ".\MSSQLSERVER2" -Q "CREATE DATABASE [FSEdu_Dev] ON PRIMARY (NAME = N'FSEdu_Dev', FILENAME = N'D:\My Work\FullScreen Solutions\FSEdu\DataBase\FSEdu_Dev.mdf', SIZE = 10MB, FILEGROWTH = 10MB) LOG ON (NAME = N'FSEdu_Dev_log', FILENAME = N'D:\My Work\FullScreen Solutions\FSEdu\DataBase\FSEdu_Dev_log.ldf', SIZE = 5MB, FILEGROWTH = 10%);"
```

**2. طبّق الـ Migrations:**
```powershell
cd "D:\My Work\FullScreen Solutions\FSEdu"

dotnet ef database update `
  --context ApplicationDbContext `
  --project src/Infrastructure/FSEdu.Persistence `
  --startup-project src/Presentation/FSEdu.Api

dotnet ef database update `
  --context IdentityDbContext `
  --project src/Infrastructure/FSEdu.Identity `
  --startup-project src/Presentation/FSEdu.Api
```

**3. شغّل الـ API من Visual Studio (F5)** — الـ Seed يعمل تلقائيًا.

━━━━━━━━━━━━━━━━━━━

## 🔄 إعادة إنشاء القاعدة من الصفر

```powershell
# 1. أوقف VS Debug (Shift+F5)
# 2. احذف القاعدة
cd "D:\My Work\FullScreen Solutions\FSEdu"
dotnet ef database drop --force `
  --context ApplicationDbContext `
  --project src/Infrastructure/FSEdu.Persistence `
  --startup-project src/Presentation/FSEdu.Api

# 3. أعد تنفيذ خطوات "كيف يُنشَأ الـ DB" أعلاه
```

━━━━━━━━━━━━━━━━━━━

## 🧰 أدوات الوصول

### Visual Studio
`View → SQL Server Object Explorer → .\MSSQLSERVER2 → FSEdu_Dev`

### SQL Server Management Studio (SSMS)
اتصل بـ: `.\MSSQLSERVER2` بـ Windows Authentication

### Azure Data Studio
نفس الـ Server name

━━━━━━━━━━━━━━━━━━━

## 🎯 Schemas داخل القاعدة

| Schema | الجداول | الوصف |
|--------|--------|--------|
| `edu` | ~35 | منطق الأعمال (مستخدمون، دورات، اشتراكات، ...) |
| `auth` | 10 | المصادقة (ASP.NET Identity + OTP + Refresh Tokens) |

━━━━━━━━━━━━━━━━━━━

## 🌐 للإنتاج

غيّر الـ Connection String في `appsettings.Production.json`:

```json
"ConnectionStrings": {
  "Default": "Server=your-prod-server;Database=FSEdu_Prod;User Id=...;Password=..."
}
```
