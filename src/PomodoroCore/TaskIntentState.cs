namespace PomodoroCore;

/// <summary>
/// Minimal persisted "session intent" data.
/// This is intentionally not a full task system: just a current intent + pinned + recents.
/// </summary>
public sealed class TaskIntentState
{
    public const int MaxIntentLength = 80;
    public const int MaxPinned = 25;
    public const int MaxRecents = 10;

    public string? CurrentIntent { get; set; }
    public List<string> Pinned { get; set; } = new();
    public List<string> Recents { get; set; } = new();

    public void Normalize()
    {
        CurrentIntent = Sanitize(CurrentIntent);
        Pinned = NormalizeList(Pinned, MaxPinned);
        Recents = NormalizeList(Recents, MaxRecents);

        // Ensure recents don't contain the current intent twice in different casing.
        if (CurrentIntent is not null)
        {
            AddMru(Recents, CurrentIntent, MaxRecents);
        }
    }

    public bool SetCurrentIntent(string? raw, bool addToRecents)
    {
        var next = Sanitize(raw);
        var changed = !EqualsIgnoreCase(CurrentIntent, next);
        CurrentIntent = next;

        if (addToRecents && next is not null)
        {
            AddMru(Recents, next, MaxRecents);
        }

        // Defensive: keep caps/dedup invariants.
        Pinned = NormalizeList(Pinned, MaxPinned);
        Recents = NormalizeList(Recents, MaxRecents);
        return changed;
    }

    public bool Pin(string? raw)
    {
        var value = Sanitize(raw);
        if (value is null) return false;

        if (Pinned.Any(p => EqualsIgnoreCase(p, value))) return false;
        Pinned.Add(value);

        Pinned = NormalizeList(Pinned, MaxPinned);
        return true;
    }

    public bool Unpin(string? raw)
    {
        var value = Sanitize(raw);
        if (value is null) return false;

        var removed = RemoveFirst(Pinned, value);
        if (removed) Pinned = NormalizeList(Pinned, MaxPinned);
        return removed;
    }

    public bool IsPinned(string? raw)
    {
        var value = Sanitize(raw);
        if (value is null) return false;
        return Pinned.Any(p => EqualsIgnoreCase(p, value));
    }

    public static string? Sanitize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // Trim and collapse whitespace/newlines into single spaces.
        var s = raw.Trim();
        var chars = new List<char>(s.Length);
        var lastWasSpace = false;
        foreach (var ch in s)
        {
            var isWs = char.IsWhiteSpace(ch) || char.IsControl(ch);
            if (isWs)
            {
                if (!lastWasSpace)
                {
                    chars.Add(' ');
                    lastWasSpace = true;
                }

                continue;
            }

            chars.Add(ch);
            lastWasSpace = false;
        }

        var normalized = new string(chars.ToArray()).Trim();
        if (normalized.Length == 0) return null;
        if (normalized.Length > MaxIntentLength) normalized = normalized[..MaxIntentLength].Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    public static string TruncateForUi(string text, int maxChars)
    {
        if (maxChars <= 0) return string.Empty;
        if (text.Length <= maxChars) return text;
        if (maxChars <= 1) return text[..1];
        return text[..(maxChars - 1)] + "…";
    }

    private static List<string> NormalizeList(List<string>? items, int max)
    {
        var list = items ?? new List<string>();

        // Sanitize + case-insensitive de-dup, preserving first occurrence order.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(list.Count);
        foreach (var item in list)
        {
            var s = Sanitize(item);
            if (s is null) continue;
            if (!seen.Add(s)) continue;
            result.Add(s);
            if (result.Count >= max) break;
        }

        return result;
    }

    private static void AddMru(List<string> list, string item, int max)
    {
        RemoveFirst(list, item);
        list.Insert(0, item);
        if (list.Count > max) list.RemoveRange(max, list.Count - max);
    }

    private static bool RemoveFirst(List<string> list, string item)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (!EqualsIgnoreCase(list[i], item)) continue;
            list.RemoveAt(i);
            return true;
        }

        return false;
    }

    private static bool EqualsIgnoreCase(string? a, string? b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

