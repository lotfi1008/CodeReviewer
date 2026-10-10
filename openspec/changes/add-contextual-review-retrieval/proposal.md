# Proposal

## Why

Review فعلی عمدتاً Diff و Context دستی را می‌بیند؛ بنابراین دربارهٔ تعریف‌ها، مصرف‌کنندگان، تست‌ها و قواعد مرتبط خارج از Diff شواهد کافی ندارد. بازیابی زمینهٔ مرتبط در همان Snapshot می‌تواند دقت و توضیح‌پذیری یافته‌ها را بهبود دهد، به شرط آن‌که امنیت، تازگی و هزینهٔ آن قابل سنجش باشد.

## What Changes

- بازیابی شواهد کد و مستندات مجاز مخزن در Head SHA بازبینی‌شده به Review افزوده می‌شود؛ هر شاهد مسیر، بازهٔ خطوط، SHA و منشأ دارد.
- مسیر اولیه بازیابی قطعی مبتنی بر path، keyword و symbol است؛ جست‌وجوی semantic/hybrid فقط به‌صورت اختیاری و پس از ارزیابی فعال می‌شود. پایگاه‌دادهٔ برداری برای نسخهٔ نخست الزامی نیست.
- Indexing افزایشی، حذف/ابطال Snapshot، محدودیت دسترسی، حذف فایل حساس، بودجهٔ زمان/توکن و رفتار هنگام نبود یا کهنگی Context تعریف می‌شود.
- کیفیت retrieval و اثر آن بر کیفیت Review در Dataset نسخه‌دار با حالت RAG روشن/خاموش سنجیده می‌شود.
- انتشار نتیجه روی MR همچنان فقط با `PublishToMergeRequest=true` انجام می‌شود؛ بازیابی یا Indexing هیچ Writeای روی MR ایجاد نمی‌کند.

## Capabilities

### New Capabilities

- `contextual-review-retrieval`: ساخت و بازیابی امن و قابل استناد زمینهٔ مرتبط با Review و ارزیابی آن.

### Modified Capabilities

- هیچ Spec نهایی موجودی تغییر نمی‌کند.

## Impact

`AiReviewService`، قرارداد خواندن Source Control در SHA، Policy مؤثر مخزن، صف، SQL/Storage، امنیت، telemetry و Evaluation درگیر می‌شوند. وابستگی پایه به `add-repository-review-policy`، `add-reliable-multi-worker-queue` و `add-rbac-security-audit` است؛ Roslyn، Work Item و Observability اتصال‌های اختیاری و مرحله‌ای هستند. آموزش مدل، Web crawl، عامل خودمختار و ارسال کل مخزن یا Secret به مدل خارج از محدوده‌اند. Reviewهای تاریخی بدون بازنویسی باقی می‌مانند.
