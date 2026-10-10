# Proposal

## Why

قواعد Review اکنون در Profile و Context دیتابیس نگهداری می‌شوند و همراه کد نسخه‌بندی نمی‌شوند. تیم‌ها باید بتوانند قواعد مخزن را قابل Review، قابل اعتبارسنجی و قابل Snapshot تعریف کنند.

## What Changes

- قرارداد نسخه‌دار `.ai-review/config.yml` اضافه می‌شود.
- Include/Exclude، مسیر حساس، قواعد معماری و معیار پذیرش پشتیبانی می‌شوند.
- تقدم Policy سازمان، Policy مخزن و Profile اجرا تعریف می‌شود.
- Policy مؤثر هر اجرا Snapshot و در گزارش قابل مشاهده می‌شود.

## Capabilities

### New Capabilities

- `repository-review-policy`: بارگذاری، اعتبارسنجی و اعمال قواعد نسخه‌پذیر مخزن.

### Modified Capabilities

- هیچ قابلیت موجود OpenSpec تغییر نمی‌کند.

## Impact

Source Control باید خواندن فایل را پشتیبانی کند و Scope، Profile و SQL تغییر می‌کنند. Roslyn و Build/Test مصرف‌کننده Policy خواهند بود. اجرای دستور دلخواه از فایل Policy ممنوع و خارج از محدوده است.
