namespace CodeReviewer.Web.Services;

public sealed class ReviewDomain
{
    public string Key { get; set; } = "correctness";
    public Guid? ModelId { get; set; }
    public string Instructions { get; set; } = "";
    public static readonly IReadOnlyDictionary<string,string> Available = new Dictionary<string,string> {
        ["compatibility"]="سازگاری قراردادها", ["security"]="امنیت", ["business"]="منطق کسب‌وکار", ["correctness"]="صحت عملکرد",
        ["performance"]="کارایی", ["database"]="پایگاه داده", ["async"]="هم‌زمانی و Async",
        ["error-handling"]="مدیریت خطا", ["maintainability"]="نگهداشت‌پذیری", ["testing"]="تست‌ها و پوشش آزمون", ["accessibility"]="دسترس‌پذیری رابط کاربری"
    };
}

