# Design

## Context

Negotiate Authentication فقط در Production fallback authentication دارد؛ Policy سطح عملیات و Audit عمومی موجود نیست. Secretها با Data Protection ذخیره می‌شوند و `AllowUntrustedCertificates` در تنظیم فعلی true است.

## Goals / Non-Goals

**Goals:** deny-by-default، سه نقش، Audit قابل جست‌وجو، exclusion مرکزی داده حساس و تنظیم Production امن.

**Non-Goals:** Identity Provider جدید، SIEM اختصاصی یا نمایش مقدار Secret.

## Decisions

- Windows group names از Configuration به نقش‌های `Viewer`, `Reviewer`, `Admin` map می‌شوند؛ Admin شامل Reviewer/Viewer و Reviewer شامل Viewer است. endpoint/pageها Policy نام‌دار می‌گیرند.
- `AuditEvents` شامل Id، TimeUtc، Actor، Action، TargetType/Id، Outcome، CorrelationId و Metadata JSON allowlist است. Audit append-only و بدون token/diff/prompt است.
- `SensitiveContentPolicy` پیش از ReviewScope اجرا می‌شود و glob سازمانی غیرقابل override، glob مخزن و تشخیص signature کلید خصوصی دارد. فقط مسیر و rule id ثبت می‌شود.
- Production startup اگر TLS bypass فعال یا role mapping خالی باشد fail-fast می‌شود؛ rotation با ذخیره Secret جدید و نگه‌داشتن ciphertext قبلی تا test موفق انجام می‌شود.
- UI کنترل را پنهان می‌کند اما enforcement همیشه server-side است.

## Risks / Trade-offs

- [اشتباه group mapping] → diagnostics فقط نام Policy و نتیجه را بدون claim حساس نشان می‌دهد.
- [false positive فایل حساس] → Admin می‌تواند فقط rule مخزن را اصلاح کند؛ rule سازمان bypass نمی‌شود.
- [رشد Audit] → retention مستقل با legal hold.

## Migration Plan

ابتدا Audit و Policyها در report-only، سپس role mapping آزمایش و enforcement فعال می‌شود. rollback با feature flag enforcement ممکن است، اما SensitiveContentPolicy و Audit فعال می‌مانند.

## RAG Integration

همان SensitiveContentPolicy و repository authorization پیش از index و دوباره پیش از retrieval اعمال می‌شوند. cache/index/embedding بر tenant و connection جدا است؛ دادهٔ حساس حتی در صورت غیرفعال‌شدن RAG در storage، telemetry یا سرویس embedding خارجی افشا نمی‌شود. رخدادهای denied و purge فقط metadata مجاز را Audit می‌کنند.
