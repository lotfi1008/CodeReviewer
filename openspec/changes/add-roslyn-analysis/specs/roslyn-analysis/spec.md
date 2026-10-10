# Spec Delta

## Purpose

این قابلیت Context معنایی محدود و قابل اثبات C# را برای Ruleهای قطعی و Review هوشمند فراهم می‌کند.

## ADDED Requirements

### Requirement: تحلیل محدود به Snapshot
سامانه SHALL Solution و فایل‌های لازم را از Commit بررسی‌شده بخواند و فقط Symbolهای مرتبط با خطوط تغییرکرده و بودجه Policy را تحلیل کند.

#### Scenario: فایل C# تغییرکرده
- **WHEN** Diff معتبر C# و Solution قابل بارگذاری وجود دارد
- **THEN** Symbol، Type، Nullable، Invocation و Reference مرتبط در Snapshot تحلیل ذخیره می‌شوند

### Requirement: Ruleهای قطعی قابل استناد
سامانه SHALL یافته قطعی Roslyn را فقط با RuleId، فایل، خط، Symbol و شاهد قابل بازتولید تولید کند.

#### Scenario: استفاده blocking از async
- **WHEN** Rule فعال استفاده `.Result` روی Task را در خط تغییرکرده اثبات می‌کند
- **THEN** Finding ساختاریافته با منبع Roslyn تولید می‌شود

### Requirement: Failure ایمن تحلیل
سامانه MUST نبود Solution، شکست Restore یا Compilation ناقص را به‌عنوان Coverage مشخص ثبت کند و نباید از آن نتیجه سلامت کد بگیرد.

#### Scenario: Solution قابل بارگذاری نیست
- **WHEN** Workspace نتواند Project را بارگذاری کند
- **THEN** Review AI می‌تواند طبق Policy ادامه یابد ولی گزارش RoslynIncomplete نمایش می‌دهد

### Requirement: ادغام بدون تکرار
سامانه SHALL یافته‌های معنایی و AI با Fingerprint و RuleId تطبیق دهد و یک مشکل را بیش از یک بار منتشر نکند.

#### Scenario: یافته مشترک AI و Roslyn
- **WHEN** هر دو منبع همان فایل، خط و نقص را گزارش می‌کنند
- **THEN** یک Finding با Sources ترکیبی در گزارش ظاهر می‌شود
