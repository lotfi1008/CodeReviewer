# Spec Delta

## Purpose

این قابلیت Trigger، Publication و Checkهای Review را برای GitHub و GitHub Enterprise با رفتار امن و idempotent فراهم می‌کند.

## ADDED Requirements

### Requirement: Webhook امضاشده GitHub
سامانه SHALL امضای HMAC و Delivery ID رویداد Pull Request را اعتبارسنجی و فقط Action و Repository پشتیبانی‌شده را پردازش کند.

#### Scenario: امضای نامعتبر
- **WHEN** Webhook GitHub با Signature نامعتبر دریافت می‌شود
- **THEN** پاسخ 401 داده و هیچ Job ایجاد نمی‌شود

### Requirement: انتشار کنترل‌شده GitHub
سامانه SHALL فقط با PublishToMergeRequest فعال، Summary، Inline Comment و Check Run را برای Head SHA بررسی‌شده ایجاد یا همگام کند.

#### Scenario: انتشار غیرفعال
- **WHEN** Review با انتشار غیرفعال تکمیل می‌شود
- **THEN** هیچ API نوشتنی GitHub فراخوانی نمی‌شود

### Requirement: Idempotency و Head Guard
سامانه MUST Comment و Check متعلق به خود را با Marker پایدار بازیابی و پیش از هر Write بازبودن PR و برابری Head SHA را کنترل کند.

#### Scenario: Push پس از Review
- **WHEN** Head SHA پیش از انتشار تغییر کرده است
- **THEN** انتشار بدون Write متوقف و نیاز به Review جدید ثبت می‌شود

### Requirement: Permission قابل تشخیص
سامانه SHALL کمبود مجوز Comment یا Check را با Scope لازم گزارش کند و نتیجه Review ذخیره‌شده را از بین نبرد.

#### Scenario: Token خواندنی
- **WHEN** GitHub نوشتن Comment را Forbidden می‌کند
- **THEN** Publication Failed ثبت و Review برای Retry پس از اصلاح مجوز باقی می‌ماند
