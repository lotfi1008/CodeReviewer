# تحلیل و نیازمندی‌های AI Code Reviewer

**Document Type:** Product / System Requirements Analysis  
**Version:** 1.0  
**Status:** Draft  
**Target Platform:** .NET / Microsoft Stack  
**Primary Source Control:** GitLab Self-Managed  
**Future Integrations:** Jira, Trello, سایر Task Management Systems  

---

## 01. هدف سند

این سند نیازمندی‌های یک سامانه اختصاصی **AI Code Reviewer** را مشخص می‌کند.

هدف سامانه این است که فرآیند Code Review در Merge Requestهای GitLab را با استفاده از ترکیبی از:

- Microsoft Agent Framework
- Microsoft.Extensions.AI
- Roslyn
- Large Language Model
- GitLab API

هوشمند کند.

سامانه باید بتواند قبل از Human Review، تغییرات یک Merge Request را بررسی کند، مشکلات مهم را شناسایی کند، نتیجه را به شکل Structured Finding تولید کند و در صورت نیاز روی Merge Request به صورت Inline Comment یا Summary منتشر کند.

این سند روی **نیازمندی‌ها و محدوده محصول** تمرکز دارد و وارد جزئیات Low-Level Implementation نمی‌شود.

---

## 02. مسئله اصلی

Code Review دستی در پروژه‌های بزرگ معمولاً با مشکلات زیر روبه‌رو است:

- Reviewهای تکراری برای مشکلات مشابه
- وابستگی کیفیت Review به Reviewer
- از دست رفتن برخی خطاهای ساده یا تکرارشونده
- زمان زیاد Reviewer برای بررسی تغییرات کم‌اهمیت
- دشواری اعمال یکسان Coding Guidelineها در کل تیم
- دشواری کنترل Architectural Ruleها
- دشواری بررسی همزمان Security، Performance، Async Usage و Database Usage
- نبود یک History ساختاریافته از مشکلات شناسایی‌شده
- دشواری اندازه‌گیری کیفیت Merge Requestها در طول زمان

AI Code Reviewer قرار نیست جای Human Reviewer را بگیرد.

هدف آن ایجاد یک **First-Pass Review هوشمند و خودکار** است تا Human Reviewer زمان بیشتری برای Business Logic، Architecture و تصمیم‌های سطح بالاتر داشته باشد.

---

## 03. اهداف اصلی محصول

سامانه باید بتواند:

1. Merge Requestهای GitLab را دریافت کند.
2. تغییرات MR را استخراج کند.
3. فایل‌های قابل Review را تشخیص دهد.
4. Diff را تحلیل کند.
5. در صورت نیاز Context بیشتری از Repository دریافت کند.
6. برای پروژه‌های .NET از Roslyn استفاده کند.
7. Rules اختصاصی سازمان یا Repository را در Review اعمال کند.
8. از LLM برای تحلیل Contextual استفاده کند.
9. Findingها را به صورت Structured Data تولید کند.
10. Findingهای کم‌ارزش یا تکراری را حذف کند.
11. نتیجه Review را روی GitLab منتشر کند.
12. قابلیت Re-Review پس از Push جدید را داشته باشد.
13. Review History را نگهداری کند.
14. امکان توسعه به سمت Jira، Trello یا Task Management Systemهای دیگر را داشته باشد.

---

## 04. موارد خارج از Scope اولیه

در نسخه اولیه موارد زیر خارج از Scope هستند:

- جایگزینی کامل Human Code Review
- تصمیم خودکار برای Merge کردن MR
- Reject خودکار Merge Request
- تغییر خودکار Source Code بدون درخواست Developer
- Auto Commit
- Auto Push
- مدیریت کامل Sprint
- مدیریت Project Management
- ایجاد خودکار Task برای تمام Findingها
- Multi-Agent Architecture پیچیده
- پشتیبانی کامل از تمام زبان‌های برنامه‌نویسی
- Fine-Tuning اختصاصی مدل در MVP

این قابلیت‌ها می‌توانند در نسخه‌های بعدی بررسی شوند.

---

## 05. کاربران و Actorهای اصلی

### 05.1 Developer

Developer کد را Push کرده و Merge Request ایجاد یا Update می‌کند.

انتظار دارد:

- Review سریع دریافت کند.
- Commentهای قابل اقدام دریافت کند.
- False Positive کم باشد.
- Commentها تکراری نباشند.
- علت Finding قابل فهم باشد.

### 05.2 Human Reviewer

Reviewer انسانی باید بتواند:

- Findingهای AI را قبل یا هنگام Review مشاهده کند.
- Findingهای معتبر را بررسی کند.
- Findingهای اشتباه را Ignore یا Resolve کند.
- از AI به عنوان Reviewer کمکی استفاده کند.

### 05.3 Repository Administrator

مسئول تنظیم:

- Repository Rules
- Review Policies
- File Exclusions
- LLM Configuration
- Severity Threshold
- Enabled Review Categories

است.

### 05.4 System Administrator

مسئول:

- Deployment
- Secrets
- GitLab Integration
- LLM Endpoint
- Monitoring
- Security
- Infrastructure

است.

---

## 06. جریان اصلی سیستم

فرآیند پایه باید به شکل زیر باشد:

```text
Developer
    |
    v
Git Push
    |
    v
Merge Request
    |
    v
GitLab Webhook
    |
    v
AI Code Reviewer
    |
    +----> Fetch MR Metadata
    |
    +----> Fetch Diff
    |
    +----> Filter Files
    |
    +----> Load Project Rules
    |
    +----> Load Required Context
    |
    +----> Roslyn Analysis
    |
    +----> AI Analysis
    |
    +----> Normalize Findings
    |
    +----> Deduplicate Findings
    |
    +----> Apply Confidence / Severity Rules
    |
    v
GitLab Review Comments / Summary
    |
    v
Human Reviewer
```

---

## 07. Triggerهای مورد نیاز

سامانه حداقل باید Triggerهای زیر را پشتیبانی کند.

### 07.1 Merge Request Created

هنگام ایجاد MR، Review اولیه انجام شود.

### 07.2 Merge Request Updated

هنگام Push تغییرات جدید به Source Branch، Review مجدد انجام شود.

### 07.3 Manual Review

امکان اجرای Review به صورت دستی وجود داشته باشد.

این Trigger می‌تواند در آینده از طریق:

- GitLab Comment Command
- API
- UI
- Pipeline Job

فعال شود.

مثال:

```text
/ai-review
```

---

## 08. GitLab Integration Requirements

نسخه اول باید با **GitLab Self-Managed** کار کند.

سامانه باید بتواند:

- اطلاعات Project را دریافت کند.
- اطلاعات Merge Request را دریافت کند.
- Source Branch و Target Branch را تشخیص دهد.
- Diff را دریافت کند.
- Commit SHAها را دریافت کند.
- فایل Repository را بخواند.
- Comment روی MR ایجاد کند.
- Inline Discussion روی Diff ایجاد کند.
- Commentهای قبلی متعلق به AI Reviewer را تشخیص دهد.
- Re-Review را مدیریت کند.

GitLab integration نباید مستقیماً داخل Review Engine پیاده‌سازی شود.

باید یک abstraction مستقل برای Source Control وجود داشته باشد.

مفهوم پیشنهادی:

```csharp
public interface ISourceControlProvider
{
    Task<MergeRequestContext> GetMergeRequestAsync(...);

    Task<IReadOnlyCollection<ChangedFile>> GetChangesAsync(...);

    Task<string?> GetFileContentAsync(...);

    Task PublishFindingAsync(...);

    Task PublishSummaryAsync(...);
}
```

در نسخه اول implementation اصلی:

```text
GitLabSourceControlProvider
```

خواهد بود.

این تصمیم امکان اضافه کردن موارد زیر را در آینده فراهم می‌کند:

- GitHub
- Azure DevOps
- Bitbucket

---

## 09. File Selection Requirements

سامانه نباید تمام فایل‌های MR را بدون Filter به LLM ارسال کند.

باید قابلیت Include/Exclude بر اساس:

- File Extension
- Directory
- Filename Pattern
- File Size
- Generated File Detection

وجود داشته باشد.

### نمونه فایل‌های قابل Review

```text
.cs
.csproj
.props
.targets
.sql
.json
.yml
.yaml
Dockerfile
```

### نمونه فایل‌های قابل Ignore

```text
bin/
obj/
node_modules/
*.min.js
*.map
Generated/
Migrations Snapshot
Package Lock Files
Binary Files
```

Rules باید در سطح Global و Repository قابل تنظیم باشند.

---

## 10. Diff Analysis Requirements

سامانه باید Diff محور باشد.

یعنی AI نباید بدون دلیل کل Repository را دریافت کند.

فرآیند باید از Change Set شروع شود و فقط در صورت نیاز Context بیشتری دریافت شود.

برای هر تغییر باید اطلاعات زیر در دسترس باشد:

- File Path
- Old Path
- New Path
- Changed Lines
- Added Lines
- Removed Lines
- Surrounding Context
- Commit SHA
- Base SHA
- Head SHA

---

## 11. Context Retrieval Requirements

یکی از مهم‌ترین نیازمندی‌های Reviewer این است که بتواند در صورت نیاز Context بیشتر دریافت کند.

نمونه:

یک Method تغییر کرده است اما برای Review صحیح باید Interface آن نیز دیده شود.

سامانه باید بتواند Contextهایی مانند موارد زیر را بازیابی کند:

- Full File
- Related Interface
- Base Class
- Referenced Method
- Called Method
- DTO
- Entity
- Configuration
- Existing Similar Implementation
- Unit Test
- Project Rule
- Architectural Rule

LLM نباید بدون محدودیت Repository را Crawl کند.

Context Retrieval باید Controlled و Budgeted باشد.

---

## 12. Roslyn Requirements

برای پروژه‌های C#/.NET، Roslyn باید یک بخش اصلی Review Engine باشد.

Roslyn باید در آینده قادر باشد اطلاعات زیر را استخراج کند:

- Syntax Tree
- Semantic Model
- Symbols
- Method Definitions
- Type Information
- Method Invocations
- References
- Attributes
- Nullable Information
- Async Signatures
- CancellationToken Usage
- Dependency Relationships

Roslyn Analysis باید مکمل AI باشد، نه جایگزین آن.

---

## 13. AI Review Categories

نسخه اولیه باید حداقل دسته‌های زیر را پشتیبانی کند.

### 13.1 Correctness

بررسی مشکلات احتمالی در Logic و Behavior.

### 13.2 Async / Concurrency

مانند:

- `.Result`
- `.Wait()`
- Missing CancellationToken propagation
- Blocking Async Code
- Race Conditionهای قابل تشخیص

### 13.3 Performance

مانند:

- Excessive Allocation
- Unnecessary Enumeration
- Loading Large Data into Memory
- Expensive Query Pattern
- Repeated Remote Calls

### 13.4 Database

مانند:

- N+1
- Missing Parameterization
- Query Explosion
- Missing Pagination
- Incorrect Transaction Usage
- Potential Full Table Scan Pattern

### 13.5 Security

مانند:

- SQL Injection Risk
- Sensitive Data Logging
- Weak Input Validation
- Dangerous Deserialization
- Incorrect Authorization Pattern

### 13.6 Error Handling

مانند:

- Swallowed Exception
- Generic Catch
- Missing Logging
- Incorrect Exception Translation

### 13.7 Maintainability

مانند:

- Excessive Complexity
- Duplicate Logic
- Unclear Responsibility
- Unnecessary Abstraction

### 13.8 Architecture

بررسی Rules اختصاصی پروژه.

مثال:

```text
Controller must not contain business logic.
```

یا:

```text
Infrastructure layer must not be referenced by Domain.
```

---

## 14. Project Rules

سامانه باید قابلیت تعریف Ruleهای اختصاصی را داشته باشد.

ساختار پیشنهادی:

```text
.ai-review/
    general.md
    dotnet.md
    architecture.md
    database.md
    security.md
    performance.md
```

Ruleها باید بتوانند در دو سطح وجود داشته باشند:

### Global Rules

برای کل سازمان.

### Repository Rules

مختص یک Repository.

Repository Rules باید بتوانند Global Ruleها را تکمیل یا Override کنند.

---

## 15. Structured Finding

خروجی LLM نباید Free Text باشد.

هر Finding باید حداقل اطلاعات زیر را داشته باشد:

```json
{
  "category": "performance",
  "severity": "high",
  "confidence": 0.92,
  "file": "src/ReportService.cs",
  "line": 142,
  "title": "Potential N+1 query",
  "description": "...",
  "reason": "...",
  "suggestion": "...",
  "ruleId": "DB-004"
}
```

این ساختار برای:

- Validation
- Filtering
- Deduplication
- Reporting
- Analytics
- Jira Integration
- Trello Integration

ضروری است.

---

## 16. Severity Levels

Findingها باید حداقل Severityهای زیر را داشته باشند:

```text
Critical
High
Medium
Low
Info
```

امکان Config کردن حداقل Severity قابل انتشار باید وجود داشته باشد.

مثال:

```text
Publish only:
Critical
High
Medium
```

---

## 17. Confidence Score

هر Finding تولیدشده توسط AI باید Confidence داشته باشد.

مثال:

```text
0.00 - 1.00
```

امکان تعریف Threshold لازم است.

مثال:

```text
MinimumPublishConfidence = 0.75
```

Findingهای زیر Threshold نباید به صورت Inline Comment منتشر شوند.

در آینده می‌توان آن‌ها را برای Telemetry یا Evaluation نگهداری کرد.

---

## 18. Deduplication

سامانه باید جلوی Commentهای تکراری را بگیرد.

برای هر Finding باید یک Stable Fingerprint تولید شود.

Fingerprint می‌تواند بر اساس ترکیبی از موارد زیر باشد:

- Repository
- File
- Rule
- Code Region
- Finding Type

باشد.

اگر یک Finding در Review قبلی وجود داشته و هنوز معتبر باشد، Comment تکراری نباید ایجاد شود.

---

## 19. Review Summary

علاوه بر Inline Comment، سامانه باید یک Summary برای MR تولید کند.

نمونه:

```text
AI Code Review Summary

Files reviewed: 12
Findings: 7

Critical: 0
High: 1
Medium: 3
Low: 3

Main concerns:
- CancellationToken is not propagated in reporting pipeline.
- One query may cause N+1 behavior.
- Sensitive report parameter is logged.
```

---

## 20. AI / LLM Abstraction

Review Engine نباید مستقیماً به یک Vendor خاص وابسته باشد.

باید از abstraction مبتنی بر:

```text
Microsoft.Extensions.AI
IChatClient
```

استفاده شود.

این امکان باید وجود داشته باشد که بدون تغییر Core Application، Provider عوض شود.

Providerهای احتمالی:

- Azure OpenAI
- OpenAI
- Internal OpenAI-Compatible Endpoint
- Self-Hosted Model
- Future Microsoft-supported Provider

---

## 21. Microsoft Agent Framework

Agent orchestration باید قابلیت استفاده از Microsoft Agent Framework را داشته باشد.

نسخه اولیه بهتر است فقط یک Agent اصلی داشته باشد:

```text
CodeReviewerAgent
```

Agent می‌تواند Toolهایی مانند موارد زیر داشته باشد:

```text
GetMergeRequestDiff
ReadFile
SearchCode
AnalyzeWithRoslyn
GetDefinition
GetReferences
GetProjectRules
BuildProject
RunTests
PublishFinding
```

در MVP نباید Multi-Agent Architecture پیچیده ایجاد شود.

---

## 22. Build and Test Integration

سامانه باید قابلیت توسعه به سمت اجرای:

```text
dotnet restore
dotnet build
dotnet test
```

را داشته باشد.

اما اجرای Build/Test باید یک capability مستقل باشد تا:

- قابل Enable/Disable باشد.
- Timeout داشته باشد.
- Resource Limit داشته باشد.
- Security Boundary مشخص داشته باشد.

در MVP می‌توان Build/Test را Optional در نظر گرفت.

---

## 23. Security Requirements

از آنجا که Source Code سازمان پردازش می‌شود، Security یکی از نیازمندی‌های اصلی است.

### 23.1 Secret Management

موارد زیر نباید در Source Code نگهداری شوند:

- GitLab Token
- LLM API Key
- Jira Token
- Trello Token
- Database Password

Secrets باید از Secret Store یا Environment امن دریافت شوند.

### 23.2 Least Privilege

GitLab Token باید کمترین Permission مورد نیاز را داشته باشد.

### 23.3 Source Code Protection

باید مشخص باشد:

- آیا Source Code از سازمان خارج می‌شود؟
- چه بخشی از Source برای LLM ارسال می‌شود؟
- Provider چه Data Retention Policy دارد؟
- Logها چه اطلاعاتی ذخیره می‌کنند؟

### 23.4 Sensitive File Exclusion

امکان Block کردن فایل‌هایی مانند:

```text
.env
secrets.json
certificates
private keys
production configs
```

باید وجود داشته باشد.

### 23.5 Prompt Injection Protection

محتوای Repository نباید بتواند System Instruction Agent را Override کند.

Code، Comment، README و سایر فایل‌های Repository باید به عنوان Untrusted Input در نظر گرفته شوند.

---

## 24. Audit Requirements

سامانه باید بتواند ثبت کند:

- چه MRی Review شده است.
- چه Commitی Review شده است.
- چه Modelی استفاده شده است.
- چه Rule Setی استفاده شده است.
- Review چه زمانی انجام شده است.
- چه Findingهایی تولید شده‌اند.
- چه Findingهایی Publish شده‌اند.
- چه Findingهایی Filter شده‌اند.

---

## 25. Observability

سامانه باید از ابتدا قابلیت Observability داشته باشد.

حداقل موارد لازم:

- Structured Logging
- Distributed Tracing
- Metrics
- Correlation Id

تکنولوژی پیشنهادی:

```text
OpenTelemetry
ILogger
Activity
```

Metrics مهم:

```text
Review Duration
LLM Duration
Token Usage
Files Reviewed
Findings Generated
Findings Published
Findings Filtered
GitLab API Duration
Failed Reviews
Retry Count
```

---

## 26. Persistence Requirements

در MVP می‌توان بخشی از State را Minimal نگه داشت، اما برای توسعه آینده Persistence لازم خواهد بود.

حداقل Entityهای قابل پیش‌بینی:

```text
Repository
Review
ReviewRun
Finding
PublishedComment
RuleSet
Integration
Feedback
```

Finding History برای جلوگیری از Duplicate Comment و Evaluation آینده مهم است.

---

## 27. Feedback Mechanism

سامانه باید در آینده بتواند Feedback Human Reviewer را ثبت کند.

نمونه Feedback:

```text
Valid
Invalid
False Positive
Useful
Not Useful
Already Known
```

این داده در آینده برای:

- تنظیم Prompt
- تغییر Rules
- کاهش False Positive
- Evaluation
- Model Comparison

قابل استفاده خواهد بود.

---

# 28. اتصال آینده به Jira و Trello

## 28.1 آیا شدنی است؟

بله.

از نظر فنی Integration با Jira و Trello کاملاً امکان‌پذیر است.

Jira دارای REST API برای عملیات مرتبط با Issue و همچنین Webhook است.

Trello نیز REST API برای Board، List، Card و Webhook دارد.

بنابراین AI Code Reviewer می‌تواند در آینده Finding یا نتیجه Review را به Task Management System ارسال کند.

---

## 28.2 کاربردهای Jira Integration

در آینده می‌توان قابلیت‌های زیر را اضافه کرد:

### Create Issue From Finding

برای Findingهای مهم به صورت خودکار یا Manual یک Jira Issue ساخته شود.

مثال:

```text
Critical Security Finding
        |
        v
Create Jira Bug
```

### Link MR to Jira Issue

Review بتواند Issue مرتبط با MR را شناسایی کند.

### Read Task Context

Reviewer بتواند Description و Acceptance Criteria مربوط به Task را دریافت کند.

در این حالت AI فقط Source Code را نمی‌بیند؛ بلکه می‌تواند بررسی کند تغییر انجام‌شده با نیازمندی Task هم‌راستا است یا خیر.

### Add Review Summary to Jira

Summary Review به عنوان Comment روی Issue ثبت شود.

### Update Issue Metadata

در صورت نیاز:

- Label
- Comment
- Custom Field
- Link

به‌روزرسانی شود.

---

## 28.3 کاربردهای Trello Integration

Integration با Trello نیز شدنی است.

قابلیت‌های احتمالی:

- ایجاد Card برای Finding
- اضافه کردن Comment به Card
- اضافه کردن Label
- Link کردن MR به Card
- خواندن Description کارت
- خواندن Checklist
- دریافت Event از Webhook
- ثبت Review Summary

---

## 28.4 Task Context-Aware Review

یکی از قابلیت‌های مهم نسخه‌های آینده می‌تواند این باشد:

```text
Jira / Trello Task
        |
        v
Requirement Context
        |
        +
        |
GitLab Merge Request
        |
        v
AI Code Reviewer
        |
        v
Does the implementation satisfy the task?
```

مثلاً Jira Issue می‌گوید:

```text
Report export must support CancellationToken
and should not load more than 100,000 rows in memory.
```

Reviewer می‌تواند علاوه بر Code Quality، بررسی کند آیا Change این Requirement را رعایت کرده است یا خیر.

این قابلیت از Code Review عمومی ارزش بیشتری دارد، زیرا Review را به Business Requirement متصل می‌کند.

---

## 29. جلوگیری از وابستگی به Jira/Trello

Core Application نباید مستقیماً به Jira یا Trello وابسته شود.

یک abstraction مستقل لازم است.

مفهوم پیشنهادی:

```csharp
public interface IWorkItemProvider
{
    Task<WorkItem?> GetAsync(...);

    Task<WorkItem> CreateAsync(...);

    Task AddCommentAsync(...);

    Task LinkAsync(...);
}
```

Implementationهای آینده:

```text
JiraWorkItemProvider
TrelloWorkItemProvider
AzureBoardsWorkItemProvider
```

این طراحی باعث می‌شود Review Engine هیچ وابستگی مستقیمی به Task Management Vendor نداشته باشد.

---

## 30. Unified Work Item Model

برای جلوگیری از ورود مدل Jira یا Trello به Domain، باید یک مدل داخلی ساده تعریف شود.

نمونه:

```text
WorkItem

Id
ExternalId
Provider
Title
Description
Status
Type
Priority
Url
Labels
AcceptanceCriteria
```

هر Provider اطلاعات سیستم خارجی را به این مدل تبدیل می‌کند.

---

## 31. Integration Architecture

Architecture پیشنهادی در سطح مفهومی:

```text
                       GitLab
                         |
                         v
                SourceControlProvider
                         |
                         v
                +------------------+
                |                  |
                |  Review Engine   |
                |                  |
                +--------+---------+
                         |
             +-----------+------------+
             |                        |
             v                        v
        AI / Roslyn              WorkItemProvider
                                      |
                        +-------------+-------------+
                        |             |             |
                        v             v             v
                      Jira          Trello      Azure Boards
```

این Separation باید از نسخه اول رعایت شود؛ حتی اگر Jira/Trello هنوز Implement نشوند.

---

# 32. Functional Requirements

## FR-001

سامانه باید GitLab Merge Request را دریافت و پردازش کند.

## FR-002

سامانه باید Diff تغییرات MR را دریافت کند.

## FR-003

سامانه باید فایل‌ها را بر اساس Ruleهای Include/Exclude فیلتر کند.

## FR-004

سامانه باید Ruleهای Global و Repository را بارگذاری کند.

## FR-005

سامانه باید برای C# قابلیت Roslyn Analysis داشته باشد.

## FR-006

سامانه باید بتواند Context تکمیلی از Repository دریافت کند.

## FR-007

سامانه باید AI Review تولید کند.

## FR-008

خروجی Review باید Structured باشد.

## FR-009

سامانه باید Severity برای Finding تعیین کند.

## FR-010

سامانه باید Confidence Score داشته باشد.

## FR-011

سامانه باید Findingهای Duplicate را تشخیص دهد.

## FR-012

سامانه باید قابلیت Publish Inline Comment روی GitLab داشته باشد.

## FR-013

سامانه باید Review Summary تولید کند.

## FR-014

سامانه باید Push جدید روی MR را تشخیص دهد و Re-Review انجام دهد.

## FR-015

سامانه باید History اجرای Review را نگهداری کند.

## FR-016

سامانه باید Provider مربوط به LLM را قابل تعویض نگه دارد.

## FR-017

سامانه باید Source Control Integration را از Review Engine جدا کند.

## FR-018

سامانه باید امکان اضافه‌شدن Work Item Provider را بدون تغییر Review Engine داشته باشد.

## FR-019

سامانه باید در آینده بتواند Context مربوط به Jira/Trello Task را وارد Review کند.

## FR-020

سامانه باید در آینده بتواند Finding را به Work Item تبدیل کند.

---

# 33. Non-Functional Requirements

## NFR-001 - Security

Source Code و Secrets باید مطابق Policy سازمان محافظت شوند.

## NFR-002 - Extensibility

اضافه کردن Git Provider، LLM Provider یا Work Item Provider جدید نباید نیازمند تغییر گسترده Core باشد.

## NFR-003 - Reliability

Failure موقت GitLab یا LLM نباید باعث از بین رفتن Review شود.

Retry Policy باید قابل پشتیبانی باشد.

## NFR-004 - Idempotency

پردازش مجدد یک Event نباید Commentهای Duplicate ایجاد کند.

## NFR-005 - Observability

هر Review باید Traceable باشد.

## NFR-006 - Configurability

Thresholdها، Rules و Integrations باید Configurable باشند.

## NFR-007 - Scalability

سیستم باید بتواند چند Repository و چند Review همزمان را پشتیبانی کند.

## NFR-008 - Timeout Control

تمام External Callها باید Timeout داشته باشند.

## NFR-009 - Cancellation

Operationهای Long Running باید CancellationToken را پشتیبانی کنند.

## NFR-010 - Data Minimization

فقط Context مورد نیاز باید برای LLM ارسال شود.

---

# 34. Configuration Requirements

حداقل Configurationهای مورد انتظار:

```text
GitLab
    BaseUrl
    Token
    WebhookSecret

AI
    Provider
    Model
    Endpoint
    ApiKey
    Temperature
    MaxTokens

Review
    MinimumSeverity
    MinimumConfidence
    MaxFiles
    MaxFileSize
    MaxContextSize

Roslyn
    Enabled

Build
    Enabled
    Timeout

Rules
    GlobalRulesPath

Integrations
    Jira.Enabled
    Trello.Enabled
```

---

# 35. Deployment Requirements

سامانه باید قابلیت Deployment در شبکه داخلی سازمان را داشته باشد.

Deployment Targetهای قابل پشتیبانی:

- Windows Server / IIS
- Linux
- Docker
- Kubernetes در صورت نیاز آینده

هیچ وابستگی اجباری به Public Cloud نباید در Core Architecture وجود داشته باشد.

---

# 36. MVP Scope

نسخه MVP بهتر است کوچک نگه داشته شود.

### داخل MVP

```text
GitLab Self-Managed
ASP.NET Core
Microsoft Agent Framework
Microsoft.Extensions.AI
Single CodeReviewerAgent
GitLab Webhook
MR Diff Analysis
C# Review
Basic Roslyn Integration
Repository Rules
Structured Findings
Severity
Confidence
Deduplication
Inline GitLab Comments
Review Summary
Logging / Tracing
```

### خارج از MVP

```text
Jira Integration
Trello Integration
Multi-Agent
Auto Fix
Auto Commit
Auto Merge
Full Repository Indexing
Vector Database
RAG پیچیده
Automatic Task Creation
Advanced Dashboard
```

---

# 37. Phase 2

پس از اعتبارسنجی MVP:

- Feedback Collection
- Better Roslyn Analysis
- Build Integration
- Test Integration
- Review Analytics
- Review Dashboard
- Better Context Retrieval
- Jira Integration
- Trello Integration
- Work Item Context-Aware Review

---

# 38. Phase 3

قابلیت‌های پیشرفته‌تر:

- Auto-Fix Suggestion
- Patch Generation
- Test Generation
- Security Specialized Review
- Repository Knowledge Base
- Cross-Repository Context
- Historical Review Context
- Architecture Drift Detection
- Multi-Agent Review در صورت اثبات نیاز
- Review Quality Evaluation
- Rule Recommendation

---

# 39. ریسک‌های اصلی

## 39.1 False Positive

بزرگ‌ترین ریسک محصول این است که AI Commentهای کم‌ارزش زیادی ایجاد کند.

راهکار محصولی:

- Confidence Threshold
- Severity Threshold
- Deduplication
- Feedback
- Project Rules

---

## 39.2 Excessive Context

ارسال Context زیاد باعث:

- افزایش Cost
- افزایش Latency
- کاهش Signal-to-Noise
- ریسک Security

می‌شود.

Context Retrieval باید Controlled باشد.

---

## 39.3 AI Hallucination

LLM ممکن است Finding اشتباه ایجاد کند.

Finding نباید صرفاً به دلیل تولید توسط مدل معتبر فرض شود.

Roslyn، Rule Validation و Confidence Filtering باید به کاهش این مشکل کمک کنند.

---

## 39.4 Vendor Lock-In

وابستگی مستقیم به یک Model Provider یا Task Management Provider ریسک محسوب می‌شود.

استفاده از abstraction برای:

```text
IChatClient
ISourceControlProvider
IWorkItemProvider
```

ضروری است.

---

## 39.5 Sensitive Source Code

اگر LLM خارج از سازمان باشد، ارسال Source Code باید مطابق Security Policy سازمان باشد.

امکان استفاده از Internal LLM باید جزء Requirements اصلی باقی بماند.

---

# 40. معیار موفقیت MVP

MVP زمانی موفق تلقی می‌شود که:

1. بتواند روی MR واقعی GitLab Review انجام دهد.
2. Comment روی Line صحیح منتشر کند.
3. Findingهای تکراری ایجاد نکند.
4. حداقل چند دسته مهم مشکل را با کیفیت قابل قبول شناسایی کند.
5. Developer بتواند دلیل Finding را بفهمد.
6. False Positive آن‌قدر زیاد نباشد که Developer Review Bot را Ignore کند.
7. اضافه کردن Rule جدید نیازمند تغییر Core Code نباشد.
8. تغییر LLM Provider نیازمند بازنویسی Review Engine نباشد.
9. Architecture امکان اضافه کردن Jira/Trello را بدون بازطراحی Core فراهم کند.

---

# 41. نتیجه‌گیری

این محصول از نظر فنی قابل پیاده‌سازی است و Microsoft Stack ابزارهای اصلی مورد نیاز آن را فراهم می‌کند.

هسته پیشنهادی محصول بر پایه این اجزا خواهد بود:

```text
ASP.NET Core
Microsoft Agent Framework
Microsoft.Extensions.AI
Roslyn
GitLab REST API
OpenTelemetry
```

Jira و Trello نباید در MVP پیاده‌سازی شوند، اما **قابلیت اتصال به Task Management System باید از ابتدا در Architecture لحاظ شود**.

وجود یک abstraction مانند:

```text
IWorkItemProvider
```

باعث می‌شود در آینده بتوان Providerهای زیر را بدون تغییر اساسی Review Engine اضافه کرد:

```text
Jira
Trello
Azure Boards
Other Task Management Systems
```

قابلیت مهم‌تر در آینده فقط ساخت Task از Finding نیست؛ بلکه استفاده از **Task Context و Acceptance Criteria به عنوان Context برای Code Review** است.

در این حالت Reviewer می‌تواند علاوه بر کیفیت کد، بررسی کند که Implementation تا چه حد با Requirement مربوط به Task هم‌راستا است.

این قابلیت می‌تواند AI Code Reviewer را از یک ابزار عمومی Code Quality به یک Reviewer آگاه از Context واقعی پروژه تبدیل کند.

---

# 42. منابع رسمی بررسی‌شده

برای بررسی امکان Integration آینده، مستندات رسمی زیر مبنا قرار گرفته‌اند:

- Atlassian Jira Cloud REST API v3
- Atlassian Jira Cloud Webhooks API
- Atlassian Jira Data Center REST API
- Atlassian Trello REST API
- Atlassian Trello Webhooks API

بر اساس این مستندات، Jira و Trello هر دو APIهای لازم برای Integration برنامه‌نویسی و Event/Webhook-based Integration را فراهم می‌کنند.
