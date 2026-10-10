# Spec Delta

## Purpose

این قابلیت رویدادهای معتبر GitLab را به Review خودکار، قابل ردگیری و بدون اجرای تکراری برای یک MR و SHA تبدیل می‌کند.

## ADDED Requirements

### Requirement: پذیرش امن Webhook
سامانه SHALL فقط رویداد GitLab با Secret معتبر، اتصال فعال و Payload قابل نگاشت به مخزن ثبت‌شده را بپذیرد و پاسخ HTTP نباید Secret یا Payload حساس را بازتاب دهد.

#### Scenario: Secret نامعتبر
- **WHEN** درخواست Webhook فاقد Secret معتبر است
- **THEN** سامانه پاسخ 401 می‌دهد و هیچ Job یا Snapshot ایجاد نمی‌کند

### Requirement: Enqueue بر اساس Policy و SHA
سامانه SHALL رویداد ایجاد، بازگشایی یا Update یک MR باز را فقط در صورت فعال‌بودن Automation مخزن و جدیدبودن Head SHA به صف بفرستد.

#### Scenario: Push جدید روی MR باز
- **WHEN** رویداد معتبر برای SHA جدید و RepositoryAutomationSettings فعال دریافت می‌شود
- **THEN** دقیقاً یک ReviewJob با مدل، Profile و سیاست انتشار Snapshot‌شده ایجاد می‌شود

### Requirement: Idempotency و انتشار اختیاری
سامانه MUST شناسه Delivery و کلید connection/repository/MR/SHA/profile را Deduplicate کند و مقدار PublishToMergeRequest را فقط از Policy صریح مخزن بگیرد.

#### Scenario: تحویل تکراری
- **WHEN** یک Delivery یا همان کلید Review دوباره دریافت می‌شود
- **THEN** سامانه نتیجه پذیرش قبلی را برمی‌گرداند و هیچ Review یا Write جدید روی MR ایجاد نمی‌کند

### Requirement: ثبت نتیجه پردازش
سامانه SHALL وضعیت Received، Ignored، Enqueued یا Rejected هر رویداد را بدون Source Code و Secret نگهداری کند.

#### Scenario: MR بسته
- **WHEN** رویداد معتبر متعلق به MR بسته دریافت می‌شود
- **THEN** رویداد با دلیل Ignored ثبت و Review اجرا نمی‌شود
