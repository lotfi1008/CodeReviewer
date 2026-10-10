# Proposal

## Why

لاگ اجرای Review در SQL موجود است، اما Trace، Metric و Health استاندارد برای تشخیص کندی یا خرابی وابستگی‌ها وجود ندارد. بهره‌برداری سازمانی به مشاهده‌پذیری بدون افشای کد نیاز دارد.

## What Changes

- OpenTelemetry برای HTTP، SQL، LLM، صف و انتشار اضافه می‌شود.
- Correlation ID از Webhook تا Job، ReviewRun و انتشار حفظ می‌شود.
- Healthهای readiness و liveness برای برنامه، SQL، Worker و وابستگی‌های خارجی تعریف می‌شوند.
- Metricها و Alertهای پیشنهادی برای زمان، توکن، Retry و Failure ثبت می‌شوند.

## Capabilities

### New Capabilities

- `observability-health`: Trace، Metric، Correlation و Health امن برای عملیات Review.

### Modified Capabilities

- هیچ قابلیت موجود OpenSpec تغییر نمی‌کند.

## Impact

Startup، کلاینت‌های خارجی، Worker و Pipeline استقرار تغییر می‌کنند. به Webhook و صف پایدار وابسته است. انتخاب Backend مانیتورینگ خاص خارج از محدوده است.
