namespace CodeReviewer.Web.Services;

public sealed class ReviewProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "بازبینی متعادل";
    public Guid? ModelId { get; set; }
    public string Strictness { get; set; } = "Balanced";
    public string Prompt { get; set; } = "یافته‌ها و پیشنهادها را به فارسی بنویسید. روی مشکلات عملی تمرکز کنید.";
    public string MinimumSeverity { get; set; } = "medium";
    public string BlockingSeverity { get; set; } = "medium";
    public double MinimumConfidence { get; set; } = 0.8;
    // Retained for historical JSON compatibility. The execution engine reviews all available text files.
    public bool ReviewDocumentation {get;set;}
    public bool RouteDomains {get;set;} = true;
    public int MaxFiles { get; set; } = 0;
    public List<ReviewDomain> Domains { get; set; } = [new() {Key="correctness"},new() {Key="security"}];
    public string BusinessRules { get; set; } = "";
    public int ConcurrentFiles { get; set; } = 2;
    public int MaxDiffCharacters { get; set; } = 12000;
    public int TimeoutSeconds { get; set; } = 7200;
    public int ModelTimeoutSeconds { get; set; } = 120;
    public int MaxOutputTokens { get; set; } = 4096;
    public void Validate()
    {
        if(SeverityRank(BlockingSeverity)<0) throw new ArgumentException("حداقل شدت نیازمند اصلاح معتبر نیست.");
        if(ConcurrentFiles is < 1 or > 4) throw new ArgumentException("تعداد فایل هم‌زمان باید ۱ تا ۴ باشد.");
        if(Domains is null || Domains.Count==0 || Domains.Any(d=>!ReviewDomain.Available.ContainsKey(d.Key) || d.Instructions.Length>8000) || Domains.Select(d=>d.Key).Distinct().Count()!=Domains.Count) throw new ArgumentException("حداقل یک حوزهٔ معتبر و بدون تکرار انتخاب کنید؛ دستور هر حوزه حداکثر ۸۰۰۰ کاراکتر است.");
        if(BusinessRules.Length>30000) throw new ArgumentException("قواعد کسب‌وکار حداکثر ۳۰ هزار کاراکتر است.");
        if(Domains.Any(d=>d.Key=="business") && string.IsNullOrWhiteSpace(BusinessRules)) throw new ArgumentException("برای بررسی کسب‌وکار، قواعد و معیارهای پذیرش را وارد کنید؛ مدل نباید قواعد را حدس بزند.");
        if(ModelTimeoutSeconds is < 5 or > 600) throw new ArgumentException("مهلت پاسخ مدل باید ۵ تا ۶۰۰ ثانیه باشد.");
        if(MaxOutputTokens is < 256 or > 8192) throw new ArgumentException("سقف توکن خروجی باید ۲۵۶ تا ۸۱۹۲ باشد.");
        if(string.IsNullOrWhiteSpace(Name) || Name.Length > 100 || Prompt.Length > 8000) throw new ArgumentException("نام تا ۱۰۰ و پرامپت تا ۸۰۰۰ کاراکتر مجاز است.");
        if(Strictness is not ("Light" or "Balanced" or "Strict")) throw new ArgumentException("میزان سخت‌گیری معتبر نیست.");
        if(SeverityRank(MinimumSeverity) < 0 || !double.IsFinite(MinimumConfidence) || MinimumConfidence is < 0.5 or > 1) throw new ArgumentException("شدت یا حداقل اطمینان معتبر نیست.");
        if(MaxDiffCharacters is < 1000 or > 50000 || TimeoutSeconds is < 30 or > 86400) throw new ArgumentException("اندازهٔ هر بخش ۱۰۰۰ تا ۵۰۰۰۰ کاراکتر و مهلت کل ۳۰ تا ۸۶۴۰۰ ثانیه است.");
    }
    public static int SeverityRank(string severity) => severity switch { "info" => 0, "low" => 1, "medium" => 2, "high" => 3, "critical" => 4, _ => -1 };
    public bool Accepts(ReviewFinding finding) => double.IsFinite(finding.Confidence) && finding.Confidence >= MinimumConfidence && finding.Confidence <= 1 && SeverityRank(finding.Severity) >= SeverityRank(MinimumSeverity);
    public string Instructions => Strictness switch
    {
        "Light" => "Be pragmatic and light. Report only clear defects with practical impact. Skip style, cosmetic changes, speculative risks and minor refactors.",
        "Strict" => "Examine edge cases, error handling, security, performance and maintainability carefully. Remain evidence-based; do not invent defects to fill a quota.",
        _ => "Balance correctness, security and performance. Prefer actionable defects over speculative or cosmetic suggestions."
    };
    public static ReviewProfile[] Presets() => [
        new() { Id=Guid.Parse("b1000000-0000-0000-0000-000000000001"), Name="سریع و سبک", Strictness="Light", MinimumSeverity="high", MinimumConfidence=0.9, MaxDiffCharacters=6000 },
        new() { Id=Guid.Parse("b1000000-0000-0000-0000-000000000002") },
        new() { Id=Guid.Parse("b1000000-0000-0000-0000-000000000003"), Name="دقیق و سخت‌گیرانه", Strictness="Strict", MinimumSeverity="low", MaxDiffCharacters=24000 }
    ];
}

