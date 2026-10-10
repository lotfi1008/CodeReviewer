# Design

## Context

Findingها زیر ReviewId ذخیره می‌شوند و Id پایدار domain ندارند. ReviewEntry برای یادداشت انسانی است اما semantics ارزیابی Finding را ندارد.

## Goals / Non-Goals

**Goals:** Feedback audit‌شده، fingerprint سازگار، dataset نسخه‌دار و metric قابل بازتولید.

**Non-Goals:** fine-tuning خودکار یا تغییر خودکار Prompt.

## Decisions

- به Findings ستون `FindingUid` unique افزوده می‌شود؛ UUIDv5 از ReviewId + fingerprint فعلی تولید می‌شود. رکوردهای قدیمی batch backfill می‌شوند.
- `FindingFeedback(Id,FindingUid,Actor,Label,Comment,CreatedUtc,SupersedesId)` append-only است؛ current view آخرین نسخه هر actor را می‌دهد.
- `EvaluationDatasets`, `EvaluationItems`, `EvaluationRuns` ورودی MR/SHA و snapshot مدل/profile/prompt را نگه می‌دارند؛ Dataset پس از اجرای نخست immutable و با نسخه جدید اصلاح می‌شود.
- metric definitions ثابت: Valid برای precision denominator، Useful جدا، Unknown حذف‌شده از precision. UI sample size را همیشه نشان می‌دهد.
- Reviewer صاحب Feedback و Admin مدیر Dataset است. همه تغییرات Audit می‌شوند و هیچ Source Control write ندارند.

## Risks / Trade-offs

- [fingerprint collision] → FindingUid شامل ReviewId و collision check روی metadata.
- [bias داور] → feedback per actor حفظ و aggregation اختلاف را نشان می‌دهد.
- [داده قدیمی بی‌شاهد] → Migrated flag و عدم ورود خودکار به dataset معتبر.

## Migration Plan

ستون nullable، backfill chunked، سپس non-null/unique index ایجاد می‌شود. rollback UI و write را خاموش می‌کند؛ داده افزوده حذف نمی‌شود.

## RAG Integration

Dataset نسخه‌دار می‌تواند برای هر مورد، evidence relevance و citation validity را جدا از برچسب Finding نگه دارد. مقایسهٔ RAG خاموش/lexical/semantic باید روی MR/SHA، policy و diff یکسان انجام شود و Recall@k، Precision@k، groundedness، false positive، latency و token را با اندازهٔ نمونه گزارش دهد؛ عدم وجود قضاوت شاهد به معنی نامرتبط‌بودن نیست.
