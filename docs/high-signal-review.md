# گزارش پیاده‌سازی بازبینی کم‌نویز

## معماری موجود و نقاط تغییر

برنامه یک Blazor Server/.NET 10 با سرویس‌های application در همان پروژه است. `ISourceControlProvider` دریافت مخزن، MR/PR و Diff را از GitLab/GitHub جدا می‌کند. `ReviewJobQueue` کار و snapshot پروفایل را در SQL نگه می‌دارد؛ Worker، `AiReviewService` را اجرا می‌کند. Diff بخش‌بندی و حوزه‌های دارای مدل یکسان گروه‌بندی می‌شوند. MEAI `IChatClient` برای OpenAI-compatible/Azure/Ollama و deadline/retry موجود حفظ شد. `SqlDataStore` نتایج، snapshotها و رویدادها را ذخیره می‌کند؛ `ReviewPublicationService` و GitLabProvider انتشار خطی و Check را انجام می‌دهند. CQRS، Roslyn یا call graph در نسخهٔ موجود وجود نداشت.

مشکلات: همهٔ فایل‌های متنی از جمله Markdown به مدل می‌رفتند؛ انتخاب حوزه به فایل وابسته نبود؛ فیلتر بیشتر خط/شدت/اطمینان را کنترل می‌کرد؛ LLM از محصول اطلاعی نداشت؛ روایت تغییر و تحلیل داخلی و شمار پیشنهادهای ردشده در خروجی نهایی بودند؛ اثر تغییر و تست دستی مدل مستقل نداشتند.

## مدل‌ها و سرویس‌های جدید

- `ProjectContext` و `ProjectFeature`: اطلاعات مختصر محصول، معماری، فناوری، مرزها، نگاشت prefix مسیر به قابلیت، UI و روابط قابلیت‌ها.
- `ProjectContextStore`: استفاده از همان SourceSnapshots SQL با کلید اتصال + مخزن؛ زیرساخت ذخیره‌سازی جدا ساخته نشد.
- `ReviewScope`: حذف مستندات پیش‌فرض، فایل تولیدشده/وابستگی و تغییر کاملاً قابل اثباتِ فاصلهٔ ابتدا/انتهای خط C#؛ مسیریابی محافظه‌کارانهٔ حوزه‌ها.
- `FindingQuality`: اطمینان و شدت و خط قبلی، به‌علاوه نقل دقیق از خط افزوده، اثر واقعی، دلیل مرتبط‌بودن با تغییر، actionable بودن و رد توصیهٔ عمومی علامت‌خورده.
- `ChangeImpactAnalyzer` و `ChangeImpactResult`: اثر مستقیم، اثر احتمالی، تست‌های دستی و ریسک مستقل از تعداد ایراد.
- `FocusedReviewOutput`: خروجی کم‌حجم یافته‌ها و سناریوهای مستند؛ دیگر از مدل روایت و reasoning فایل درخواست نمی‌شود.
- `RegressionScenario`: اقدام، انتظار، فایل/خط شاهد و UI path معلوم.

## تأمین Context

صفحهٔ `/project-context` اتصال و مخزن را از سرویس موجود می‌گیرد و نقشهٔ JSON قابل تنظیم را در SQL ذخیره می‌کند. نام و قابلیت‌های DMP در منطق عمومی hardcode نشده‌اند. نقشه ۶۰ قابلیت/۲۴ هزار کاراکتر سقف دارد تا context مختصر بماند. آغاز هر اجرا snapshot همان نقشه ثبت می‌شود. تغییر نقشهٔ بعدی گزارش قبلی را عوض نمی‌کند. تولید خودکار نقشهٔ کامل از repository در این نسخه انجام نمی‌شود.

برای هر قابلیت PathPrefixes دقیق مثل `src/Orders/`، نام قابل فهم برای کاربر، UiPath واقعی، رابطه‌ها و در صورت نیاز سناریوهای Steps/Expected ثبت می‌شود. دادهٔ مخزن مرجع است و در prompt به‌عنوان دستور اجرا معرفی نمی‌شود. فقط قابلیت‌های مرتبط با فایل به مدل فرستاده می‌شوند.

## تحلیل اثر و تست دستی

اثر مستقیم از تغییر فایل در مسیر ثبت‌شدهٔ یک قابلیت به دست می‌آید. اثر احتمالی از RelatedFeatures ثبت‌شده به دست می‌آید و تأیید مسیر فراخوانی ادعا نمی‌شود. برای مسیر حساس ثبت‌شده Risk=High، تغییر اجرایی دیگر Medium و نبود تغییر اجرایی مرتبط Low تعیین می‌شود؛ این یک برآورد قاعده‌محور است. بدون نگاشت، گزارش صریحاً ناقص‌بودن شناخت قابلیت‌ها را اعلام می‌کند.

سناریوهای تنظیم‌شده فقط برای قابلیت‌های تحت تأثیر فعال می‌شوند. مدل می‌تواند حداکثر سه سناریو در پاسخ هر بخش پیشنهاد کند؛ پذیرش نیازمند نقل واقعی از خط افزوده، اقدام و انتظار مشخص است. UI path حدسی رد می‌شود؛ نبود UI معلوم، سناریوی سیستمی ایجاد می‌کند. سناریوهای تکراری یکسان حذف می‌شوند. گزارش اصلی حداکثر هشت سناریو دارد؛ بقیه در صفحهٔ اختصاصی تست‌ها هستند. هیچ سناریویی اجراشده تلقی نمی‌شود. فایل‌های تست تغییرکرده قابل مشاهده‌اند؛ کشف همهٔ تست‌های خارج از Diff هنوز انجام نمی‌شود.

## خروجی و مصرف توکن

پنل و کامنت GitLab از formatter مشترک استفاده می‌کنند: نتیجه، ایرادهای مهم، اثر مستقیم/احتمالی، تست دستی و آمادگی انتشار. روایت فایل‌به‌فایل، خطوط افزوده/حذف‌شده، rejected findings، reasoning، token و prompt در گزارش عادی نیستند. SQL coverage و ReviewEvents برای عیب‌یابی حفظ شدند.

مستندات به‌طور پیش‌فرض تحلیل زمان اجرا نمی‌شوند؛ گزینهٔ ReviewDocumentation تحلیل مستقل آن‌ها را فعال می‌کند. حوزه‌های database/performance/async/accessibility/testing بر اساس نشانه‌های مرتبط مسیریابی می‌شوند. security/correctness و حوزه‌های با دستور اختصاصی محافظه‌کارانه حفظ می‌شوند تا فقدان یک keyword به حذف بررسی حساس منجر نشود. RouteDomains قابل غیرفعال‌سازی است و پروفایل‌ها حذف نشده‌اند. گروه‌بندی مدل مشترک، context اختصاصی فایل و حذف narration، هزینهٔ ورودی/خروجی را کاهش می‌دهند. هر درخواست مدل stateless است؛ context لازم در هر بخش تکرار می‌شود و prompt-cache بین سرویس‌های خارجی ادعا نشده است.

## سازگاری و محدودیت‌ها

ستون nullable Details به Findings افزوده شد تا شواهد به همراه نتیجه حفظ شوند؛ دادهٔ قدیمی حذف نشد. یافته‌های قدیمی فاقد شواهد ساختاریافته در گزارش/انتشار جدید وارد نمی‌شوند و برای خروجی جدید بازبینی تازه لازم است. source-control adapters و LLM transports حفظ شدند.

وجود quote واقعی به‌تنهایی درستی معنایی ایراد را تضمین نمی‌کند؛ داوری انسانی همچنان لازم است. تحلیل caller/callee کل مخزن، Roslyn semantic graph، ارزیابی رفتار برنامه و اجرای تست پیاده نشده‌اند. اثر مستقیم این نسخه حاصل نگاشت مسیر به قابلیت است، نه اثبات همهٔ مسیرهای اجرا. هیچ سرویس، graph/vector DB یا dependency بزرگ جدید اضافه نشد. MR واقعی قبلی بسته است و انتشار مجدد آن در این کار انجام نشد.

## فایل‌های تغییرکرده

همهٔ مسیرها نسبت به ریشهٔ پروژه‌اند:

- `src/CodeReviewer.Web/Services/ProjectContext.cs` (جدید)
- `src/CodeReviewer.Web/Services/ReviewScope.cs` (جدید)
- `src/CodeReviewer.Web/Services/ChangeImpactAnalyzer.cs` (جدید)
- `src/CodeReviewer.Web/Services/FindingQuality.cs` (جدید)
- `src/CodeReviewer.Web/Services/AiReviewService.cs`
- `src/CodeReviewer.Web/Services/ReviewCoverage.cs`
- `src/CodeReviewer.Web/Services/ReviewProfile.cs`
- `src/CodeReviewer.Web/Services/ReviewDomain.cs`
- `src/CodeReviewer.Web/Services/SqlDataStore.cs`
- `src/CodeReviewer.Web/Services/ReviewCommentFormatter.cs`
- `src/CodeReviewer.Web/Services/ReviewDecision.cs`
- `src/CodeReviewer.Web/Services/GitLabPublication.cs`
- `src/CodeReviewer.Web/Program.cs`
- `src/CodeReviewer.Web/Components/ReviewSummary.razor` (جدید)
- `src/CodeReviewer.Web/Components/Pages/ProjectContextPage.razor` (جدید)
- `src/CodeReviewer.Web/Components/Pages/RegressionTests.razor` (جدید)
- `src/CodeReviewer.Web/Components/Pages/ReviewReport.razor`
- `src/CodeReviewer.Web/Components/Pages/Profiles.razor`
- `src/CodeReviewer.Web/Components/Layout/AdminLayout.razor`
- `tests/CodeReviewer.Web.Tests/HighSignalReviewTests.cs` (جدید)
- `tests/CodeReviewer.Web.Tests/ReviewCommentTests.cs`
- `tests/CodeReviewer.Web.Tests/PublicationTests.cs`
- `tests/CodeReviewer.Web.Tests/ConnectionCatalogTests.cs`
- `docs/high-signal-review.md` (این گزارش)

## آزمون‌ها

۴۶ آزمون موفق: تطبیق شاهد/خط/اطمینان، رد advice علامت‌خورده، رد UI path جعلی، نگاشت مستقیم و احتمالی، ریسک مستقل از ایراد، جداکردن diagnostics از report، scope و routing، formatting بدون حذف تغییر literal، سازگاری انتشار و تست انتهابه‌انتها با SQL واقعی و مدل شبیه‌سازی‌شده. تست انتهابه‌انتها ثابت کرد فقط یک فایل اجرایی از سه فایل به LLM می‌رود، صفر ایراد با ریسک High و تست دستی ذخیره می‌شود و snapshot نقشه پس از ویرایش تنظیمات ثابت می‌ماند.

## اجرای نسخه و بررسی صفحات

نسخهٔ جدید روی پورت ۵۰۷۷ اجرا شد. نقشهٔ مخزن، پروفایل، گزارش و CSS با HTTP 200 بررسی شدند؛ بخش روایت داخلی در HTML گزارش نیست. ابزار مرورگر با خطای محیطی راه‌اندازی نشد؛ بررسی بصری/کلیک فرم‌ها انجام نشده است. فایل README نیز برای سیاست گزارش و scope جدید به‌روز شد. متن context برای LLM به‌صورت Unicode فشرده ارسال می‌شود. metadata شاهد یافته‌ها در SQL round-trip آزمون می‌شود.
