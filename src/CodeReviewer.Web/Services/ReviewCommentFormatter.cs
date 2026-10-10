using System.Net;
using System.Text;
namespace CodeReviewer.Web.Services;
public static class ReviewCommentFormatter
{
    public static IReadOnlyList<ReviewFinding> Important(ReviewRun run)=>run.Findings.Where(FindingQuality.HasEvidence).ToArray();
    public static bool HasUsefulContent(ReviewRun run)=>run.Impact is not null || Important(run).Count>0;
    public static string Quote(string text)=>string.Join("\n",WebUtility.HtmlEncode(text).Replace("@","&#64;").Replace("\r","").Split('\n').Select(l=>"> "+l));
    public static string Risk(string? value)=>value switch {"High"=>"زیاد","Medium"=>"متوسط","Low"=>"کم",_=>"ارزیابی نشده"};
    public static string Build(ReviewRun run)
    {
        if(run.Status=="Completed" && !HasUsefulContent(run)) throw new InvalidOperationException("این گزارش قدیمی شواهد ساختاریافته و تحلیل اثر ندارد؛ بازبینی تازه لازم است.");
        var findings=Important(run);
        var b=new StringBuilder("# خلاصهٔ بازبینی کد\n\n## نتیجهٔ بازبینی\n\n");
        if(run.Status!="Completed") b.AppendLine(run.Status=="Running" ? "بررسی در حال انجام است؛ نتیجه موقت است.\n" : "بررسی ناقص است؛ تکمیل بررسی لازم است.\n");
        b.AppendLine(findings.Count==0 ? "در بخش بررسی‌شده ایراد مهمی شناسایی نشد." : $"{findings.Count} ایراد قابل اقدام شناسایی شد.");
        b.AppendLine($"\n- بحرانی/زیاد: {findings.Count(f=>f.Severity is "critical" or "high")}\n- متوسط: {findings.Count(f=>f.Severity=="medium")}\n- کم/اطلاع: {findings.Count(f=>f.Severity is "low" or "info")}\n- ریسک رگرسیون: {Risk(run.Impact?.Risk)}");
        b.AppendLine("- تست دستی UI: "+(run.Impact is null ? "نیازمند تعیین" : run.Impact.ManualTests.Any(t=>!string.IsNullOrWhiteSpace(t.UiPath)) ? "بله" : "سناریوی UI تأییدشده در دسترس نیست"));
        if(findings.Count>0) {
            b.AppendLine("\n## ایرادهای مهم");
            foreach(var f in findings) b.AppendLine("\n"+Quote($"{f.Title} [{f.Severity}] — {f.File}:{f.Line}\n{f.Description}\nاثر: {f.Impact}\nاصلاح: {f.Suggestion}"));
        }
        b.AppendLine("\n## تحلیل اثر");
        if(run.Impact is { } impact) {
            foreach(var f in impact.Direct) b.AppendLine("\nاثر مستقیم: "+Quote(f.Feature));
            foreach(var f in impact.Possible) b.AppendLine("\nاثر احتمالی: "+Quote(f.Feature));
            if(!impact.ContextConfigured || impact.UnmappedFiles>0) b.AppendLine("\nنگاشت قابلیت‌ها کامل نیست؛ اثر تغییرات خارج از نقشهٔ مخزن باید بررسی شود.");
            if(impact.Direct.Count==0 && impact.ContextConfigured) b.AppendLine("\nقابلیت منطبق با نقشه شناسایی نشد.");
            b.AppendLine("\n## تست دستی رگرسیون");
            foreach(var test in impact.ManualTests.Take(8)) b.AppendLine("\n- [ ] "+WebUtility.HtmlEncode($"{test.UiPath} {test.Steps} — انتظار: {test.Expected}").Replace("@","&#64;").Replace("\n"," ").Replace("\r"," "));
            if(impact.ManualTests.Count>8) b.AppendLine("\nسناریوهای تکمیلی در گزارش سامانه در دسترس‌اند.");
            if(impact.ManualTests.Count==0) b.AppendLine("\nسناریوی مشخص با شاهد کافی تولید نشد؛ مسیرهای محصول را در نقشهٔ مخزن تکمیل کنید.");
            if(impact.RelatedTests.Count>0) b.AppendLine($"\n{impact.RelatedTests.Count} فایل تست در تغییرات دیده شد؛ اجرای آن‌ها انجام نشده است.");
        } else b.AppendLine("\nاین اجرای قدیمی تحلیل اثر ساختاریافته ندارد؛ بازبینی تازه لازم است.");
        b.AppendLine("\n## آمادگی انتشار");
        var blockers=findings.Count(f=>ReviewProfile.SeverityRank(f.Severity)>=ReviewProfile.SeverityRank(run.Profile?.BlockingSeverity ?? "medium"));
        b.AppendLine(blockers>0 ? $"\n{blockers} ایراد مسدودکننده باید پیش از انتشار رفع شود." : "\nایراد مسدودکننده‌ای در بخش بررسی‌شده شناسایی نشد.");
        if(run.Status!="Completed") b.AppendLine("تکمیل بازبینی لازم است.");
        b.AppendLine("سناریوهای پیشنهادی اجرا نشده‌اند؛ نتیجه تضمین آمادگی Production نیست.");
        return b.ToString();
    }
}

