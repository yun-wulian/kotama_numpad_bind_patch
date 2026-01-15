using System;
using System.Text.RegularExpressions;

namespace Kotama.NumpadRebind;

internal static class MouseSidePressText
{
    private static readonly Regex MouseButtonRegex = new(
        @"(?ix)\b(?:mb|mouse)\s*([45])\b");

    private static readonly Regex XButtonRegex = new(
        @"(?ix)\bxbutton\s*([12])\b");

    public static bool IsMouseSideRelated(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string v = value.Trim();
        return v.Contains("<Mouse>/backButton", StringComparison.OrdinalIgnoreCase)
            || v.Contains("<Mouse>/forwardButton", StringComparison.OrdinalIgnoreCase)
            || v.Contains("backButton", StringComparison.OrdinalIgnoreCase)
            || v.Contains("forwardButton", StringComparison.OrdinalIgnoreCase)
            || MouseButtonRegex.IsMatch(v)
            || XButtonRegex.IsMatch(v);
    }

    public static bool IsMouseSideControlPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string v = value.Trim();
        if (!v.Contains("<Mouse>/", StringComparison.OrdinalIgnoreCase) && !v.StartsWith("/backButton", StringComparison.OrdinalIgnoreCase) &&
            !v.StartsWith("/forwardButton", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return v.Contains("backButton", StringComparison.OrdinalIgnoreCase) || v.Contains("forwardButton", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeToMouseControlPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string v = value.Trim();
        if (v.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase))
        {
            return v;
        }

        if (v.StartsWith("/backButton", StringComparison.OrdinalIgnoreCase))
        {
            return "<Mouse>" + v;
        }

        if (v.StartsWith("/forwardButton", StringComparison.OrdinalIgnoreCase))
        {
            return "<Mouse>" + v;
        }

        if (v.Contains("backButton", StringComparison.OrdinalIgnoreCase))
        {
            return "<Mouse>/backButton";
        }

        if (v.Contains("forwardButton", StringComparison.OrdinalIgnoreCase))
        {
            return "<Mouse>/forwardButton";
        }

        Match mb = MouseButtonRegex.Match(v);
        if (mb.Success)
        {
            return mb.Groups[1].Value == "4" ? "<Mouse>/backButton" : "<Mouse>/forwardButton";
        }

        Match xb = XButtonRegex.Match(v);
        if (xb.Success)
        {
            return xb.Groups[1].Value == "1" ? "<Mouse>/backButton" : "<Mouse>/forwardButton";
        }

        return v;
    }

    public static string ToDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string v = value.Trim();
        if (v.Contains("backButton", StringComparison.OrdinalIgnoreCase) ||
            MouseButtonRegex.IsMatch(v) && MouseButtonRegex.Match(v).Groups[1].Value == "4" ||
            XButtonRegex.IsMatch(v) && XButtonRegex.Match(v).Groups[1].Value == "1")
        {
            return "MB4";
        }

        if (v.Contains("forwardButton", StringComparison.OrdinalIgnoreCase) ||
            MouseButtonRegex.IsMatch(v) && MouseButtonRegex.Match(v).Groups[1].Value == "5" ||
            XButtonRegex.IsMatch(v) && XButtonRegex.Match(v).Groups[1].Value == "2")
        {
            return "MB5";
        }

        return v;
    }
}

