# Design

## Context

برنامه `ILogger` و ReviewEvents دارد ولی Activity/Meter و endpoint Health ندارد. Log داخلی حاوی پیام‌های فارسی است و نباید به محل ذخیره Source Code تبدیل شود.

## Goals / Non-Goals

**Goals:** OpenTelemetry استاندارد، correlation سرتاسری، health مناسب orchestrator و cardinality کنترل‌شده.

**Non-Goals:** انتخاب اجباری Prometheus، Application Insights یا Grafana.

## Decisions

- یک `ActivitySource` و `Meter` با نام نسخه‌دار تعریف می‌شود؛ instrumentation ASP.NET/HttpClient/SqlClient و spanهای دستی Review/LLM/Publish اضافه می‌شوند.
- CorrelationId از header معتبر یا GUID جدید ساخته، در ReviewRun/Job/WebhookEvent ذخیره و به outbound metadata غیرحساس افزوده می‌شود.
- metric labels فقط provider، operation، terminal status و model connection id هستند؛ MR id، file و prompt label نمی‌شوند.
- `/health/live` فقط process، `/health/ready` SQL و queue claim، و `/health/dependencies` با دسترسی Admin وضعیت cached سرویس‌ها را می‌دهد تا probe باعث بار خارجی نشود.
- exporter از Configuration فعال است؛ sampling و endpoint بدون تغییر کد تنظیم می‌شوند. redaction middleware header/body حساس را حذف می‌کند.

## Risks / Trade-offs

- [هزینه Trace] → sampling parent-based و spanهای chunk به event محدود.
- [cardinality] → review/file identifiers فقط baggage/log field، نه metric label.
- [health cascade] → dependency health readiness را برای اختلال کوتاه خارجی fail نمی‌کند.

## Migration Plan

ابتدا telemetry بدون exporter Deploy و تست redaction اجرا می‌شود؛ سپس exporter در محیط پایلوت و alertها فعال می‌شوند. rollback با غیرفعال‌کردن exporter است.

## RAG Integration

span و metric بازیابی/Indexing فقط latency، coverage، تعداد candidate/selected، cache hit، token و error code کم‌تنوع دارند. path، excerpt، query، embedding و محتوای Work Item وارد Trace، Log یا label نمی‌شوند. اختلال retrieval در dashboard از خطای Review متمایز است.
