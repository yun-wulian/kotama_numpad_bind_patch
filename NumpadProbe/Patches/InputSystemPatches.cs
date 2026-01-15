using HarmonyLib;
using UnityEngine.InputSystem;

namespace Kotama.NumpadRebind;

[HarmonyPatch(typeof(InputActionRebindingExtensions.RebindingOperation), nameof(InputActionRebindingExtensions.RebindingOperation.WithControlsExcluding))]
internal static class Patch_RebindingOperation_WithControlsExcluding
{
    private static bool Prefix(InputActionRebindingExtensions.RebindingOperation __instance, string path, ref InputActionRebindingExtensions.RebindingOperation __result)
    {
        if (!PatchedPressText.IsPatchedControlPath(path))
        {
            return true;
        }

        NumpadRebindPlugin.LogSource?.LogDebug($"Ignoring WithControlsExcluding(\"{path}\") to keep extra keys bindable (numpad / mouse side).");
        __result = __instance;
        return false;
    }
}

[HarmonyPatch(typeof(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl), nameof(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl.ListenRebindingAnyKey))]
internal static class Patch_SettingsMenuKeyboardCtrl_ListenRebindingAnyKey
{
    private static void Prefix(InputAction.CallbackContext context)
    {
        try
        {
            string path = context.control?.path;
            if (!string.IsNullOrEmpty(path) && PatchedPressText.IsPatchedControlPath(path))
            {
                NumpadRebindPlugin.LogSource?.LogInfo($"ListenRebindingAnyKey: controlPath=\"{path}\" displayName=\"{context.control.displayName}\"");
            }
        }
        catch
        {
            // Best-effort logging only.
        }
    }
}

