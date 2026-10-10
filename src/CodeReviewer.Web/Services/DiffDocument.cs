using System.Text.RegularExpressions;
namespace CodeReviewer.Web.Services;

public sealed record DiffLine(int? OldLine,int? NewLine,string Text,string Kind);
public static class DiffDocument
{
    public static IReadOnlyList<DiffLine> Parse(string diff)
    {
        var result=new List<DiffLine>(); var oldLine=0; var newLine=0; var inHunk=false;
        foreach(var raw in diff.Split('\n'))
        {
            var text=raw.TrimEnd('\r');
            var match=Regex.Match(text,@"^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@");
            if(match.Success) { oldLine=int.Parse(match.Groups[1].Value); newLine=int.Parse(match.Groups[2].Value); inHunk=true; result.Add(new(null,null,text,"hunk")); }
            else if(inHunk && text.StartsWith('+')) result.Add(new(null,newLine++,text[1..],"added"));
            else if(inHunk && text.StartsWith('-')) result.Add(new(oldLine++,null,text[1..],"removed"));
            else if(inHunk && text.StartsWith(' ')) result.Add(new(oldLine++,newLine++,text[1..],"context"));
            else if(text.Length>0) result.Add(new(null,null,text,"meta"));
        }
        return result;
    }
}
