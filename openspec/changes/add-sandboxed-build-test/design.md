# Design

## Context

Blazor host و ReviewWorker در یک Process هستند و اجرای فرمان repository مرز امنیتی ندارد. Policy مخزن باید فقط command preset و solution را انتخاب کند.

## Goals / Non-Goals

**Goals:** runner جدا، resource limit، cancellation و نتیجه قابل اعتماد.

**Non-Goals:** اجرای shell دلخواه یا دسترسی عمومی اینترنت.

## Decisions

- قرارداد `IBuildTestRunner.SubmitAsync` با service/worker جدا و حساب OS محدود تعریف می‌شود؛ وب مستقیماً Process repository را اجرا نمی‌کند.
- `BuildExecution` شامل RunId، SnapshotSha، preset، state، timestamps، exit code، limits و artifact refs است. command از preset سرور (`dotnet restore/build/test`) ساخته می‌شود نه رشته مخزن.
- workspace ephemeral و read-only source + writable output است؛ Process tree در Windows Job Object یا isolation معادل قرار می‌گیرد. network deny و allowlist feed اختیاری است.
- stdout/stderr streaming با redaction، سقف byte و tail محفوظ ذخیره می‌شود. نتیجه build/test در ReviewDecision جداست و AI نمی‌تواند آن را جعل کند.
- Queue مستقل یا job type مجزا با lease استفاده می‌شود؛ publication فقط پس از terminal policy و همچنان با opt-in است.

## Risks / Trade-offs

- [Sandbox ناقص Windows] → اجرای runner روی host جدا توصیه و capability check fail-closed.
- [restore کند] → cache فقط package و با tenant isolation.
- [output حساس] → regex+known secret redaction و size cap.

## Migration Plan

ابتدا runner health و no-op preset، سپس build بدون network روی مخزن نمونه و در آخر test فعال می‌شود. rollback policy را disabled و executionهای جاری را cancel می‌کند.

## RAG Integration

Build/Test output به‌صورت پیش‌فرض وارد index یا embedding نمی‌شود، چون می‌تواند Secret و متن تزریق‌شده داشته باشد. فقط status و ارجاع به artifact پالایش‌شده در گزارش باقی می‌ماند؛ اگر در آینده بازیابی خروجی لازم شود، قرارداد منبع، ACL و retention جداگانه می‌خواهد. انتخاب تست با شواهد RAG نمی‌تواند preset/command allowlist را گسترش دهد.
