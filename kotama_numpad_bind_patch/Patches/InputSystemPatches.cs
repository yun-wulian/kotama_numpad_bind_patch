using HarmonyLib;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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
            string normalized = InputPathUtil.NormalizeControlPath(path);

            // The rebinding action is typically bound to "<Keyboard>/anyKey", which means
            // context.control can often be the "anyKey" control, not the actual key pressed.
            // In that case, scan the keyboard for the key pressed this frame.
            if (InputPathUtil.IsAnyKeyPath(normalized))
            {
                try
                {
                    Keyboard kb = Keyboard.current;
                    if (kb != null)
                    {
                        foreach (KeyControl key in kb.allKeys)
                        {
                            if (key != null && key.wasPressedThisFrame)
                            {
                                normalized = InputPathUtil.NormalizeControlPath(key.path);
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    // ignore
                }

                try
                {
                    Mouse mouse = Mouse.current;
                    if (mouse != null)
                    {
                        if (mouse.backButton != null && mouse.backButton.wasPressedThisFrame)
                        {
                            normalized = InputPathUtil.NormalizeControlPath(mouse.backButton.path);
                        }
                        else if (mouse.forwardButton != null && mouse.forwardButton.wasPressedThisFrame)
                        {
                            normalized = InputPathUtil.NormalizeControlPath(mouse.forwardButton.path);
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }

            if (!string.IsNullOrWhiteSpace(normalized) && !InputPathUtil.IsAnyKeyPath(normalized))
            {
                RebindingUiState.UpdateLastControlPath(normalized);

                // Keep the log low-noise: only record patched keys + Shift (known issue trigger).
                if (PatchedPressText.IsPatchedControlPath(normalized) ||
                    normalized.Contains("Shift", System.StringComparison.OrdinalIgnoreCase))
                {
                    NumpadRebindPlugin.LogSource?.LogInfo($"ListenRebindingAnyKey: controlPath=\"{normalized}\" displayName=\"{context.control.displayName}\"");
                }
            }
        }
        catch
        {
            // Best-effort logging only.
        }
    }
}

