# Spec Delta

## Purpose

این قابلیت اجرای Review Job را میان چند Worker با Lease، Retry محدود و بازیابی قابل اطمینان هماهنگ می‌کند.

## ADDED Requirements

### Requirement: Claim انحصاری با Lease
سامانه SHALL هر Job آماده را به‌صورت اتمیک فقط به یک Worker Lease دهد و تا اعتبار Lease، Worker دیگر حق اجرای آن Job را ندارد.

#### Scenario: رقابت دو Worker
- **WHEN** دو Worker هم‌زمان یک Job آماده را Claim می‌کنند
- **THEN** فقط یکی Lease معتبر دریافت می‌کند و دیگری Job متفاوت یا پاسخ خالی می‌گیرد

### Requirement: Heartbeat و Recovery
Worker SHALL Heartbeat ثبت کند و سامانه MUST Job دارای Lease منقضی را بدون ازدست‌دادن ReviewRun برای Retry واجد شرایط کند.

#### Scenario: توقف Worker
- **WHEN** Heartbeat تا پایان Lease تمدید نمی‌شود
- **THEN** Job پس از انقضا دوباره قابل Claim می‌شود و Attempt افزایش می‌یابد

### Requirement: Retry و Dead-letter
سامانه SHALL خطاهای موقت را با Backoff محدود Retry و خطاهای دائمی یا عبور از سقف Attempt را Dead-letter کند.

#### Scenario: عبور از سقف تلاش
- **WHEN** Job به حداکثر Attempt تنظیم‌شده می‌رسد
- **THEN** Job DeadLetter و ReviewRun Failed می‌شود و Retry خودکار دیگری انجام نمی‌شود

### Requirement: جلوگیری از اجرای منطقی تکراری
سامانه MUST برای connection/repository/MR/SHA/profile تنها یک Job فعال داشته باشد و انتخاب PublishToMergeRequest را در Payload ثابت نگه دارد.

#### Scenario: Enqueue هم‌زمان یک Review
- **WHEN** دو درخواست هم‌زمان همان کلید منطقی را Enqueue می‌کنند
- **THEN** هر دو به یک Run موجود اشاره می‌کنند و فقط یک Job فعال ذخیره می‌شود
