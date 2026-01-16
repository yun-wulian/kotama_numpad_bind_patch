using System;

namespace Kotama.NumpadRebind;

internal static class RebindingUiState
{
    public static bool IsRebinding;
    public static int ActiveCnfId;
    public static IntPtr ActiveKeyboardPtr;
    public static string LastControlPath;

    public static bool IsActiveFor(int cnfId)
    {
        return IsRebinding && cnfId != 0 && cnfId == ActiveCnfId;
    }

    public static bool IsActiveFor(EscapeGame.UIGen.Keyboard keyboard)
    {
        return IsRebinding
            && keyboard != null
            && ActiveKeyboardPtr != IntPtr.Zero
            && keyboard.Pointer == ActiveKeyboardPtr;
    }

    public static void ClearLastControl()
    {
        LastControlPath = null;
    }

    public static void UpdateLastControlPath(string controlPath)
    {
        if (!IsRebinding || ActiveCnfId == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(controlPath))
        {
            return;
        }

        LastControlPath = controlPath.Trim();
    }
}

