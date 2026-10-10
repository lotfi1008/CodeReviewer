# Design

## Context

UI مستقیماً `ReviewJobQueue.Enqueue` را صدا می‌زند و endpoint ورودی خارجی وجود ندارد. GitLab adapter اطلاعات MR و Diff را دارد، اما تنظیم Automation در سطح مخزن ذخیره نمی‌شود.

## Goals / Non-Goals

**Goals:** endpoint کم‌هزینه، اعتبارسنجی پیش از Parse کامل، Deduplication پایدار و Snapshot تنظیم اجرای خودکار.

**Non-Goals:** Auto-approve، Auto-merge و پردازش رویدادهای غیر MR.

## Decisions

- endpoint `POST /api/webhooks/gitlab/{connectionId}` ابتدا `X-Gitlab-Token` را با Secret حفاظت‌شده و constant-time مقایسه می‌کند. جایگزین امضای عمومی رد شد چون GitLab Self-Managed قرارداد Token دارد.
- جدول `WebhookEvents(ConnectionId,DeliveryKey,EventType,ReceivedUtc,Status,RepositoryId,RequestNumber,HeadSha,RunId,Reason)` با unique روی `(ConnectionId,DeliveryKey)` ایجاد می‌شود. اگر UUID موجود نباشد Hash canonical از event type/project/object/sha کلید است.
- جدول `RepositoryAutomationSettings` با کلید connection/repository شامل Enabled، ModelId، ProfileId، PublishResults و event flags است. مقادیر در Payload Job Snapshot می‌شوند.
- Flow: authenticate → parse bounded body → resolve policy → fetch current MR از API → verify open/SHA → transaction ثبت event و Enqueue. Publication فقط از `PublishResults` می‌آید.
- Admin تنظیمات را تغییر می‌دهد؛ endpoint Anonymous است اما فقط Secret معتبر می‌پذیرد. Metric فقط Status و Provider دارد.

## Risks / Trade-offs

- [GitLab رویداد فاقد UUID] → fallback hash و unique منطقی Review.
- [Race بین event و fetch] → SHA جاری API مرجع و event قدیمی Ignored است.
- [SQL unavailable] → پاسخ 503 تا GitLab Retry کند؛ رویداد موفق قبل از commit پاسخ 2xx نمی‌گیرد.

## Migration Plan

1. جداول nullable و endpoint غیرفعال Deploy شوند.
2. Secret و Policy برای یک مخزن پایلوت ثبت شود.
3. Webhook فعال و Metric/Duplicate بررسی شود.
4. Rollback با Disable Policy انجام می‌شود؛ داده رویداد برای Audit باقی می‌ماند.

## RAG Integration

در صورت فعال‌بودن `add-contextual-review-retrieval`، Push معتبر همان Head SHA یک درخواست refresh/invalidation با کلید connection/repository/SHA ثبت می‌کند؛ duplicate delivery یا event قدیمی index مستقل یا شاهد کهنه ایجاد نمی‌کند. این کار مستقل از `PublishResults` است و هیچ Writeای روی MR نمی‌زند.
