# Spec Delta

## Purpose

این قابلیت دسترسی کاربران، حفاظت فایل‌های حساس و ردپای ممیزی عملیات مهم CodeReviewer را برای محیط سازمانی کنترل می‌کند.

## ADDED Requirements

### Requirement: مجوزدهی مبتنی بر نقش
سامانه SHALL نقش‌های Viewer، Reviewer و Admin را اعمال کند؛ Viewer فقط مشاهده، Reviewer اجرای Review و انتشار، و Admin مدیریت تنظیمات و Policy را مجاز است.

#### Scenario: تغییر اتصال توسط Viewer
- **WHEN** کاربر Viewer تلاش می‌کند اتصال Source Control را تغییر دهد
- **THEN** سامانه پاسخ Forbidden می‌دهد و هیچ داده‌ای تغییر نمی‌کند

### Requirement: حذف فایل حساس از Pipeline
سامانه MUST مسیرهای حساس سازمان و مخزن را پیش از Log، Snapshot یا Prompt حذف کند و دلیل حذف را بدون محتوای فایل ثبت کند.

#### Scenario: Diff شامل کلید خصوصی
- **WHEN** مسیر یا محتوای فایل با Policy حساس تطبیق دارد
- **THEN** فایل برای مدل ارسال نمی‌شود و Coverage وضعیت SensitiveExcluded می‌گیرد

### Requirement: Audit تغییرناپذیر عملیات
سامانه SHALL Actor، زمان UTC، عملیات، هدف، نتیجه و CorrelationId عملیات مدیریتی و انتشار را ثبت کند و Secret یا Source Code را در Audit ننویسد.

#### Scenario: انتشار نتیجه Review
- **WHEN** Reviewer انتشار روی MR را آغاز می‌کند
- **THEN** Audit موفق یا ناموفق با RunId و هویت کاربر ذخیره می‌شود

### Requirement: تنظیم امن Production
سامانه MUST در Production گواهی نامعتبر را به‌صورت پیش‌فرض رد و نبود نگاشت نقش معتبر را Deny کند.

#### Scenario: کاربر احرازشده بدون نقش
- **WHEN** کاربر Production احراز شده ولی هیچ نقش مجاز ندارد
- **THEN** سامانه دسترسی کاربردی را رد و رویداد امنیتی ثبت می‌کند
