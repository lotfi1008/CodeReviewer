# Design

## Context

Profile و ProjectContext در SQL هستند؛ `ISourceControlProvider` فقط repository list، requests و changes را دارد و فایل دلخواه commit را نمی‌خواند.

## Goals / Non-Goals

**Goals:** Policy versioned، precedence قطعی، validation امن و snapshot مؤثر.

**Non-Goals:** script/plugin داخل repository یا override محدودیت سازمان.

## Decisions

- `ISourceControlProvider.GetFileAsync(connection,repo,path,sha)` محتوای bounded را می‌خواند. نبود فایل Policy معتبر است و defaults سازمان اعمال می‌شوند.
- YAML schema v1 فیلدهای include/exclude/sensitive، domains، architecture rules، acceptance criteria، solution و build allowlist دارد. parser duplicate key، alias، custom tag و unknown executable field را رد می‌کند.
- Merge سه‌مرحله‌ای است: OrganizationPolicy محدودیت نهایی، RepositoryPolicy قابل محدودسازی، RunProfile فقط گزینه مجاز. نتیجه `EffectiveReviewPolicy` immutable است.
- `PolicySnapshots` با ReviewId، source hashes، canonical JSON و validation warnings ذخیره می‌شود. Secret و raw فایل حساس ذخیره نمی‌شود.
- Validation در UI preview و شروع Review یکسان است؛ error مسیر YAML و code ثابت دارد.

## Risks / Trade-offs

- [تغییر schema] → version صریح و migration tool آینده.
- [YAML پیچیده] → subset محدود و خطای fail-closed.
- [Policy قدیمی در cache] → cache key شامل connection/repo/sha/path است.

## Migration Plan

Reader و preview ابتدا بدون enforcement Deploy، سپس v1 برای مخزن پایلوت فعال می‌شود. نبود فایل رفتار فعلی را حفظ می‌کند؛ rollback enforcement را خاموش می‌کند.

## RAG Integration

schema v1 بخش اختیاری `retrieval` برای mode (`off`/`lexical`/`semantic`)، source allowlist، max files/candidates/tokens و timeout دارد؛ مقادیر به سقف سازمانی محدود می‌شوند و نبود بخش، RAG را خاموش نگه می‌دارد. Hash Policy در کلید Snapshot/Index وارد می‌شود تا تغییر قواعد، cache یا شواهد قبلی را دوباره معتبر نکند. semantic و embedding بیرونی مجوز صریح مستقل می‌خواهند.
