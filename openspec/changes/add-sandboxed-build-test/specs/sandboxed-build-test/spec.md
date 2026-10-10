# Spec Delta

## Purpose

این قابلیت Build و Test اختیاری را در محیط محدود اجرا و نتیجه واقعی آن را جدا از قضاوت AI گزارش می‌کند.

## ADDED Requirements

### Requirement: اجرای صریح و ایزوله
سامانه SHALL Build/Test را فقط با Policy فعال و Command allowlist در Runner جدا با محدودیت زمان، CPU، حافظه و دیسک اجرا کند.

#### Scenario: Build غیرفعال
- **WHEN** Policy اجرای Build/Test را فعال نکرده است
- **THEN** هیچ Process ایجاد نمی‌شود و گزارش وضعیت NotRequested نشان می‌دهد

### Requirement: کنترل Network و Secret
Runner MUST Network را پیش‌فرض مسدود و فقط Secretهای صریح، کوتاه‌عمر و Redact‌شده لازم برای Restore مجاز را تزریق کند.

#### Scenario: دسترسی Network غیرمجاز
- **WHEN** Process تلاش می‌کند به مقصدی خارج از Allowlist وصل شود
- **THEN** اتصال رد و رویداد امنیتی بدون Secret ثبت می‌شود

### Requirement: نتیجه تفکیک‌شده
سامانه SHALL Passed، Failed، TimedOut و InfrastructureError را جداگانه ذخیره و خروجی را با سقف اندازه و Redaction نمایش دهد.

#### Scenario: شکست تست
- **WHEN** فرمان تست با Exit Code غیرصفر و Runner سالم پایان می‌یابد
- **THEN** نتیجه Failed ثبت می‌شود و به‌عنوان خطای زیرساخت یا Finding AI معرفی نمی‌شود

### Requirement: لغو و پاک‌سازی
سامانه MUST با لغو Review، Process tree را متوقف و Workspace موقت را با ثبت نتیجه پاک‌سازی کند.

#### Scenario: لغو حین تست
- **WHEN** کاربر Review را هنگام اجرای تست لغو می‌کند
- **THEN** Processها خاتمه می‌یابند و نتیجه Cancelled بدون انتشار خودکار ثبت می‌شود
