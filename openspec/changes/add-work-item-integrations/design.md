# Design

## Context

ProjectContext دستی است و abstraction Work Item وجود ندارد. Source credentials الگوی ConnectionCatalog رمزگذاری‌شده دارند.

## Goals / Non-Goals

**Goals:** مدل vendor-neutral، context snapshot، explicit write و idempotency.

**Non-Goals:** sync کامل workflow یا تغییر خودکار status.

## Decisions

- `IWorkItemProvider` متدهای Get/Create/AddComment/Link و `WorkItem` داخلی با Title، Description، AcceptanceCriteria، Url و UpdatedUtc دارد.
- `WorkItemConnections` secret حفاظت‌شده و project mapping را نگه می‌دارد. Providerهای Jira REST و Azure DevOps Work Items پشت factory resolve می‌شوند.
- reference detector فقط الگوهای configured در branch/title/description را می‌پذیرد؛ چند match در UI انتخاب می‌شود و حدس خودکار نمی‌شود.
- `WorkItemSnapshots` با ReviewId و source id ذخیره، content limit و untrusted delimiter در prompt اعمال می‌شود.
- create/comment نیازمند Reviewer، preview و idempotency marker شامل FindingUid است؛ Audit نتیجه خارجی را ثبت می‌کند.

## Risks / Trade-offs

- [Acceptance Criteria ساختار متفاوت] → mapping configurable و raw bounded fallback.
- [prompt injection] → data-only envelope و عدم ترکیب با system instructions.
- [duplicate issue] → marker و search/reconcile پیش از retry.

## Migration Plan

ابتدا interface و Jira read-only، سپس Azure read-only، بعد explicit create/comment فعال می‌شود. rollback write capability را disable می‌کند و snapshotها باقی می‌مانند.

## RAG Integration

Work Item فقط پس از انتخاب/تأیید مرجع و با مجوز مستقل provider به‌عنوان منبع جداگانه وارد retriever می‌شود؛ title/acceptance criteria با URL، updated time و source id برچسب می‌خورند. متن آن data-only، bounded و قابل حذف است و با کد مخزن در یک index یا cache بدون scope مشترک ادغام نمی‌شود. بازیابی متن Work Item هرگز Create/Comment/Link را آغاز نمی‌کند.
