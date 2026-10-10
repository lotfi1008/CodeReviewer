# Proposal

## Why

Context کسب‌وکار اکنون دستی است و به Work Item واقعی متصل نیست. اتصال Jira و Azure Boards امکان بررسی هم‌راستایی تغییر با Acceptance Criteria و ساخت Issue کنترل‌شده را می‌دهد.

## What Changes

- `IWorkItemProvider` و مدل داخلی Vendor-neutral اضافه می‌شوند.
- Jira و Azure Boards برای خواندن Context و ثبت Comment پشتیبانی می‌شوند.
- Work Item از MR تشخیص داده و Snapshot آن به Review متصل می‌شود.
- ساخت Issue از Finding فقط با اقدام صریح کاربر انجام می‌شود.

## Capabilities

### New Capabilities

- `work-item-integration`: دریافت Requirement و ایجاد/پیوند Work Item از طریق Provider مستقل.

### Modified Capabilities

- هیچ قابلیت موجود OpenSpec تغییر نمی‌کند.

## Impact

Connection Catalog، Context، Review و UI تغییر می‌کنند. Secret و Permission جدید لازم است. تغییر خودکار وضعیت Task و ایجاد خودکار Issue خارج از محدوده است.
