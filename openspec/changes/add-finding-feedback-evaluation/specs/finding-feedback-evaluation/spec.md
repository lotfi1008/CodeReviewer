# Spec Delta

## Purpose

این قابلیت قضاوت انسانی درباره یافته‌ها را ثبت و کیفیت مدل و Profile را روی مجموعه‌های ارزیابی قابل اندازه‌گیری می‌کند.

## ADDED Requirements

### Requirement: ثبت Feedback نسخه‌دار
سامانه SHALL به Reviewer اجازه دهد یکی از Valid، FalsePositive، Useful، NotUseful یا AlreadyKnown را با توضیح اختیاری روی Finding پایدار ثبت کند.

#### Scenario: تغییر نظر Reviewer
- **WHEN** همان کاربر Feedback تازه‌ای برای همان Finding ثبت می‌کند
- **THEN** نسخه جدید ذخیره و نسخه قبلی برای Audit حفظ می‌شود

### Requirement: هویت پایدار Finding
سامانه MUST Fingerprint را از Snapshot تغییر، مکان، دسته و محتوای نرمال‌شده بسازد تا Feedback با بازنویسی گزارش تاریخی گم نشود.

#### Scenario: بارگذاری گزارش قدیمی
- **WHEN** گزارش دارای Finding پیش از این قابلیت خوانده می‌شود
- **THEN** شناسه سازگار هنگام Migration تولید می‌شود و متن Finding تغییر نمی‌کند

### Requirement: Evaluation قابل بازتولید
سامانه SHALL مجموعه MRهای برچسب‌خورده را با نسخه Dataset، مدل، Profile و Prompt snapshot ارزیابی و precision، acceptance و false-positive rate را گزارش کند.

#### Scenario: اجرای دوباره Dataset
- **WHEN** Dataset و تنظیمات یکسان دوباره ارزیابی می‌شوند
- **THEN** نتایج به‌عنوان Run جدا با ورودی‌های یکسان قابل مقایسه ذخیره می‌شوند

### Requirement: جداسازی Feedback از Publication
سامانه MUST ثبت Feedback را بدون ایجاد Comment، Status یا Label روی MR انجام دهد.

#### Scenario: ثبت False Positive
- **WHEN** Reviewer یک Finding را FalsePositive علامت می‌زند
- **THEN** فقط داده Feedback تغییر می‌کند و Source Control هیچ Write دریافت نمی‌کند
