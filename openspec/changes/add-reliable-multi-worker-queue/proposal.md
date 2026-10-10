# Proposal

## Why

صف فعلی Job را با یک State ساده Claim می‌کند و بازیابی آن برای یک میزبان طراحی شده است. برای اجرای Production باید Crash، Retry و چند Worker بدون اجرای هم‌زمان یک MR/SHA مدیریت شوند.

## What Changes

- Lease، Heartbeat، Attempt، زمان Retry و Dead-letter به Job افزوده می‌شود.
- Claim اتمیک و تمدید Lease در SQL انجام می‌شود.
- کلید یکتای Repository/MR/SHA/Profile از اجرای تکراری جلوگیری می‌کند.
- Backoff محدود و Recovery پس از توقف Worker تعریف می‌شود.

## Capabilities

### New Capabilities

- `reliable-review-queue`: اجرای پایدار، قابل Retry و چند-Worker برای Review Jobها.

### Modified Capabilities

- هیچ قابلیت موجود OpenSpec تغییر نمی‌کند.

## Impact

`ReviewJobQueue`، `ReviewWorker`، Schema SQL و وضعیت UI تغییر می‌کنند. رفتار ReviewRunهای تاریخی و انتخاب انتشار حفظ می‌شود. جایگزینی SQL Server با Broker خارجی خارج از محدوده است.
