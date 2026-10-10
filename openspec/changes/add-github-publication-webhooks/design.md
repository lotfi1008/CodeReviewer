# Design

## Context

`GitHubProvider` PR و files را می‌خواند، اما default `PublishReviewAsync` NotSupported است. Publication GitLab جزئیات vendor را جدا نگه داشته است.

## Goals / Non-Goals

**Goals:** webhook امضاشده، check و comment idempotent با semantic برابر GitLab.

**Non-Goals:** نصب خودکار GitHub App یا merge/approval.

## Decisions

- webhook `X-Hub-Signature-256` را روی raw bounded body constant-time و `X-GitHub-Delivery` را unique می‌کند. تنظیم repository همان مدل Automation مشترک است.
- interface publication به عملیات داخلی Summary/Inline/Status شکسته می‌شود، اما orchestrator vendor-neutral ترتیب و Head guard را کنترل می‌کند.
- Summary marker و finding fingerprint در body پنهان، Inline از review comment API و Status از Check Runs (GitHub App) یا commit status fallback طبق connection capability استفاده می‌کند.
- قبل از batch و هر مرحله write، PR open و head SHA چک می‌شود. پاسخ مبهم با read-after-write reconcile می‌شود.
- scope test هنگام ذخیره connection قابلیت read/comment/check را گزارش می‌دهد؛ tokenها Data Protection می‌مانند.

## Risks / Trade-offs

- [تفاوت GitHub App/PAT] → capability negotiation و status fallback.
- [rate limit] → conditional requests، pagination bounded و retry-after.
- [line mapping] → فقط خط افزوده معتبر، در غیر این صورت Summary fallback بدون inline جعلی.

## Migration Plan

ابتدا read-only webhook validation، سپس check در repo آزمایشی، بعد comments و automation فعال می‌شود. rollback connection policy را disable می‌کند.

## RAG Integration

GitHub provider قرارداد خواندن فایل و فهرست Snapshot در SHA و webhook-driven invalidation را مانند GitLab پشتیبانی می‌کند. Permissionهای read محتوا از write publication جدا گزارش می‌شوند؛ فعال‌بودن retrieval هرگز مجوز comment/check یا انتشار خودکار محسوب نمی‌شود.
