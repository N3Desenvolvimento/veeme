namespace Veeme.Utils;

public static class DbRowMapper
{
    public static IReadOnlyList<Dictionary<string, object?>> MapToDictionaries(IEnumerable<dynamic> rows)
    {
        var result = new List<Dictionary<string, object?>>();

        foreach (var row in rows)
        {
            if (row is IDictionary<string, object> rowDictionary)
            {
                var mapped = new Dictionary<string, object?>(
                    rowDictionary.Count,
                    StringComparer.OrdinalIgnoreCase
                );
                foreach (var pair in rowDictionary)
                {
                    mapped[pair.Key] = pair.Value;
                }
                result.Add(mapped);
                continue;
            }

            result.Add(new Dictionary<string, object?> { ["value"] = row });
        }

        return result;
    }

    public static decimal ConvertToDecimal(object? value)
    {
        if (value is null || value is DBNull)
        {
            return 0m;
        }

        if (value is decimal d)
        {
            return d;
        }

        if (value is int i)
        {
            return i;
        }

        if (value is long l)
        {
            return l;
        }

        if (value is double db)
        {
            return (decimal)db;
        }

        if (decimal.TryParse(value.ToString(), out var parsed))
        {
            return parsed;
        }

        return 0m;
    }

    public static bool IsTruthy(object? value)
    {
        if (value is null || value is DBNull)
        {
            return false;
        }

        if (value is bool b)
        {
            return b;
        }

        var text = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return text.Equals("S", StringComparison.OrdinalIgnoreCase)
            || text.Equals("1", StringComparison.OrdinalIgnoreCase)
            || text.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
    }
}
