namespace Kotama.NumpadRebind;

internal static class PatchedPressText
{
    public static bool IsPatchedRelated(string value)
    {
        return NumpadPressText.IsNumpadRelated(value) || MouseSidePressText.IsMouseSideRelated(value);
    }

    public static bool IsPatchedControlPath(string value)
    {
        return NumpadPressText.IsNumpadControlPath(value) || MouseSidePressText.IsMouseSideControlPath(value);
    }

    public static string NormalizeToControlPath(string value)
    {
        if (MouseSidePressText.IsMouseSideRelated(value) || MouseSidePressText.IsMouseSideControlPath(value))
        {
            return MouseSidePressText.NormalizeToMouseControlPath(value);
        }

        return NumpadPressText.NormalizeToKeyboardControlPath(value);
    }

    public static string ToDisplayName(string value)
    {
        if (MouseSidePressText.IsMouseSideRelated(value) || MouseSidePressText.IsMouseSideControlPath(value))
        {
            return MouseSidePressText.ToDisplayName(value);
        }

        return NumpadPressText.ToDisplayName(value);
    }
}

