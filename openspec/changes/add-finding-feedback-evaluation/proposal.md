# Proposal

## Why

سامانه یافته تولید می‌کند اما صحت و مفیدبودن آن‌ها به‌صورت ساختاریافته ثبت نمی‌شود. بدون Feedback نمی‌توان False Positive یا کیفیت مدل و پروفایل را در پایلوت سنجید.

## What Changes

- Feedbackهای Valid، False Positive، Useful، Not Useful و Already Known ثبت می‌شوند.
- Finding شناسه پایدار و نسخه Feedback با هویت کاربر خواهد داشت.
- مجموعه ارزیابی MRها و گزارش کیفیت مدل/پروفایل ایجاد می‌شود.
- Feedback گزارش تاریخی را بازنویسی نمی‌کند.

## Capabilities

### New Capabilities

- `finding-feedback-evaluation`: ثبت قضاوت انسانی و محاسبه معیارهای ارزیابی Review.

### Modified Capabilities

- هیچ قابلیت موجود OpenSpec تغییر نمی‌کند.

## Impact

مدل Finding، SQL، گزارش Review و داشبورد تحت تأثیرند. Analytics بعدی مصرف‌کننده این داده است. آموزش خودکار مدل خارج از محدوده است.
