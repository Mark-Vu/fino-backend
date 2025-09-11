using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using Amazon.Textract;
using Amazon.Textract.Model;
using Tabula;

namespace FinoBackend.Services.BankStatementConverter;

public static class BankStatementCsvHelper
{
    // === Header detection ===
    public static bool LooksLikeHeader(IReadOnlyList<string> row)
    {
        var j = string.Join(' ', row).ToLowerInvariant();

        bool hasDate = j.Contains("date") || j.Contains("ngay");
        bool hasTxn = j.Contains("transaction") || j.Contains("description") ||
                      j.Contains("details") || j.Contains("narration") ||
                      j.Contains("particulars") || j.Contains("dien giai") ||
                      j.Contains("noi dung");

        bool hasMoney = j.Contains("withdrawal") || j.Contains("deposit") ||
                        j.Contains("credit") || j.Contains("debit") ||
                        j.Contains("balance") || j.Contains("so tien") ||
                        j.Contains("so du");

        // Require at least date + transaction, OR transaction + money
        return (hasDate && hasTxn) || (hasTxn && hasMoney);
    }

    public static string CsvEscape(string v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        return v.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? $"\"{v.Replace("\"", "\"\"")}\""
            : v;
    }

    public static string NormalizeForKey(string s) =>
        Regex.Replace((s ?? "").ToLowerInvariant().Trim(), @"\s+", " ");

    public static string HeaderKey(IReadOnlyList<string> r) =>
        string.Join("|", r.Select(NormalizeForKey));

    public static bool IsBlankRow(IReadOnlyList<string> row) =>
        row.All(c => string.IsNullOrWhiteSpace(c));

    public static List<string> CleanRow(List<string> row)
    {
        // normalize "blank" → ""
        var cleaned = row.Select(cell =>
        {
            var text = (cell ?? "").Trim();
            return string.Equals(text, "blank", StringComparison.OrdinalIgnoreCase) ? "" : text;
        }).ToList();

        // trim trailing empty cells
        int lastNonEmpty = cleaned.FindLastIndex(c => !string.IsNullOrWhiteSpace(c));
        return lastNonEmpty >= 0 ? cleaned.Take(lastNonEmpty + 1).ToList() : new List<string>();
    }

    // === Textract CSV builder ===
    public static Stream BuildCsvFromBlocks(IReadOnlyList<Block> allBlocks)
    {
        var blockMap = allBlocks.ToDictionary(b => b.Id!, b => b);
        var tables = allBlocks.Where(b => b.BlockType == BlockType.TABLE).ToList();

        var sb = new StringBuilder();
        bool foundFirstHeader = false;
        string? headerKey = null;

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

            if (!foundFirstHeader)
            {
                int headerIndex = allRows.FindIndex(LooksLikeHeader);
                if (headerIndex < 0) continue;

                foundFirstHeader = true;
                var filteredRows = allRows.Skip(headerIndex).ToList();
                headerKey = HeaderKey(filteredRows.First());

                foreach (var row in filteredRows)
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
                    var rowKey = HeaderKey(row);
                    if (headerKey != null && rowKey == headerKey) continue;
                    sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
                }
            }
        }

        return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
    }

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

    // === Tabula CSV builder ===
    public static (string csv, bool hasHeader, int dataRows) BuildCsvFromTablesPreferHeader(
        IReadOnlyList<Table> tables)
    {
        var sb = new StringBuilder();
        var wroteHeader = false;
        var headerKey = "";
        var dataRows = 0;
        int headerColCount = 0;

        foreach (var table in tables ?? Array.Empty<Table>())
        {
            var rows = table.Rows
                .Select(r => CleanRow(r.Select(c => c.GetText() ?? "").ToList()))
                .ToList();

            if (rows.Count == 0) continue;

            // find header in first few rows (up to 3)
            var headerIdx = -1;
            var limit = Math.Min(rows.Count, 3);
            for (int i = 0; i < limit; i++)
            {
                if (LooksLikeHeader(rows[i]) && !IsBlankRow(rows[i]))
                {
                    headerIdx = i;
                    break;
                }
            }

            if (headerIdx < 0) continue;

            var header = CleanRow(rows[headerIdx]);
            if (header.Count == 0) continue;

            if (!wroteHeader)
            {
                headerColCount = header.Count; // define schema width
                sb.AppendLine(string.Join(",", header.Select(CsvEscape)));
                wroteHeader = true;
                headerKey = HeaderKey(header);
            }

            for (int i = headerIdx + 1; i < rows.Count; i++)
            {
                var row = CleanRow(rows[i]);
                if (row.Count == 0 || IsBlankRow(row)) continue;
                if (LooksLikeHeader(row) && HeaderKey(row) == headerKey) continue;

                // normalize row to header width
                if (row.Count > headerColCount)
                    row = row.Take(headerColCount).ToList();
                else if (row.Count < headerColCount)
                    row.AddRange(Enumerable.Repeat("", headerColCount - row.Count));

                sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
                dataRows++;
            }
        }

        return (sb.ToString(), wroteHeader, dataRows);
    }

    // === Normalization helper ===
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
