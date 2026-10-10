# Proposal

## Why

اجرای مجدد با مدل یا Profile دیگر ممکن است، اما مقایسه ساختاریافته روی Snapshot یکسان وجود ندارد. انتخاب مدل برای تیم باید بر اساس کیفیت، زمان و مصرف قابل مقایسه باشد.

## What Changes

- Comparison Run با یک MR/SHA و دو یا چند تنظیم ایجاد می‌شود.
- یافته‌های مشترک، منحصربه‌فرد و متناقض تطبیق داده می‌شوند.
- زمان، Token و Feedback هر گزینه گزارش می‌شود.
- نتیجه Comparison هرگز به‌صورت خودکار روی MR منتشر نمی‌شود.

## Capabilities

### New Capabilities

- `model-profile-comparison`: اجرای کنترل‌شده و مقایسه نتایج مدل‌ها و Profileها.

### Modified Capabilities

- هیچ قابلیت موجود OpenSpec تغییر نمی‌کند.

## Impact

Queue، ReviewRun، Finding matching و UI گزارش تغییر می‌کنند. به Feedback برای معیار کیفیت وابسته است. Benchmark عمومی مدل‌ها خارج از محدوده است.
