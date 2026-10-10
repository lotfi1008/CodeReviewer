# Proposal

## Why

GitHub اکنون فقط دریافت PR و Diff را پشتیبانی می‌کند و انتشار یا Trigger خودکار ندارد. تکمیل Provider امکان استفاده یکسان تیم‌های GitHub Enterprise را فراهم می‌کند.

## What Changes

- Webhook امن Pull Request و Push اضافه می‌شود.
- Summary، Inline Review Comment و Check Run منتشر می‌شوند.
- Head SHA، Idempotency و مالکیت کامنت پیش از Write کنترل می‌شوند.
- رفتار در abstraction مشترک Source Control تثبیت می‌شود.

## Capabilities

### New Capabilities

- `github-review-integration`: Trigger و انتشار کامل Review در GitHub/GitHub Enterprise.

### Modified Capabilities

- هیچ قابلیت موجود OpenSpec تغییر نمی‌کند.

## Impact

`GitHubProvider`، Webhook، Publication و تنظیم Connection تغییر می‌کنند. به قراردادهای Webhook و Queue پایدار وابسته است. GitHub App provisioning خودکار خارج از محدوده است.
