# Spec Delta

## Purpose

این قابلیت چند تنظیم Review را روی Snapshot یکسان اجرا و کیفیت، زمان و هزینه نتایج را بدون اثر روی MR مقایسه می‌کند.

## ADDED Requirements

### Requirement: ورودی مشترک Comparison
سامانه SHALL یک MR/SHA، Diff و Context Snapshot ثابت را برای همه Variantها استفاده و مدل و Profile هر Variant را ثبت کند.

#### Scenario: تغییر MR حین Comparison
- **WHEN** Head SHA پس از ایجاد Comparison تغییر می‌کند
- **THEN** Variantها همچنان Snapshot اولیه را مقایسه و گزارش stale بودن را نمایش می‌دهند

### Requirement: تطبیق یافته‌ها
سامانه SHALL یافته‌ها را بر اساس مکان، دسته و شباهت نرمال‌شده به مشترک، منحصربه‌فرد یا متناقض گروه‌بندی و Match مبهم را مشخص کند.

#### Scenario: یافته مشترک
- **WHEN** دو Variant نقص یکسانی را در همان فایل و خط گزارش می‌کنند
- **THEN** یک گروه مشترک با ارجاع به هر دو Finding نمایش داده می‌شود

### Requirement: معیارهای قابل مقایسه
سامانه SHALL Duration، Token، تعداد یافته پذیرفته و Feedback را برای هر Variant بدون ادعای برتری فاقد داده کافی گزارش کند.

#### Scenario: نبود Feedback
- **WHEN** هیچ Feedback انسانی برای Comparison وجود ندارد
- **THEN** معیار کیفیت Unknown نمایش داده می‌شود و فقط زمان و مصرف مقایسه می‌شوند

### Requirement: عدم Publication
سامانه MUST تمام Comparison Runها را بدون Summary، Comment، Check یا Label روی Source Control اجرا کند.

#### Scenario: Profile دارای انتشار فعال
- **WHEN** Variant از Profile یا تنظیمی با Publication فعال ساخته می‌شود
- **THEN** Comparison آن گزینه را Override می‌کند و هیچ Write خارجی انجام نمی‌شود
