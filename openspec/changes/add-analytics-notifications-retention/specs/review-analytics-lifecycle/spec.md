# Spec Delta

## Purpose

این قابلیت داده Review را به معیارهای قابل فهم، اعلان کنترل‌شده، Export امن و چرخه عمر قابل ممیزی تبدیل می‌کند.

## ADDED Requirements

### Requirement: Analytics قابل فیلتر
سامانه SHALL معیارهای تعداد، وضعیت، Duration، Token، یافته و Feedback را بر اساس بازه زمانی، مخزن، مدل و Profile بدون نمایش Source Code تجمیع کند.

#### Scenario: داشبورد مخزن
- **WHEN** مدیر یک مخزن و بازه زمانی را انتخاب می‌کند
- **THEN** معیارها فقط از Reviewهای مجاز و نهایی همان محدوده محاسبه می‌شوند

### Requirement: اعلان idempotent
سامانه SHALL اعلان را فقط برای Rule فعال و رویداد معنی‌دار ارسال و Provider/event/destination را Deduplicate کند.

#### Scenario: Retry ارسال Slack
- **WHEN** پاسخ ارسال نامشخص است و Job Retry می‌شود
- **THEN** Idempotency key ثابت مانع اعلان تکراری می‌شود یا وضعیت نامشخص را برای بررسی ثبت می‌کند

### Requirement: Export امن
سامانه SHALL Export JSON، CSV و PDF را با مجوز، Cancellation و سقف داده تولید و Secret، Prompt و Diff را پیش‌فرض حذف کند.

#### Scenario: لغو Export بزرگ
- **WHEN** کاربر Export در حال اجرا را لغو می‌کند
- **THEN** تولید متوقف و فایل ناقص قابل دانلود باقی نمی‌ماند

### Requirement: Retention قابل پیش‌نمایش
سامانه MUST پیش از Purge تعداد رکوردها را Dry-run نمایش و حذف را Audit کند؛ داده تحت Legal Hold حذف نمی‌شود.

#### Scenario: اجرای Retention
- **WHEN** Admin حذف داده منقضی را تأیید می‌کند
- **THEN** فقط رکوردهای واجد شرایط و وابستگی‌های تعریف‌شده حذف و نتیجه Audit می‌شود
