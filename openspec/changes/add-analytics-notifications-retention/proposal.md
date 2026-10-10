# Proposal

## Why

تاریخچه اجرا قابل مشاهده است اما روند کیفیت، هزینه، اعلان نتیجه و چرخه عمر داده مدیریت نمی‌شود. مدیر پایلوت به گزارش قابل سنجش و سیاست نگهداری نیاز دارد.

## What Changes

- Dashboard کیفیت، زمان، توکن، Failure و False Positive اضافه می‌شود.
- اعلان Email، Slack و Teams با Rule و Deduplication ارسال می‌شود.
- Export JSON/CSV/PDF و Jobهای Retention/Purge تعریف می‌شوند.
- حذف داده Audit‌شده و قابل پیش‌نمایش خواهد بود.

## Capabilities

### New Capabilities

- `review-analytics-lifecycle`: تحلیل روند، اعلان، Export و چرخه عمر داده Review.

### Modified Capabilities

- هیچ قابلیت موجود OpenSpec تغییر نمی‌کند.

## Impact

SQL، UI، Scheduler و Providerهای اعلان تغییر می‌کنند. به Observability و Feedback وابسته است. BI عمومی و نگهداری نامحدود خارج از محدوده است.
