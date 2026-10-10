# Spec Delta

## Purpose

این قابلیت Policy نسخه‌پذیر مخزن را به‌صورت امن بارگذاری و با سیاست سازمان و Profile اجرا ترکیب می‌کند.

## ADDED Requirements

### Requirement: قرارداد نسخه‌دار Policy
سامانه SHALL فایل `.ai-review/config.yml` را با Version شناخته‌شده، Schema محدود و پیام خطای قابل اقدام اعتبارسنجی کند.

#### Scenario: Version پشتیبانی‌نشده
- **WHEN** فایل Policy دارای Version ناشناخته است
- **THEN** Review پیش از فراخوانی مدل متوقف و خطای Validation بدون محتوای حساس ثبت می‌شود

### Requirement: تقدم Policy
سامانه MUST محدودیت‌های سازمان را غیرقابل تضعیف بداند، سپس Policy مخزن و در نهایت Profile اجرا را برای گزینه‌های مجاز اعمال کند.

#### Scenario: مخزن فایل حساس سازمان را Include می‌کند
- **WHEN** Policy مخزن مسیری را مجاز و Policy سازمان همان مسیر را حساس اعلام کرده است
- **THEN** مسیر همچنان حذف می‌شود و دلیل تقدم سازمان ثبت می‌گردد

### Requirement: Snapshot و نمایش Policy مؤثر
سامانه SHALL Policy مؤثر و Hash فایل مبنا را در هر ReviewRun Snapshot و در گزارش قابل مشاهده کند.

#### Scenario: تغییر Policy پس از Review
- **WHEN** Policy مخزن بعد از تکمیل Review تغییر می‌کند
- **THEN** گزارش تاریخی همچنان Policy مؤثر زمان اجرا را نشان می‌دهد

### Requirement: Policy بدون اجرای کد
سامانه MUST فقط فیلدها و فرمان‌های Allowlist‌شده را بپذیرد و هیچ Shell، Template یا expression دلخواهی از Policy اجرا نکند.

#### Scenario: فیلد ناشناخته اجرایی
- **WHEN** Policy شامل فرمان یا فیلد اجرایی مجازنشده است
- **THEN** Validation ناموفق می‌شود و فرمان اجرا نمی‌شود
