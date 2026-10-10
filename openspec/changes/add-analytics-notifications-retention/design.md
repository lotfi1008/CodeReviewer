# Design

## Context

History صفحه‌بندی دارد اما aggregate، scheduler، export job و purge وجود ندارد. ReviewEvents و Snapshotها ممکن است نامحدود رشد کنند.

## Goals / Non-Goals

**Goals:** aggregate دقیق، notification idempotent، export bounded و retention audit‌شده.

**Non-Goals:** data warehouse یا BI عمومی.

## Decisions

- queryهای analytics ابتدا روی indexهای ReviewRuns/Findings/Feedback و viewهای SQL اجرا می‌شوند؛ aggregate روزانه فقط پس از نیاز حجمی materialize می‌شود.
- `NotificationRules`, `NotificationDeliveries` با unique event/rule/destination و provider abstraction ایجاد می‌شوند. payload قالب ثابت و بدون code است.
- `ExportJobs` در queue پایدار اجرا و فایل در storage با TTL، hash و authorization owner ذخیره می‌شود؛ CSV injection escape و PDF از formatter امن استفاده می‌کند.
- `RetentionPolicies` و `LegalHolds` تعیین می‌کنند؛ `RetentionRuns` ابتدا DryRun و سپس confirmation token کوتاه‌عمر می‌خواهد. حذف child-to-parent در batch انجام می‌شود.
- Analytics Viewer مجاز، تنظیم rule/retention Admin، export داده مجاز بر اساس repository access است.

## Risks / Trade-offs

- [query سنگین] → index، time range اجباری و async export.
- [اعلان تکراری] → delivery key و reconcile provider.
- [حذف غیرقابل برگشت] → dry-run، backup policy و legal hold.

## Migration Plan

ابتدا index و read-only dashboard، سپس export/notification، و retention آخر با dry-run-only Deploy می‌شود. rollback schedulerها را disable می‌کند.

## RAG Integration

Dashboard فقط metricهای aggregate بازیابی، پوشش، grounded citation و هزینه/زمان را با حداقل اندازهٔ نمونه نمایش می‌دهد. `RetentionPolicies` علاوه بر Review data، index chunks، evidence metadata و embeddingهای احتمالی را با همان ACL و Legal Hold پوشش می‌دهد؛ حذف مخزن cascade کنترل‌شده و audit‌شده است. اعلان و Export شامل raw snippet پیش‌فرض نیستند.
