using System;

namespace Kotama.NumpadRebind;

internal static class InputPathUtil
{
    public static bool IsAnyKeyPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string v = path.Trim();
        return v.EndsWith("/anyKey", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "anyKey", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeControlPath(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return raw;
        }

        string v = raw.Trim();
        if (v.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) ||
            v.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase))
        {
            return v;
        }

        if (v.StartsWith("/Keyboard/", StringComparison.OrdinalIgnoreCase))
        {
            return "<Keyboard>/" + v["/Keyboard/".Length..];
        }

        if (v.StartsWith("/Mouse/", StringComparison.OrdinalIgnoreCase))
        {
            return "<Mouse>/" + v["/Mouse/".Length..];
        }

        // Some paths may come in as "<Keyboard>/..." already; leave unknown formats unchanged.
        return v;
    }
}

