using HtmlAgilityPack;

namespace App.Scraper.Utils;

public static class HtmlTextExtractor
{
    private static readonly string[] SkipTags = ["script", "style", "nav", "footer", "head"];

    public static string Extract(HtmlDocument doc)
    {
        var root = doc.DocumentNode.SelectSingleNode("//main") ?? doc.DocumentNode;
        var sb = new System.Text.StringBuilder();
        WalkNode(root, sb);
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s{2,}", " ").Trim();
    }

    private static void WalkNode(HtmlNode node, System.Text.StringBuilder sb)
    {
        if (SkipTags.Contains(node.Name.ToLower())) return;

        if (node.NodeType == HtmlNodeType.Text)
        {
            var text = HtmlEntity.DeEntitize(node.InnerText).Trim();
            if (!string.IsNullOrWhiteSpace(text))
                sb.Append(text).Append(' ');
            return;
        }

        foreach (var child in node.ChildNodes)
            WalkNode(child, sb);
    }
}
