# Design

## Context

Review Engine Diff را chunk می‌کند و call graph یا checkout مخزن ندارد. Source provider باید فایل‌های Snapshot را بخواند و Policy محدوده را تعیین کند.

## Goals / Non-Goals

**Goals:** تحلیل معنایی bounded، Ruleهای deterministic و Context کوچک برای AI.

**Non-Goals:** index کل سازمان، اثبات runtime یا جایگزینی Build/Test.

## Decisions

- `ICodeAnalysisProvider.AnalyzeAsync(AnalysisRequest)` abstraction و `RoslynCodeAnalysisProvider` ایجاد می‌شود؛ خروجی فقط DTOهای SymbolContext و DeterministicFinding است.
- Source snapshot در workspace موقت read-only با commit SHA materialize می‌شود. Solution انتخاب‌شده Policy است؛ نبود آن status مشخص می‌دهد.
- ابتدا changed spans به syntax node/symbol map و references با سقف file/symbol/time resolve می‌شوند. Package restore فقط با cache/Policy و بدون secret پیش‌فرض است.
- Ruleها RuleId/version دارند و موارد `.Result/.Wait`, nullable dereference قابل اثبات و cancellation signature را پوشش می‌دهند. Rule config در snapshot Policy ثبت می‌شود.
- Context Roslyn قبل از prompt serialize، size-limit و injection-labelled می‌شود. `FindingSources` رابطه AI/Roslyn را نگه می‌دارد و dedupe محافظه‌کارانه است.

## Risks / Trade-offs

- [Solution بزرگ] → budget و partial coverage.
- [کد uncompilable] → syntax analysis ادامه و semantic incomplete گزارش می‌شود.
- [restore خطرناک] → اجرای targetهای MSBuild خارج از runner ممنوع؛ workspace امن محدود است.

## Migration Plan

ابتدا provider پشت feature flag و report-only اجرا می‌شود، سپس Ruleهای منتخب در یک مخزن فعال و بعد Context AI اضافه می‌شود. rollback provider را غیرفعال و snapshotهای تاریخی را حفظ می‌کند.

## RAG Integration

Roslyn فقط symbol identity و reference hints مقید به همان Head SHA را به retriever می‌دهد؛ خود Roslyn index مستقلِ کل مخزن یا مسیر دورزدن SensitiveContentPolicy نمی‌سازد. نبود semantic model باعث fallback به بازیابی path/keyword می‌شود و finding قطعی Roslyn از شاهد بازیابی‌شدهٔ احتمالی متمایز می‌ماند.
