# Design

## Context

`AiReviewService` امروز `GetChangesAsync` و `ProjectContextStore.ForFile` را به prompt می‌دهد؛ جست‌وجوی مخزن، خواندن فایل در commit و ردگیری شاهد وجود ندارد. Policy مخزن، صف پایدار، RBAC، Roslyn و Evaluation در Changeهای مستقل تعریف شده‌اند و این طراحی قرارداد اتصال به آن‌ها را مشخص می‌کند.

## Goals / Non-Goals

**Goals:** بازیابی دقیق و bounded، انطباق با SHA و دسترسی، استناد قابل بررسی، ارزیابی جداگانهٔ retrieval و Review، مسیر rollout و rollback بدون تغییر رفتار پیش‌فرض.

**Non-Goals:** crawl اینترنت یا مخازن نامرتبط، index کامل سازمان، vector database اجباری، اجرای کد در مسیر retrieval، تصمیم انتشار توسط مدل، و استفادهٔ خودکار از متن Work Item به‌عنوان دستور.

## Decisions

- قرارداد `IReviewContextRetriever.RetrieveAsync(ReviewContextQuery, CancellationToken)` خروجی `EvidenceChunk` و `RetrievalCoverage` می‌دهد. Query شامل `ConnectionId`, `RepositoryId`, `HeadSha`, changed paths/spans, symbol hints, effective policy hash و budget است. `EvidenceChunk` شامل path، line range، SHA، content hash، source kind، excerpt محدود و score داخلی است. این مرز به جای اتصال مستقیم prompt به Search Engine انتخاب می‌شود تا provider قابل تعویض باشد.
- فاز نخست از فهرست فایل‌های Snapshot و `GetFileAsync(..., sha)` در Source Control، chunk کردن بر مرز symbol/section و index واژگانی/مسیر استفاده می‌کند. نام/namespace/import، ارجاع Roslyn در صورت وجود، فایل‌های تست هم‌نام و اسناد مجاز candidate می‌سازند؛ ranking با وزن path/symbol/keyword و dedup است. این راه از وابستگی فوری به embedding/vector ساده‌تر و قابل توضیح‌تر است. semantic/hybrid فقط adapter اختیاری پشت flag با baseline و gate ارزیابی است؛ هر embedding بیرونی opt-in جداگانه می‌خواهد.
- Index از `RepositorySnapshot(ConnectionId,RepositoryId,Sha,PolicyHash,State,IndexedUtc)` و `ContextChunk(...,Path,StartLine,EndLine,Kind,ContentHash,SearchText,DeletedUtc)` تشکیل می‌شود؛ indexها روی scope/sha/path و کلید idempotency قرار می‌گیرند. refresh با job lease‌دار و upsert انجام می‌شود، فایل حذف‌شده tombstone می‌گیرد، Snapshot ناقص هرگز به‌عنوان کامل ارائه نمی‌شود. index جامع کل مخزن ابتدا برای مخازن پایلوت و با quota؛ fallback بازیابی on-demand از فایل‌های منتخب است.
- ترتیب کنترل داده: authorization مخزن → effective policy و SensitiveContentPolicy → دریافت bounded فایل → scan حساسیت → chunk/index → retrieval دوباره با همان policy/ACL → prompt data-only. Index و cache بر tenant/connection/repository/SHA/policy version جدا می‌شوند؛ هیچ cache cross-tenant وجود ندارد. raw snippet در Log/Trace ممنوع، storage در حالت سکون محافظت و purge مطابق retention است. Work Item تنها با اجازهٔ منبع و ACL خودش و برچسب منشأ جدا وارد می‌شود؛ در v1 پیش‌فرض فقط کد/اسناد مخزن است.
- `ReviewEvidence` با ReviewRunId، chunk identity، SHA، path/range/hash، نوع و رتبه، coverage state و نسخهٔ retriever ذخیره می‌شود؛ snapshot متن فقط در صورت نیاز بازتولید و به‌صورت محدود/محافظت‌شده نگهداری می‌شود. استناد مدل با این جدول و SHA اعتبارسنجی می‌شود. استناد معتبر به Context، الزام شاهد دقیق از Diff برای inline comment را جایگزین نمی‌کند.
- `MaxContextTokens`, `MaxCandidates`, `MaxFiles`, `MaxFileBytes`, `MaxIndexBytes`, `RetrievalTimeout`, source allowlist و mode در EffectiveReviewPolicy با سقف سازمانی تنظیم می‌شوند؛ پیش‌فرض RAG خاموش است. Timeout/partial/stale به coverage صریح تبدیل و Review موجود ادامه می‌یابد. در کمبود شاهد، ادعای cross-file قطعی پذیرفته نمی‌شود؛ انتشار همچنان از `PublishToMergeRequest` snapshot می‌آید.
- telemetry فقط زمان index/retrieval، candidate/selected counts، coverage، cache hit، توکن و error code با label کم‌تنوع ثبت می‌کند. معیارهای offline: Recall@k/Precision@k با برچسب شاهد مرتبط، grounded citation rate، false-positive rate، پذیرش یافته، latency و token delta. Dataset حداقل ۲۰ MR شامل موارد cross-file و negative است؛ baseline بدون RAG و lexical در input ثابت مقایسه می‌شوند. semantic فقط اگر امنیت و سقف هزینه/زمان رعایت و بهبود کیفیت همراه با عدم‌پسرفت false positive نشان داده شود فعال خواهد شد.

## Risks / Trade-offs

- [Index کهنه یا Snapshot ناقص] → SHA/policy-key و coverage صریح؛ عدم استفاده از شاهد قدیمی.
- [Prompt injection در کد/اسناد] → data-only envelope، citation validation و ممنوعیت tool/publication از متن بازیابی‌شده.
- [نشت Secret به index یا embedding] → فیلتر دو مرحله‌ای قبل از index و retrieval، embedding بیرونی خاموش، تست با fixture حساس.
- [هزینه و latency] → quota، top-k، cache محدود، timeout و fallback؛ معیار gate پیش از فعال‌سازی گسترده.
- [ادعای بی‌پشتوانهٔ مدل] → راستی‌آزمایی استناد و برچسب عدم قطعیت، نه تبدیل هر Context به حقیقت.

## Migration Plan

1. Schema additive و retriever غیرفعال Deploy؛ Reviewهای قدیمی بدون Evidence خوانده شوند.
2. خواندن Snapshot و lexical retrieval برای یک مخزن پایلوت با report-only، بدون تغییر prompt یا انتشار، فعال شود.
3. پس از آزمون ACL/Secret/freshness و Dataset، context محدود به prompt افزوده و معیارها با baseline مقایسه شوند.
4. semantic/hybrid فقط در صورت عبور از gate و با opt-in داده/embedding فعال شود.
5. rollback با خاموش‌کردن retrieval و توقف jobهای index انجام می‌شود؛ index قابل purge است و Review/Publication قبلی دست‌نخورده می‌مانند.
