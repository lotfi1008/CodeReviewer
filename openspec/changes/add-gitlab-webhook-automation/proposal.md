# Proposal

## Why

بازبینی اکنون فقط از UI آغاز می‌شود و Push جدید به MR به‌صورت خودکار شناسایی نمی‌شود. برای پایلوت سازمانی باید رویداد GitLab با کنترل امنیتی و بدون ایجاد Review تکراری وارد همان صف پایدار شود.

## What Changes

- endpoint امن برای رویدادهای ایجاد، بازگشایی و Update مرج‌ریکوست اضافه می‌شود.
- Secret، شناسه رویداد، وضعیت MR و Head SHA پیش از Enqueue اعتبارسنجی می‌شوند.
- تنظیم `RepositoryAutomationSettings` مدل، پروفایل و سیاست انتشار هر مخزن را تعیین می‌کند.
- دریافت دوباره رویداد یا Push با SHA یکسان idempotent خواهد بود.

## Capabilities

### New Capabilities

- `gitlab-webhook-automation`: دریافت امن رویداد GitLab و ایجاد Review خودکار با سیاست مخزن.

### Modified Capabilities

- هیچ قابلیت موجود OpenSpec تغییر نمی‌کند.

## Impact

`Program.cs`، Source Control، صف Review و SQL تغییر می‌کنند. وابستگی این Change صف چند-Worker است. Auto-approve، Auto-merge و فعال‌کردن پیش‌فرض انتشار خارج از محدوده‌اند.
