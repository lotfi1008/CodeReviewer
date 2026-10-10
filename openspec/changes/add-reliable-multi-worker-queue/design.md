# Design

## Context

`ReviewJobs` فقط State و CancelRequested دارد و Claim با UPDATE انجام می‌شود. Startup تمام Runningها را Queued می‌کند که در استقرار چند نمونه می‌تواند Job سالم میزبان دیگر را بدزدد.

## Goals / Non-Goals

**Goals:** at-least-once execution با اثر idempotent، چند Worker، Retry قابل پیش‌بینی و Recovery بدون reset جمعی.

**Non-Goals:** exactly-once توزیع‌شده و جایگزینی SQL Server با Broker.

## Decisions

- ستون‌های `LeaseOwner`, `LeaseExpiresUtc`, `HeartbeatUtc`, `Attempt`, `MaxAttempts`, `NextAttemptUtc`, `LastErrorCode`, `CompletedUtc` و `LogicalKey` افزوده می‌شوند؛ index آماده‌بودن و unique filtered برای LogicalKey فعال ساخته می‌شود.
- Claim در transaction با `UPDLOCK, READPAST, ROWLOCK` و شرط Queued/Retry + NextAttempt انجام و Lease 60 ثانیه صادر می‌شود. Worker هر 20 ثانیه تمدید می‌کند.
- `Complete`, `Retry`, `DeadLetter`, `Cancel` فقط با RunId و LeaseOwner فعلی update می‌کنند. از دست‌رفتن Lease اجرای جاری را cancel می‌کند.
- خطاها به Transient/Permanent طبقه‌بندی؛ backoff نمایی با jitter و سقف تنظیم‌شده است. Result خارجی از Markerهای publication idempotent استفاده می‌کند.
- Migration Payload قدیمی را حفظ و ستون‌های جدید را default می‌کند. یک SQL migration ledger نسخه Schema را ثبت می‌کند.

## Risks / Trade-offs

- [Clock skew] → زمان SQL Server، نه ساعت Worker، مرجع Lease است.
- [عملیات LLM طولانی] → heartbeat مستقل از task و cancellation هنگام lease loss.
- [at-least-once] → تمام writes خارجی و FinishReview باید idempotent شوند.

## Migration Plan

Schema additive Deploy، سپس Worker جدید فعال و بعد reset قدیمی Startup حذف می‌شود. Rollback Worker قدیمی فقط پس از Drain Jobهای Running مجاز است؛ ستون‌های جدید حذف نمی‌شوند.

## RAG Integration

Index refresh به‌صورت job type جدا با logical key شامل connection/repository/SHA/policy hash و همان lease/cancellation/retry اجرا می‌شود؛ failure آن terminal Review را خراب نمی‌کند و نتیجهٔ partial/unavailable به retriever منتقل می‌شود. Queue نباید اجرای دوبارهٔ refresh را به نسخهٔ کهنهٔ Review متصل کند.
