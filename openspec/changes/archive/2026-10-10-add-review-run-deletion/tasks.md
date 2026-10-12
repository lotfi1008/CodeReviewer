# Tasks

## 1. لایهٔ داده (SqlDataStore)

- [x] 1.1 افزودن یک نوع بازگشتی صریح برای نتیجهٔ حذف (مثلاً `ReviewDeleteResult` با مقادیر `Deleted`، `NotFound`، `Running`) در [`SqlDataStore`](src/CodeReviewer.Web/Services/SqlDataStore.cs:12) و تأیید ساخت پروژه با `dotnet build src/CodeReviewer.Web/CodeReviewer.Web.csproj`
- [x] 1.2 پیاده‌سازی متد `DeleteReview(Guid id)` با یک `SqlConnection`/`SqlTransaction` واحد که ابتدا وضعیت و `MergeRequest` را با `UPDLOCK` می‌خواند و در صورت نبود رکورد `NotFound` و در صورت `Status='Running'` نتیجهٔ `Running` برمی‌گرداند، بدون تغییر داده
- [x] 1.3 تکمیل ترتیب حذف در همان تراکنش: `dbo.Findings` → `dbo.ReviewEvents` → `dbo.SourceSnapshots` (با شرط `ConnectionId` و الگوی `'%:' + CONVERT(nvarchar(36),@id)`) → `dbo.ReviewRuns`، سپس `Commit`؛ و `Rollback` در صورت خطا؛ تأیید با بازبینی کد که هیچ رکورد یتیم باقی نمی‌ماند

## 2. رابط کاربری — صفحهٔ سوابق

- [x] 2.1 افزودن کنترل «حذف» در ردیف هر اجرا در [`History.razor`](src/CodeReviewer.Web/Components/Pages/History.razor:1) با حالت تأییدیهٔ درون‌صفحه‌ای دوعاملی طبق تصمیم D3 در [`design.md`](openspec/changes/add-review-run-deletion/design.md:1)
- [x] 2.2 اتصال دکمهٔ تأیید به `Database.DeleteReview`، سپس `Load()` برای بازخوانی فهرست و شمارش کل، و نمایش پیام موفقیت یا خطای فارسی
- [x] 2.3 غیرفعال‌کردن یا نمایش پیام توضیحی برای کنترل حذف وقتی `run.Status == "Running"`

## 3. رابط کاربری — صفحهٔ گزارش

- [x] 3.1 افزودن کنترل «حذف» با تأییدیهٔ درون‌صفحه‌ای در [`ReviewReport.razor`](src/CodeReviewer.Web/Components/Pages/ReviewReport.razor:1)
- [x] 3.2 پس از حذف موفق فراخوانی `Navigation.NavigateTo("/history")`؛ در صورت `Running` یا خطا عدم هدایت و نمایش پیام روشن

## 4. استایل (در صورت نیاز)

- [x] 4.1 افزودن استایل دکمهٔ خطرناک/حالت تأییدیه در [`admin.css`](src/CodeReviewer.Web/wwwroot/admin.css:1) (کلاس `.button-danger`) و تأیید سازگاری ظاهری RTL به‌صورت بصری

## 5. آزمون‌ها

- [x] 5.1 افزودن آزمون واحد/یکپارچه برای `DeleteReview` که حذف موفق یک گزارش، پاک‌شدن یافته‌ها/رویدادها/Snapshotها و `NotFound` برای شناسهٔ ناموجود را پوشش دهد؛ تأیید با `dotnet test` روی پروژهٔ `CodeReviewer.Web.Tests`
- [x] 5.2 افزودن آزمون که رد شدن حذف اجرای با وضعیت `Running` و دست‌نخورده‌ماندن دادهٔ اجراهای دیگر را تأیید کند؛ تأیید با `dotnet test`
- [x] 5.3 اطمینان از عدم افشای محتوای حساس در پیام‌های لاگ/UI حذف با بازبینی پیام‌ها و دلیل Skip (هیچ رمز/رشتهٔ اتصال در پیام‌ها نیست)

## 6. اعتبارسنجی نهایی

- [x] 6.1 اجرای `dotnet build CodeReviewer.slnx` (موفق، ۰ هشدار/۰ خطا) و `dotnet test CodeReviewer.slnx`؛ آزمون‌های جدید در نبود SQL Server به‌صورت پویا Skip می‌شوند و ۱۲ خطای باقی‌مانده مربوط به آزمون‌های پیشین `ConnectionCatalogTests` و وابسته به `localhost` است (نامرتبط با این تغییر)
- [x] 6.2 اجرای `openspec validate add-review-run-deletion --strict` و تأیید معتبربودن همهٔ دلتاها و سناریوها
- [ ] 6.3 تأیید دستی سناریوهای صفحهٔ سوابق و صفحهٔ گزارش (تأیید، انصراف، حذف، بازخوانی/هدایت) در اجرای محلی — به محیط دارای SQL Server سپرده شد (اجرای محلی در این محیط ممکن نبود)
