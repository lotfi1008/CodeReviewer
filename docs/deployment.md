# استقرار عملیاتی روی IIS

## ساخت بسته

از ریشهٔ پروژه اجرا کنید: `powershell -File tools/Publish-Iis.ps1`.
بستهٔ ZIP و SHA256 در `artifacts` ساخته می‌شود. تنظیمات Development، توکن‌های ذخیره‌شده و کلیدهای رمزگشایی داخل بسته نیستند.

## پیش‌نیاز و تنظیمات سرور

1. Windows Server با IIS، Windows Authentication، WebSocket و .NET 10 Hosting Bundle نصب شود. پس از نصب Hosting Bundle سرویس IIS راه‌اندازی مجدد شود.
2. یک Application Pool اختصاصی، ۶۴ بیتی، با No Managed Code و هویت سرویس ثابت بسازید. Windows Authentication را فعال و Anonymous Authentication را غیرفعال کنید. این نسخه در Production ورود ویندوز را الزامی می‌کند؛ دسترسی سایت را به گروه مجاز سازمان محدود کنید.
3. این نسخه Worker را در همان پردازش وب اجرا می‌کند؛ **فقط یک پردازش و یک نمونه** از برنامه به دیتابیس متصل باشد. `maximumWorkerProcesses=1`، `disallowOverlappingRotation=true`، `startMode=AlwaysRunning`، `idleTimeout=00:00:00`، Application Initialization و `preloadEnabled=true` تنظیم شوند. هنگام ارتقا ابتدا Pool قدیمی را متوقف کنید. صف SQL بعد از راه‌اندازی مجدد اجراهای ناتمام را از ابتدا انجام می‌دهد؛ انتشار کامنت کنترل تکرار دارد.
4. پوشهٔ پایدار خارج از مسیر انتشار مانند `D:\CodeReviewerData` بسازید؛ فقط هویت Pool و مدیران مجوز خواندن/نوشتن داشته باشند. متغیر محیطی `CODEREVIEWER_DATA_DIR` را به آن تنظیم کنید و Load User Profile را برای Pool فعال کنید.
5. تنظیمات زیر را با IIS/environment مخصوص همین Pool اعمال کنید. مقادیر واقعی را در سورس یا ZIP قرار ندهید:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__CodeReviewer=Server=SQL_HOST;Database=CodeReviewer;Integrated Security=True;Encrypt=True;TrustServerCertificate=True
CODEREVIEWER_DATA_DIR=D:\CodeReviewerData
AllowedHosts=review.example.internal
```

برنامه schema را در شروع ایجاد/ارتقا می‌دهد؛ دیتابیس باید قبلاً ایجاد شده باشد و هویت سرویس در زمان شروع مجوز DDL لازم داشته باشد. برای SQL روی سرور جدا، حساب دامنهٔ سرویس با دسترسی مشخص استفاده کنید. پشتیبان SQL و کلیدهای Data Protection را طبق سیاست سازمان نگهداری کنید.

6. بسته را باز کرده و سایت IIS را به آن متصل کنید؛ `web.config` تولیدشده را نگه دارید. HTTPS binding با گواهی سازمان و نام میزبان مناسب تنظیم کنید. مطابق تنظیم درخواستی فعلی، `ServiceConnections:AllowUntrustedCertificates=true` برای اتصال خروجی به سرویس‌های داخلی برقرار است.
7. توکن‌های SQL با Data Protection/DPAPI به هویت ویندوز متصل‌اند. انتقال دیتابیس توسعه به هویت جدید به‌تنهایی کافی نیست؛ کلیدهای اتصال را در پنل روی سرور مقصد دوباره ثبت کنید یا انتقال رمزنگاری را از قبل برنامه‌ریزی کنید. پوشهٔ keys نباید در وب‌روت باشد.

## آزمون پذیرش روی مقصد

- ورود کاربر مجاز و رد کاربر خارج از گروه سازمان را بررسی کنید.
- در تنظیمات، اتصال GitLab و مدل واقعی را آزمایش کنید. پروفایل دلخواه با حوزه‌ها و قواعد واقعی بسازید.
- یک MR واقعی انتخاب و Review را آغاز کنید؛ صفحه را ببندید و از سوابق دوباره لاگ را باز کنید.
- شمار فایل‌ها، پوشش هر بخش، یافته‌ها و تحلیل را با Diff مقایسه کنید. خطای مدل باید دلیل و وضعیت ناقص داشته باشد؛ بررسی ناقص نباید به‌عنوان کامل منتشر شود.
- انتشار Summary و Inline را در GitLab بررسی کنید. انتشار مجدد همان گزارش نباید نظر تکراری ایجاد کند. با تغییر SHA انتشار گزارش قبلی باید رد شود.
- توقف و شروع Pool را در اجرای مجاز آزمایشی بررسی کنید؛ کار ناتمام از صف SQL بازیابی می‌شود. بازبینی از ابتدا هزینهٔ مدل مجدد دارد.
- برای پذیرش پایلوت، حداقل ۲۰ MR واقعی با صفر کامنت تکراری و اعتبار انسانی دست‌کم ۸۰٪ یافته‌ها لازم است؛ اجرای یک MR جای این معیار را نمی‌گیرد.

## وضعیت و حدود فعلی

مرور دستی و آغاز Review از پنل، صف SQL، پروفایل چندحوزه‌ای، لاگ و انتشار GitLab موجود است. اتصال GitHub دریافت PR/Diff را پشتیبانی می‌کند؛ انتشار GitHub و شروع خودکار با Webhook هنوز پیاده نشده‌اند. Worker سرویس مستقل ویندوز نیست. Build/Test مخزن و Roslyn اجرا نمی‌شوند. فایل باینری یا Diff حذف‌شده توسط سرویس به‌عنوان پوشش ناقص مشخص می‌شود. محدودیت آزمایشی تعداد فایل یا پسوند C# وجود ندارد؛ زمان، توکن و اندازهٔ هر بخش برای پایداری قابل تنظیم‌اند.

## منابع استقرار

- [Microsoft: ASP.NET Core on IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0)
- [Microsoft: IIS advanced configuration](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/advanced?view=aspnetcore-10.0)
