using Amazon.Textract.Model;
using System.Text;
using System.Text.RegularExpressions;
using Amazon.Textract;

namespace FinoBackend.Services.ReceiptConverter;

public static class ReceiptCsvHelper
{
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
                .Select(g => g.OrderBy(c => c.ColumnIndex).Select(c => GetCellText(c, blockMap)).ToList())
                .ToList();

            if (allRows.Count == 0) continue;

            // --- Detect the first header row ---
            if (!foundFirstHeader)
            {
                int headerIndex = allRows.FindIndex(LooksLikeHeader);
                if (headerIndex < 0) continue; // skip until we find a valid header

                foundFirstHeader = true;
                var filteredRows = allRows.Skip(headerIndex).ToList();
                headerKey = RowKey(filteredRows.First());

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
                    var rowKey = RowKey(row);
                    sb.AppendLine(string.Join(",", row.Select(CsvEscape)));
                }
            }
        }

        return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    // 🔑 Header detection (English + Vietnamese keywords)
    private static bool LooksLikeHeader(IReadOnlyList<string> row)
    {
        var j = string.Join(' ', row).ToLowerInvariant();

        // --- Common receipt headers in English ---
        bool hasReceipt = j.Contains("receipt") || j.Contains("invoice") || j.Contains("bill");
        bool hasDate = j.Contains("date");
        bool hasAmount = j.Contains("amount") || j.Contains("total") || j.Contains("price");

        // --- Common receipt headers in Vietnamese ---
        bool hasVietnameseNames =
            j.Contains("người gửi") || j.Contains("người nhận") || j.Contains("địa chỉ") ||
            j.Contains("số điện thoại") || j.Contains("mã vận đơn");

        bool hasVietnameseMoney =
            j.Contains("tiền thu hộ") || j.Contains("tổng cộng") || j.Contains("thành tiền") ||
            j.Contains("đơn giá") || j.Contains("số lượng") || j.Contains("stt") ;

        return (hasReceipt && (hasDate || hasAmount)) || hasVietnameseNames || hasVietnameseMoney;
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
}
