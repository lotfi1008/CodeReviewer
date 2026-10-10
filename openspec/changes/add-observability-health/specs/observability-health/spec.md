# Spec Delta

## Purpose

این قابلیت وضعیت عملیاتی Review، صف، مدل و Source Control را بدون افشای کد یا Secret قابل مشاهده و پایش می‌کند.

## ADDED Requirements

### Requirement: Correlation سرتاسری
سامانه SHALL یک CorrelationId پایدار را از ورودی UI/Webhook تا Job، ReviewRun، فراخوانی مدل و انتشار حمل کند.

#### Scenario: Review ناشی از Webhook
- **WHEN** Webhook یک Review ایجاد می‌کند
- **THEN** Traceها و Eventهای تمام مراحل با CorrelationId مشترک قابل جست‌وجو هستند

### Requirement: Metricهای عملیاتی
سامانه SHALL زمان Review و LLM، Token، تعداد فایل و یافته، Retry، Queue depth و خطای Publication را با برچسب‌های کم‌کاردینال ثبت کند.

#### Scenario: پایان Review
- **WHEN** Review به وضعیت نهایی می‌رسد
- **THEN** شمارش نتیجه و Duration یک‌بار با Provider و Status ثبت می‌شود

### Requirement: Health تفکیک‌شده
سامانه SHALL liveness را بدون وابستگی خارجی و readiness را بر اساس SQL و قابلیت Claim Worker ارائه دهد؛ خرابی GitLab یا مدل باید در dependency health جدا گزارش شود.

#### Scenario: SQL در دسترس نیست
- **WHEN** readiness اجرا و SQL قابل دسترس نیست
- **THEN** readiness ناموفق و liveness موفق باقی می‌ماند

### Requirement: حفاظت Telemetry
سامانه MUST از ثبت Prompt، Diff، Token، Authorization header و Secret در Log، Trace و Metric جلوگیری کند.

#### Scenario: خطای API خارجی
- **WHEN** فراخوانی مدل یا GitLab خطا می‌دهد
- **THEN** Telemetry فقط نوع عملیات، وضعیت، مدت و شناسه‌های غیرحساس را ثبت می‌کند
