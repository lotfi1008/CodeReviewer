# Spec Delta

## Purpose

این قابلیت Work Item مرتبط را از Provider مستقل دریافت و Context آن را به‌صورت قابل ممیزی وارد Review می‌کند.

## ADDED Requirements

### Requirement: مدل Provider مستقل
سامانه SHALL Jira و Azure Boards را پشت قرارداد مشترک برای Get، Comment، Create و Link نگه دارد و مدل Vendor را وارد Review Engine نکند.

#### Scenario: دریافت Jira Issue
- **WHEN** MR به Issue معتبر Jira اشاره می‌کند
- **THEN** عنوان، شرح، Acceptance Criteria و لینک به مدل داخلی WorkItem نگاشت می‌شوند

### Requirement: Context Snapshot امن
سامانه MUST Work Item را داده غیرقابل اعتماد تلقی، اندازه آن را محدود و Snapshot استفاده‌شده را بدون Secret به ReviewRun متصل کند.

#### Scenario: متن شامل دستور Prompt Injection
- **WHEN** Acceptance Criteria شامل دستور برای تغییر رفتار مدل است
- **THEN** متن فقط به‌عنوان Reference Data علامت‌گذاری و System Instruction حفظ می‌شود

### Requirement: ایجاد Work Item با اقدام صریح
سامانه SHALL ساخت Issue از Finding را فقط پس از انتخاب کاربر مجاز و پیش‌نمایش Provider، Project و محتوا انجام دهد.

#### Scenario: ایجاد Issue تأییدشده
- **WHEN** Reviewer پیش‌نمایش را تأیید می‌کند
- **THEN** دقیقاً یک Work Item با Idempotency key ساخته و لینک آن ذخیره می‌شود

### Requirement: Failure غیرمخرب Provider
سامانه SHALL خطای Work Item Provider را از نتیجه Review جدا کند و در صورت نبود Context، محدودیت را شفاف گزارش دهد.

#### Scenario: Jira در دسترس نیست
- **WHEN** دریافت Context با خطای موقت شکست می‌خورد
- **THEN** طبق Policy Review متوقف یا با وضعیت ContextIncomplete ادامه می‌یابد و Comment خارجی ایجاد نمی‌شود
