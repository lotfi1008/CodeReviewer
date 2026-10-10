# Proposal

## Why

گزارش فعلی صریحاً Build/Test را اجراشده تلقی نمی‌کند. برای کشف خطای واقعی لازم است این عملیات اختیاری، محدود و جدا از Process وب انجام شود.

## What Changes

- Runner مستقل با Command allowlist برای restore/build/test اضافه می‌شود.
- Timeout، CPU/RAM، فضای دیسک و سیاست Network اعمال می‌شوند.
- خروجی خلاصه و Redact‌شده جدا از یافته AI ذخیره می‌شود.
- شکست زیرساخت از شکست Build/Test تفکیک می‌شود.

## Capabilities

### New Capabilities

- `sandboxed-build-test`: اجرای ایزوله و قابل کنترل Build/Test برای Review.

### Modified Capabilities

- هیچ قابلیت موجود OpenSpec تغییر نمی‌کند.

## Impact

Worker، Policy مخزن، Storage و گزارش تغییر می‌کنند. به صف پایدار و امنیت وابسته است. اجرای Script دلخواه و دسترسی Network پیش‌فرض خارج از محدوده است.
