using System;
using System.Text.RegularExpressions;
using cfg.TbCfg;

namespace Kotama.NumpadRebind;

internal static class NumpadDisplay
{
    private static string ExtractNumpadToken(string bindingKey)
    {
        if (string.IsNullOrWhiteSpace(bindingKey))
        {
            return string.Empty;
        }

        string v = bindingKey.Trim();
        int idx = v.LastIndexOf("numpad", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            string suffix = v[(idx + "numpad".Length)..].TrimStart();
            if (suffix.Length > 0 && char.IsDigit(suffix[0]))
            {
                return suffix[0].ToString();
            }

            if (suffix.StartsWith("enter", StringComparison.OrdinalIgnoreCase))
            {
                return "Enter";
            }

            if (suffix.StartsWith("plus", StringComparison.OrdinalIgnoreCase))
            {
                return "Plus";
            }

            if (suffix.StartsWith("minus", StringComparison.OrdinalIgnoreCase))
            {
                return "Minus";
            }

            if (suffix.StartsWith("multiply", StringComparison.OrdinalIgnoreCase))
            {
                return "Multiply";
            }

            if (suffix.StartsWith("divide", StringComparison.OrdinalIgnoreCase))
            {
                return "Divide";
            }

            if (suffix.StartsWith("period", StringComparison.OrdinalIgnoreCase))
            {
                return "Period";
            }

            if (suffix.StartsWith("equals", StringComparison.OrdinalIgnoreCase))
            {
                return "Equals";
            }
        }

        Match m = Regex.Match(v, @"(?ix)\b(?:numpad|keypad|kp)[\s\-_]*([0-9])\b");
        if (m.Success)
        {
            return m.Groups[1].Value;
        }

        return string.Empty;
    }

    private static bool TryMapToBaseLookup(string bindingKey, out string mapped)
    {
        mapped = string.Empty;

        // Avoid guessing "Keypad8/Numpad8" etc. Even if InputDisData exists, its sprite can be missing
        // and FairyGUI will throw (we saw this in Keyboard.SetCurrentBtn -> NTexture(sprite:null)).
        //
        // Instead, map to a known-safe non-numpad lookup ("8", "+", "Enter"...), which should always
        // have a valid sprite/text entry in the game's tables.
        if (NumpadPressText.TryMapToNonNumpadLookup(bindingKey, out mapped))
        {
            return !string.IsNullOrWhiteSpace(mapped);
        }

        string token = ExtractNumpadToken(bindingKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        mapped = token;
        return true;
    }

    public static InputDisData GetBindingDisData(string bindingKey)
    {
        string normalizedKey = NumpadPressText.NormalizeToKeyboardControlPath(bindingKey);

        // Prefer a known-safe base lookup (e.g., "5") and keep PressTxt as the logical remap key.
        // NOTE: The game uses Keyboard._bindingData.PressTxt (0x38) as the key source for SwapBinding.
        // If we put a display string like "Numpad 4" into PressTxt, swapping/conflict resolution breaks.
        if (TryMapToBaseLookup(bindingKey, out string mapped))
        {
            try
            {
                InputDisData baseData = EscapeGame.UIGen.KeyboardBindingHelper.GetInputDisData(mapped);
                if (baseData != null)
                {
                    return InputDisDataClone.CloneWithPressText(baseData, normalizedKey);
                }
            }
            catch
            {
                // ignore
            }
        }

        return InputDisDataClone.CreateMinimal(normalizedKey);
    }
}

