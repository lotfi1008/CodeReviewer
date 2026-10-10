# Proposal

## Why

احراز هویت Windows در Production وجود دارد، اما همه کاربران احرازشده عملاً دسترسی یکسان دارند و عملیات مدیریتی Audit کامل ندارد. پایلوت سازمانی به Least Privilege و ردگیری تغییرات حساس نیاز دارد.

## What Changes

- نقش‌های `Admin`، `Reviewer` و `Viewer` و Policyهای مجزا اضافه می‌شوند.
- تغییر اتصال، پروفایل، Context، اجرای Review، لغو و انتشار Audit می‌شوند.
- فایل‌های حساس پیش از ذخیره Snapshot یا ارسال به مدل مسدود می‌شوند.
- TLS معتبر در Production پیش‌فرض و Secret rotation قابل انجام می‌شود.

## Capabilities

### New Capabilities

- `rbac-security-audit`: مجوزدهی، حفاظت داده حساس و Audit سازمانی.

### Modified Capabilities

- هیچ قابلیت موجود OpenSpec تغییر نمی‌کند.

## Impact

Authorization، صفحات مدیریتی، مسیرهای Review، SQL و پیکربندی استقرار تحت تأثیرند. وابستگی به صف پایدار برای Audit اجرای Worker وجود دارد. سامانه مدیریت هویت مستقل خارج از محدوده است.
