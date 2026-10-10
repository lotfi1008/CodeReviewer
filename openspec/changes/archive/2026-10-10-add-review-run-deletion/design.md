# Design

## Context

انگیزه در [`proposal.md`](openspec/changes/add-review-run-deletion/proposal.md) آمده است. وضعیت فعلی که طراحی را شکل می‌دهد:

- مخزن دادهٔ گزارش‌ها [`SqlDataStore`](src/CodeReviewer.Web/Services/SqlDataStore.cs:12) است و جدول اصلی `dbo.ReviewRuns` با کلید `Id` (uniqueidentifier) می‌باشد.
- دادهٔ وابستهٔ مستقیم بر اساس `ReviewId`:
  - `dbo.Findings` با `ReviewId` و کلید خارجی به `dbo.ReviewRuns`.
  - `dbo.ReviewEvents` با `ReviewId` و کلید خارجی به `dbo.ReviewRuns` و ایندکس `IX_ReviewEvents_Run`.
- دادهٔ وابستهٔ غیرمستقیم در `dbo.SourceSnapshots` (کلید ترکیبی `ConnectionId` + `Scope`) قرار دارد. Snapshotهای مخصوص یک اجرا با شناسهٔ اجرا در انتهای `Scope` ذخیره می‌شوند:
  - `context:{runId}` (در [`AiReviewService`](src/CodeReviewer.Web/Services/AiReviewService.cs:60))
  - `impact:{runId}` (در [`AiReviewService`](src/CodeReviewer.Web/Services/AiReviewService.cs:62))
  - `coverage:{runId}` (در [`AiReviewService`](src/CodeReviewer.Web/Services/AiReviewService.cs:64))
  - `publication:{runId}` (در [`ReviewPublicationService`](src/CodeReviewer.Web/Services/ReviewPublicationService.cs:20))
  - همه با `ConnectionId` استخراج‌شده از `MergeRequest` همان اجرا ذخیره می‌شوند.
- `ConnectionId` اجرا داخل JSON ستون `MergeRequest` است و در [`History`](src/CodeReviewer.Web/Services/SqlDataStore.cs:197) به `MergeRequestItem` دیسریالایز می‌شود.
- زمینهٔ `project-context:{repo}`، `requests:{repo}`، `diff:{repo}:{number}`، `verified-diff:...` و `repositories` شامل GUID اجرا نیستند؛ بنابراین با شناسهٔ اجرا برخورد نمی‌کنند.
- هیچ طرح‌واره یا ستون جدیدی لازم نیست؛ بنابراین Migration لازم نیست.
- دو نقطهٔ ورود رابط کاربری موجودند: [`History.razor`](src/CodeReviewer.Web/Components/Pages/History.razor:1) و [`ReviewReport.razor`](src/CodeReviewer.Web/Components/Pages/ReviewReport.razor:1). هیچ الگوی تأییدیهٔ تعاملی موجود نیست؛ تنها JS تعاملی در [`ReconnectModal.razor.js`](src/CodeReviewer.Web/Components/Layout/ReconnectModal.razor.js:1) است.
- RBAC هنوز پیاده نشده (تغییر `add-rbac-security-audit` پیشنهاد جداگانه است)؛ بنابراین این تغییر به یک دروازهٔ دسترسی موجود تکیه نمی‌کند.

## Goals / Non-Goals

**Goals:**

- حذف واقعی و اتمی یک گزارش با همهٔ دادهٔ وابسته‌اش، بدون باقی‌گذاشتن رکورد یتیم.
- جلوگیری از تعارض با Worker فعال در زمان حذف اجرای «در حال اجرا».
- تأییدیهٔ صریح در رابط کاربری و به‌روزرسانی درست وضعیت پس از حذف.
- عملیات قابل آزمون و بدون افشای دادهٔ حساس.

**Non-Goals:**

- حذف گروهی، حذف بر اساس فیلتر، یا حذف زمان‌بندی‌شده.
- حذف نرم، آرشیو یا بازیابی.
- حذف دادهٔ مشترک (Profiles، Connections، `ReviewEntries` سراسری).
- تغییر در انتشار اختیاری Merge Request.

## Decisions

### D1 — افزودن متد اختصاصی حذف در `SqlDataStore`

یک متد عمومی جدید مانند `DeleteReview(Guid id)` با یک نوع بازگشتی صریح (مثلاً نتیجهٔ شمارشی شامل `Deleted`، `NotFound`، `Running`) اضافه می‌شود.

- دلیل: همهٔ دسترسی داده در [`SqlDataStore`](src/CodeReviewer.Web/Services/SqlDataStore.cs:12) متمرکز است و منطق اتمی باید همان‌جا بماند تا در UI تکرار نشود.
- گزینه‌های بررسی‌شده:
  - انجام حذف چندمرحله‌ای در کامپوننت UI: رد شد؛ منطق را در UI پخش می‌کند و اتمی نیست.
  - افزودن سرویس جداگانه: رد شد؛ منطق کوچک است و وابستگی اضافی توجیه ندارد.

### D2 — ترتیب حذف درون یک تراکنش واحد

متد حذف یک `SqlConnection` و یک `SqlTransaction` می‌گیرد و به‌ترتیب زیر اجرا می‌کند:

1. خواندن `Status` و `MergeRequest` اجرا با قفل به‌روزرسانی (مثلاً `SELECT Status, MergeRequest FROM dbo.ReviewRuns WITH (UPDLOCK) WHERE Id=@id`).
2. اگر رکورد نبود → `NotFound` بدون انجام حذف.
3. اگر `Status='Running'` بود → `Running`؛ تراکنش بدون تغییر داده commit/rollback می‌شود و حذف انجام نمی‌شود.
4. `DELETE dbo.Findings WHERE ReviewId=@id`.
5. `DELETE dbo.ReviewEvents WHERE ReviewId=@id`.
6. `DELETE dbo.SourceSnapshots WHERE ConnectionId=@connectionId AND Scope LIKE '%:' + CONVERT(nvarchar(36),@id)`.
7. `DELETE dbo.ReviewRuns WHERE Id=@id`.
8. `transaction.Commit()`.

- دلیل ترتیب: ابتدا وابستگی‌ها و در پایان والد؛ این ترتیب با کلیدهای خارجی سازگار است و از خطای FK جلوگیری می‌کند.
- دلیل `UPDLOCK`: بین بررسی وضعیت و حذف، Worker نتواند وضعیت را تغییر دهد و تراکنش را دچار تعارض منطقی کند.
- دلیل الگوی `LIKE '%:' + runId`: همهٔ Snapshotهای اجرا با `:{runId}` تمام می‌شوند و هیچ Scope دیگری GUID ندارد؛ این الگو آینده‌نگر و در برابر افزودن Scopeهای جدید مقاوم است.
- خطای میانهٔ کار → `transaction.Rollback()` (با dispose ضمنی) تا حذف ناقص رخ ندهد.

### D3 — تأییدیهٔ درون‌صفحه‌ای (Inline) به‌جای JS `confirm`

کامپوننت‌ها حالت تأیید دوعاملی/دکمهٔ تأیید را با متغیر محلی نگه می‌دارند: کلیک اول تأییدیه را نشان می‌دهد، کلیک دوم حذف را اجرا می‌کند.

- دلیل: بدون نیاز به JS interop جدید، قابل آزمون و سازگار با رندر Blazor Server؛ هشدار «غیرقابل‌بازگشت» به‌صورت متن درون صفحه نمایش داده می‌شود.
- گزینهٔ بررسی‌شده: `IJSRuntime` و `window.confirm` — رد شد؛ وابستگی JS و نبود تست‌پذیری روشن.

### D4 — به‌روزرسانی رابط کاربری پس از حذف

- در [`History.razor`](src/CodeReviewer.Web/Components/Pages/History.razor:1): پس از حذف موفق، `Load()` بازخوانی می‌شود تا فهرست و شمارش کل به‌روز شود؛ پیام موفقیت نمایش داده می‌شود.
- در [`ReviewReport.razor`](src/CodeReviewer.Web/Components/Pages/ReviewReport.razor:1): پس از حذف موفق، `Navigation.NavigateTo("/history")` فراخوانی می‌شود؛ در صورت `Running` یا خطا، پیام روشن بدون هدایت نمایش داده می‌شود.
- کنترل حذف برای اجرای `Running` غیرفعال یا با پیام توضیحی نمایش داده می‌شود تا کاربر بیهوده تلاش نکند (محافظت اصلی در لایهٔ داده است).

### D5 — مشاهده‌پذیری و امنیت

- نتیجهٔ عملیات با `ILogger`/`Console.Error` به‌صورت خلاصه و بدون محتوای حساس لاگ می‌شود؛ فقط شناسهٔ اجرا و نتیجهٔ شمارشی.
- پیام‌های UI فارسی و بدون افشای محتوای گزارش یا اعتبارنامه‌ها.
- با توجه به نبود RBAC فعلی، دروازهٔ دسترسی همان مجوز دسترسی به رابط مدیریتی است؛ این یک محدودیت پذیرفته‌شده است و در صورت اجرای `add-rbac-security-audit` بعداً قابل تشدید است.

### D6 — Idempotency

فراخوانی حذف روی شناسهٔ ناموجود یا حذف‌شده، بدون استثنا و با نتیجهٔ `NotFound` برمی‌گردد؛ بنابراین تلاش دوم بی‌خطر است.

## Risks / Trade-offs

- [حذف غیرقابل‌بازگشت] → تأییدیهٔ صریح دوعاملی و متن هشدار روشن؛ دادهٔ حذف‌شده در دامنهٔ این تغییر قابل بازیابی نیست (حذف نرم خارج از محدوده است).
- [رقابت با Worker فعال] → `UPDLOCK` درون تراکنش و رد صریح وضعیت `Running`.
- [Snapshotهای آیندهٔ اجرا با الگوی متفاوت] → الگوی `LIKE '%:' + runId` عمداً عمومی انتخاب شده تا هر Scope پایان‌یافته با شناسهٔ اجرا را پوشش دهد؛ در صورت افزودن Scope بدون GUID باید این متد بازبینی شود.
- [نبود RBAC] → محدود به دسترسی رابط مدیریتی؛ تشدید در تغییر RBAC آینده.
- [حذف ناخواستهٔ دادهٔ مشترک] → شرط `ConnectionId` و الگوی اختصاصی از حذف Snapshotهای مشترک (مثل `project-context`) جلوگیری می‌کند.

## Migration Plan

- نیازی به Migration طرح‌واره نیست؛ هیچ جدول یا ستونی اضافه/تغییر نمی‌کند.
- استقرار: انتشار نسخهٔ جدید برنامه؛ قابلیت فقط در کد افزوده می‌شود.
- بازگشت (Rollback): بازگردانی نسخهٔ کد قبلی؛ چون طرح‌واره تغییر نکرده، دادهٔ موجود سازگار می‌ماند. گزارش‌های حذف‌شده بازنمی‌گردند.

## Open Questions

- آیا در آینده باید رویداد Audit سراسری (خارج از `ReviewEvents` حذف‌شده) برای ثبت که «چه کسی چه گزارشی را حذف کرد» نگه داشت؟ تصمیم آن به تغییر `add-rbac-security-audit` موکول می‌شود و در دامنهٔ این تغییر نیست.
