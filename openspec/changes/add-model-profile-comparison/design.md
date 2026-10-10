# Design

## Context

هر ReviewRun مستقل است و ورودی Diff دوباره از provider خوانده می‌شود؛ Comparison به Snapshot واحد و parent entity نیاز دارد.

## Goals / Non-Goals

**Goals:** ورودی یکسان، variant isolation، matching توضیح‌پذیر و publication صفر.

**Non-Goals:** رتبه‌بندی عمومی مدل یا انتخاب خودکار winner.

## Decisions

- `ComparisonRuns` parent با SnapshotId و `ComparisonVariants` با ReviewRunId/model/profile ایجاد می‌شوند. snapshot شامل MR/SHA، diffs، context و policy hash است.
- Variant Job نوع مشخص `Comparison` دارد و `PublishToMergeRequest=false` در invariant دامنه و publication service enforce می‌شود، نه فقط UI.
- matcher ابتدا exact fingerprint، سپس file/line/category و similarity محدود عنوان را اعمال می‌کند؛ ambiguousها merge نمی‌شوند.
- metric usage از ReviewEvents/LLM usage و feedback از FindingUid خوانده می‌شود. UI sample size و unknown را نشان می‌دهد.
- Snapshot size limit و retention مستقل دارد؛ access مطابق repository است.

## Risks / Trade-offs

- [هزینه چندبرابر] → estimate و confirmation پیش از enqueue.
- [nondeterminism مدل] → input hash و زمان اجرا ثبت، برابری خروجی ادعا نمی‌شود.
- [matching اشتباه] → conservative thresholds و گروه Ambiguous.

## Migration Plan

جداول additive و UI پشت feature flag Deploy می‌شوند. ابتدا دو variant و مدل mock، سپس پایلوت واقعی فعال می‌شود. rollback enqueue جدید را متوقف می‌کند.

## RAG Integration

Comparison برای سنجش مدل/پروفایل، شواهد RAG را در Snapshot مشترک ثابت می‌کند. مقایسهٔ اثر RAG، variantهای off/lexical/semantic را با همان diff، SHA و policy اما mode متفاوت اجرا می‌کند و retrieval quality، groundedness، هزینه و latency را جدا از اختلاف مدل گزارش می‌دهد. هیچ variant مجاز به انتشار روی MR نیست.
