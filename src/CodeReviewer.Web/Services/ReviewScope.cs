using System.Text.RegularExpressions;

namespace CodeReviewer.Web.Services;

public static class ReviewScope
{
    public static bool Documentation(string path)=>Regex.IsMatch(path,@"(^|/)(docs?|specifications?)/|\.(md|mdx|rst|adoc|txt)$",RegexOptions.IgnoreCase);
    public static string? SkipReason(ChangedFile file,ReviewProfile profile)
    {
        if(file.Generated || Regex.IsMatch(file.Path,@"(^|/)(bin|obj|node_modules|vendor)/|\.(g|generated|designer)\.cs$|\.min\.(js|css)$|(^|/)(package-lock\.json|yarn\.lock|packages\.lock\.json)$",RegexOptions.IgnoreCase)) return "فایل تولیدشده یا وابستگی";
        if(Documentation(file.Path) && !profile.ReviewDocumentation) return "مستندات؛ خارج از تحلیل زمان اجرا";
        // Only ignore changes proven identical after edge-whitespace normalization.
        // Do not collapse interior whitespace, string literals, or indentation-sensitive languages.
        if(file.Path.EndsWith(".cs",StringComparison.OrdinalIgnoreCase) && !file.Diff.Contains("\"\"\"") && !file.Diff.Contains("@\""))
        {
            var rows=DiffDocument.Parse(file.Diff);
            var added=rows.Where(r=>r.Kind=="added").Select(r=>r.Text.Trim()).ToArray();
            var removed=rows.Where(r=>r.Kind=="removed").Select(r=>r.Text.Trim()).ToArray();
            if(added.Length>0 && added.SequenceEqual(removed) && added.All(l=>!l.Contains('"'))) return "فقط فاصلهٔ ابتدا یا انتهای خط";
        }
        return null;
    }
    public static IReadOnlyList<ReviewDomain> Route(ChangedFile file,ReviewProfile profile)
    {
        if(!profile.RouteDomains) return profile.Domains;
        var text=file.Path+"\n"+file.Diff;
        bool Has(string pattern)=>Regex.IsMatch(text,pattern,RegexOptions.IgnoreCase);
        return profile.Domains.Where(d=>!string.IsNullOrWhiteSpace(d.Instructions) || d.Key switch {
            "performance"=>Has(@"query|select|foreach|for\s*\(|cache|stream|serializ|async|file|http"),
            "database"=>Has(@"sql|query|DbContext|migration|repository|entity|Dapper"),
            "accessibility"=>Has(@"\.razor|\.tsx|\.jsx|\.html|\.vue|aria-|<input|<button"),
            "testing"=>Has(@"test|spec|assert|fixture"),
            "async"=>Has(@"async|await|Task|Thread|lock\s*\(|Semaphore|Cancellation"),
            // Security/correctness remain conservative: a keyword absence is not proof of irrelevance.
            _=>true
        }).ToArray();
    }
}
