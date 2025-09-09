using Amazon.Textract.Model;
using Amazon.Textract;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace FinoBackend.Services.ReceiptConverter;

public static class ReceiptCsvHelper
{
    // --- Keyword sets ---
    private static readonly HashSet<string> EnglishKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "receipt", "invoice", "bill",
        "date", "amount", "total", "price"
    };

    private static readonly HashSet<string> VietnameseKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "nguoi gui", "nguoi nhan", "dia chi", "so dien thoai", "ma van don",
        "tien thu ho", "tong cong", "thanh tien", "don gia", "so luong", "stt"
    };

    public static Stream BuildCsvFromBlocks(IReadOnlyList<Block> allBlocks)
    {
        var blockMap = allBlocks.ToDictionary(b => b.Id!, b => b);
        var tables = allBlocks.Where(b => b.BlockType == BlockType.TABLE).ToList();

        var sb = new StringBuilder();
        bool foundFirstHeader = false;

        foreach (var table in tables)
        {
            var cellIds = table.Relationships?
                .Where(r => r.Type == RelationshipType.CHILD)
                .SelectMany(r => r.Ids)
                .ToHashSet() ?? new HashSet<string>();

            var cells = allBlocks
                .Where(b => b.BlockType == BlockType.CELL && cellIds.Contains(b.Id));

            var allRows = cells
                .GroupBy(c => c.RowIndex)
                .OrderBy(g => g.Key)
                .Select(g => g.OrderBy(c => c.ColumnIndex)
                              .Select(c => GetCellText(c, blockMap))
                              .ToList())
                .ToList();

            if (allRows.Count == 0) continue;

            // --- Detect the first header row ---
            if (!foundFirstHeader)
            {
                int headerIndex = allRows.FindIndex(LooksLikeHeader);
                if (headerIndex < 0) continue; // skip until we find a valid header

                foundFirstHeader = true;
                foreach (var row in allRows.Skip(headerIndex))
                {
                    if (row.All(string.IsNullOrWhiteSpace)) continue;
                    sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
                }
            }
            else
            {
                foreach (var row in allRows)
                {
                    if (row.All(string.IsNullOrWhiteSpace)) continue;
                    sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
                }
            }
        }

        return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    // 🔑 Header detection
    private static bool LooksLikeHeader(IReadOnlyList<string> row)
    {
        var normalized = Normalize(string.Join(' ', row));

        return EnglishKeywords.Any(k => normalized.Contains(k)) ||
               VietnameseKeywords.Any(k => normalized.Contains(k));
    }

    private static string RowKey(IReadOnlyList<string> row) =>
        string.Join("|", row.Select(s => (s ?? string.Empty).Trim().ToLowerInvariant()));

    private static string GetCellText(Block cell, IDictionary<string, Block> blockMap)
    {
        if (cell.Relationships == null || cell.Relationships.Count == 0)
            return string.Empty;

        var parts = new List<string>();
        foreach (var rel in cell.Relationships)
        {
            if (rel.Type != RelationshipType.CHILD || rel.Ids == null) continue;
            foreach (var id in rel.Ids)
            {
                if (!blockMap.TryGetValue(id, out var child)) continue;
                if (child.BlockType == BlockType.WORD && !string.IsNullOrEmpty(child.Text))
                {
                    parts.Add(child.Text);
                }
                else if (child.BlockType == BlockType.SELECTION_ELEMENT &&
                         child.SelectionStatus == SelectionStatus.SELECTED)
                {
                    parts.Add("☑");
                }
            }
        }
        return Regex.Replace(string.Join(" ", parts), @"\s+", " ").Trim();
    }

    private static string CsvEscape(string v)
    {
        if (string.IsNullOrEmpty(v)) return string.Empty;
        return v.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? $"\"{v.Replace("\"", "\"\"")}\""
            : v;
    }

    // 🔹 Normalize string (remove accents, lowercase, trim)
    private static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var ch in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        return sb.ToString()
                 .Normalize(NormalizationForm.FormC)
                 .ToLowerInvariant()
                 .Trim();
    }
}
