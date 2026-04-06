using System;
using System.Collections.Generic;

namespace PatreonDownloader.Implementation
{
    public static class PageFilterParser
    {
        public static HashSet<int> Parse(string filter)
        {
            HashSet<int> result = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(filter))
                return result;

            string[] parts = filter.Split(',', StringSplitOptions.None);
            foreach (string rawPart in parts)
            {
                string part = rawPart.Trim();
                if (string.IsNullOrWhiteSpace(part))
                    throw new FormatException("empty page segment");

                int dashIndex = part.IndexOf('-');
                if (dashIndex >= 0)
                {
                    if (part.IndexOf('-', dashIndex + 1) >= 0)
                        throw new FormatException($"invalid range: {part}");

                    string startPart = part.Substring(0, dashIndex).Trim();
                    string endPart = part.Substring(dashIndex + 1).Trim();

                    if (!int.TryParse(startPart, out int start) || !int.TryParse(endPart, out int end))
                        throw new FormatException($"invalid range: {part}");
                    if (start < 1 || end < 1)
                        throw new FormatException($"page numbers must be positive: {part}");
                    if (start > end)
                        throw new FormatException($"range start must be less or equal than end: {part}");

                    for (int page = start; page <= end; page++)
                        result.Add(page);
                }
                else
                {
                    if (!int.TryParse(part, out int page))
                        throw new FormatException($"invalid page number: {part}");
                    if (page < 1)
                        throw new FormatException($"page numbers must be positive: {part}");

                    result.Add(page);
                }
            }

            return result;
        }
    }
}
