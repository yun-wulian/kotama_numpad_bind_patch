using System;
using System.Text.RegularExpressions;

namespace Kotama.NumpadRebind;

internal static class NumpadPressText
{
    private static readonly Regex NumpadDigitRegex = new(
        @"(?ix)\b(?:num(?:pad)?|keypad|kp)[\s\-_]*([0-9])\b");

    public static bool IsNumpadRelated(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string v = value.Trim();
        return v.Contains("numpad", StringComparison.OrdinalIgnoreCase)
            || v.Contains("keypad", StringComparison.OrdinalIgnoreCase)
            || NumpadDigitRegex.IsMatch(v);
    }

    public static bool IsNumpadControlPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string v = value.Trim();
        return v.Contains("/numpad", StringComparison.OrdinalIgnoreCase)
            || v.Contains("numpad", StringComparison.OrdinalIgnoreCase)
            || v.Contains("keypad", StringComparison.OrdinalIgnoreCase)
            || v.Contains("kp", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryMapToNonNumpadLookup(string pressTxt, out string mapped)
    {
        mapped = pressTxt;
        if (string.IsNullOrWhiteSpace(pressTxt))
        {
            return false;
        }

        string v = pressTxt.Trim();

        Match digit = NumpadDigitRegex.Match(v);
        if (digit.Success)
        {
            mapped = digit.Groups[1].Value;
            return true;
        }

        if (v.Contains("numpad", StringComparison.OrdinalIgnoreCase) ||
            v.Contains("keypad", StringComparison.OrdinalIgnoreCase))
        {
            string normalized = v
                .Replace("Keypad", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Numpad", "", StringComparison.OrdinalIgnoreCase)
                .Replace("NumPad", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Num Pad", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            mapped = normalized switch
            {
                "Enter" or "Return" => "Enter",
                "Plus" or "+" => "+",
                "Minus" or "-" => "-",
                "Multiply" or "*" => "*",
                "Divide" or "/" => "/",
                "Period" or "." => ".",
                "Equals" or "=" => "=",
                "NumLock" or "Num Lock" => "NumLock",
                _ => normalized,
            };

            return !string.Equals(mapped, pressTxt, StringComparison.Ordinal);
        }

        if (v.Contains("<Keyboard>/numpad", StringComparison.OrdinalIgnoreCase))
        {
            // If the game passes through control paths, map to a basic printable key.
            // Examples: "<Keyboard>/numpad1" => "1"
            int idx = v.LastIndexOf("numpad", StringComparison.OrdinalIgnoreCase);
            string suffix = v[(idx + "numpad".Length)..].TrimStart();
            if (suffix.Length > 0 && char.IsDigit(suffix[0]))
            {
                mapped = suffix[0].ToString();
                return true;
            }

            if (suffix.StartsWith("enter", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "Enter";
                return true;
            }

            if (suffix.StartsWith("plus", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "+";
                return true;
            }

            if (suffix.StartsWith("minus", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "-";
                return true;
            }

            if (suffix.StartsWith("multiply", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "*";
                return true;
            }

            if (suffix.StartsWith("divide", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "/";
                return true;
            }

            if (suffix.StartsWith("period", StringComparison.OrdinalIgnoreCase))
            {
                mapped = ".";
                return true;
            }

            if (suffix.StartsWith("equals", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "=";
                return true;
            }

            if (suffix.StartsWith("numlock", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "NumLock";
                return true;
            }

            mapped = "Enter";
            return true; // fallback
        }

        return false;
    }

    public static string ToDisplayName(string pressTxt)
    {
        if (string.IsNullOrWhiteSpace(pressTxt))
        {
            return pressTxt;
        }

        string v = pressTxt.Trim();
        if (!IsNumpadRelated(v))
        {
            return v;
        }

        Match digit = NumpadDigitRegex.Match(v);
        if (digit.Success)
        {
            return $"Numpad {digit.Groups[1].Value}";
        }

        if (v.Contains("Enter", StringComparison.OrdinalIgnoreCase) ||
            v.Contains("Return", StringComparison.OrdinalIgnoreCase))
        {
            return "Numpad Enter";
        }

        if (v.Contains("Plus", StringComparison.OrdinalIgnoreCase) || v.Contains("+"))
        {
            return "Numpad +";
        }

        if (v.Contains("Minus", StringComparison.OrdinalIgnoreCase) || v.Contains("-"))
        {
            return "Numpad -";
        }

        if (v.Contains("Multiply", StringComparison.OrdinalIgnoreCase) || v.Contains("*"))
        {
            return "Numpad *";
        }

        if (v.Contains("Divide", StringComparison.OrdinalIgnoreCase) || v.Contains("/"))
        {
            return "Numpad /";
        }

        if (v.Contains("Period", StringComparison.OrdinalIgnoreCase) || v.Contains("."))
        {
            return "Numpad .";
        }

        if (v.Contains("Equals", StringComparison.OrdinalIgnoreCase) || v.Contains("="))
        {
            return "Numpad =";
        }

        if (v.Contains("NumLock", StringComparison.OrdinalIgnoreCase) || v.Contains("Num Lock", StringComparison.OrdinalIgnoreCase))
        {
            return "NumLock";
        }

        return v;
    }

    public static string NormalizeToKeyboardControlPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string v = value.Trim();

        // Already a control path.
        if (v.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase))
        {
            return v;
        }

        if (v.StartsWith("/numpad", StringComparison.OrdinalIgnoreCase))
        {
            return "<Keyboard>" + v;
        }

        Match digit = NumpadDigitRegex.Match(v);
        if (digit.Success)
        {
            return "<Keyboard>/numpad" + digit.Groups[1].Value;
        }

        if (v.Contains("Enter", StringComparison.OrdinalIgnoreCase) ||
            v.Contains("Return", StringComparison.OrdinalIgnoreCase))
        {
            return "<Keyboard>/numpadEnter";
        }

        if (v.Contains("Plus", StringComparison.OrdinalIgnoreCase) || v.Contains("+"))
        {
            return "<Keyboard>/numpadPlus";
        }

        if (v.Contains("Minus", StringComparison.OrdinalIgnoreCase) || v.Contains("-"))
        {
            return "<Keyboard>/numpadMinus";
        }

        if (v.Contains("Multiply", StringComparison.OrdinalIgnoreCase) || v.Contains("*"))
        {
            return "<Keyboard>/numpadMultiply";
        }

        if (v.Contains("Divide", StringComparison.OrdinalIgnoreCase) || v.Contains("/"))
        {
            return "<Keyboard>/numpadDivide";
        }

        if (v.Contains("Period", StringComparison.OrdinalIgnoreCase) || v.Contains("."))
        {
            return "<Keyboard>/numpadPeriod";
        }

        if (v.Contains("Equals", StringComparison.OrdinalIgnoreCase) || v.Contains("="))
        {
            return "<Keyboard>/numpadEquals";
        }

        if (v.Contains("NumLock", StringComparison.OrdinalIgnoreCase) || v.Contains("Num Lock", StringComparison.OrdinalIgnoreCase))
        {
            return "<Keyboard>/numLock";
        }

        return v;
    }
}

