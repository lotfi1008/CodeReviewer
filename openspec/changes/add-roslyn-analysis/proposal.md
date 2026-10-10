# Proposal

## Why

تحلیل فعلی عمدتاً از Diff و Context دستی استفاده می‌کند و روابط معنایی C# را اثبات نمی‌کند. Roslyn می‌تواند Context دقیق Symbol، Type، Nullable و async را برای کاهش حدس مدل فراهم کند.

## What Changes

- تحلیلگر مستقل Roslyn برای Solution و فایل‌های C# مرتبط اضافه می‌شود.
- Symbol، Invocation، Reference، Nullable و async به خروجی ساختاریافته تبدیل می‌شوند.
- Ruleهای قطعی و Context محدودشده به موتور Review افزوده می‌شوند.
- یافته‌های Roslyn و AI با شناسه مشترک Deduplicate می‌شوند.

## Capabilities

### New Capabilities

- `roslyn-analysis`: تحلیل معنایی کنترل‌شده کد .NET و ادغام نتیجه با Review.

### Modified Capabilities

- هیچ قابلیت موجود OpenSpec تغییر نمی‌کند.

## Impact

Review Engine، Context، یافته‌ها و وابستگی‌های NuGet تغییر می‌کنند. به Repository Policy وابسته است. Index کامل مخزن و اجرای Build خارج از این Change است.
