using System.Text;
namespace CodeReviewer.Web.Services;

public sealed record DiffChunk(string Text, HashSet<int> AddedLines);
public static class DiffChunker
{
    public static IReadOnlyList<DiffChunk> Split(string diff,int size)
    {
        if(size<1000) throw new ArgumentOutOfRangeException(nameof(size));
        var chunks=new List<DiffChunk>(); var buffer=new StringBuilder(); var lines=new HashSet<int>();
        void Flush() { if(buffer.Length==0) return; chunks.Add(new(buffer.ToString(),new(lines))); buffer.Clear(); lines.Clear(); }
        foreach(var row in DiffDocument.Parse(diff))
        {
            if(row.Kind is "hunk" or "meta") continue;
            var prefix=$"[{row.Kind} old:{row.OldLine?.ToString() ?? "-"} new:{row.NewLine?.ToString() ?? "-"}] ";
            var width=size-prefix.Length-2;
            for(var offset=0; offset<Math.Max(1,row.Text.Length);offset+=width)
            {
                var part=row.Text.Substring(Math.Min(offset,row.Text.Length),Math.Min(width,row.Text.Length-offset));
                if(buffer.Length+prefix.Length+part.Length+1>size) Flush();
                buffer.Append(prefix).Append(part).Append('\n');
                if(row.Kind=="added" && row.NewLine is int number) lines.Add(number);
            }
        }
        Flush(); return chunks;
    }
}
