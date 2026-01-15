using System;
using cfg.TbCfg;
using HarmonyLib;

namespace Kotama.NumpadRebind;

[HarmonyPatch(typeof(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl), nameof(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl.PreCheckCompositeCanOverride))]
internal static class Patch_SettingsMenuKeyboardCtrl_PreCheckCompositeCanOverride
{
    private static bool Prefix(string compositeKey, cfg.TbCfg.InputDeviceType deviceType, ref bool __result)
    {
        if (!PatchedPressText.IsPatchedRelated(compositeKey) && !PatchedPressText.IsPatchedControlPath(compositeKey))
        {
            return true;
        }

        // The game uses this as a gate before allowing composite binding overrides.
        // If it blocks numpad/mouse side, force-allow.
        NumpadRebindPlugin.LogSource?.LogInfo($"Force-allow composite override for \"{compositeKey}\" ({deviceType}).");
        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl), nameof(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl.SetKeyboardBinding))]
internal static class Patch_SettingsMenuKeyboardCtrl_SetKeyboardBinding
{
    private static bool Prefix(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl __instance, string formatKey, EscapeGame.UIGen.Keyboard keyboard, ref bool playUx, ref bool __result)
    {
        if (string.IsNullOrWhiteSpace(formatKey))
        {
            __result = false;
            playUx = false;
            return false;
        }

        if (!PatchedPressText.IsPatchedRelated(formatKey) && !PatchedPressText.IsPatchedControlPath(formatKey))
        {
            return true;
        }

        // Keep the game's own UX state for successful binds. We bypass the original failure path,
        // so leaving playUx intact doesn't trigger the "un-bindable key" hint, but it helps the
        // menu restore focus/navigation correctly after rebinding.

        try
        {
            if (NumpadBindingSalvage.TryApply(__instance, formatKey, keyboard, playUx))
            {
                __result = true;
                return false;
            }

            NumpadRebindPlugin.LogSource?.LogWarning($"Cannot salvage binding for \"{formatKey}\" (missing UI state).");
        }
        catch (Exception ex)
        {
            NumpadRebindPlugin.LogSource?.LogWarning($"Failed to salvage binding for \"{formatKey}\": {ex.GetType().Name}: {ex.Message}");
        }

        __result = false;
        return false;
    }

    private static void Postfix_NoOp(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl __instance, string formatKey, EscapeGame.UIGen.Keyboard keyboard, bool playUx, ref bool __result)
    {
        if (__result)
        {
            return;
        }

        if (!PatchedPressText.IsPatchedRelated(formatKey) && !PatchedPressText.IsPatchedControlPath(formatKey))
        {
            return;
        }
    }
}

[HarmonyPatch(typeof(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl), nameof(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl.SwitchRebindingListen))]
internal static class Patch_SettingsMenuKeyboardCtrl_SwitchRebindingListen
{
    private static void Postfix(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl __instance, bool isRebinding)
    {
        try
        {
            RebindingUiState.IsRebinding = isRebinding;
            if (!isRebinding)
            {
                IntPtr rebindingKeyboardPtr = RebindingUiState.ActiveKeyboardPtr;
                RebindingUiState.ActiveCnfId = 0;
                RebindingUiState.ActiveKeyboardPtr = IntPtr.Zero;

                // Rebinding is ending: refresh the active row to show the stored override text,
                // now that the game's "press any key" prompt should be cleared.
                try
                {
                    EscapeGame.UIGen.Keyboard optionItem = null;
                    try
                    {
                        if (rebindingKeyboardPtr != IntPtr.Zero)
                        {
                            optionItem = new EscapeGame.UIGen.Keyboard(rebindingKeyboardPtr);
                        }
                    }
                    catch
                    {
                        optionItem = null;
                    }

                    optionItem ??= __instance._optionItem;
                    if (optionItem != null)
                    {
                        InputDisData existingBinding = null;
                        try { existingBinding = optionItem._bindingData; } catch { /* ignore */ }

                        int optionCnfId = 0;
                        try { optionCnfId = optionItem._tbSettingKeyboard?.Id ?? 0; } catch { /* ignore */ }

                        if (optionCnfId != 0)
                        {
                            string overrideKey = null;
                            try { overrideKey = EscapeGame.UIGen.KeyboardBindingHelper.GetOverride(optionCnfId); } catch { /* ignore */ }

                            if (!string.IsNullOrWhiteSpace(overrideKey) &&
                                (PatchedPressText.IsPatchedControlPath(overrideKey) || PatchedPressText.IsPatchedRelated(overrideKey)))
                            {
                                try
                                {
                                    InputDisData disData = PatchedDisplay.GetBindingDisData(overrideKey);
                                    if (disData != null)
                                    {
                                        // Use SetData to reset internal UI sub-states (including the "press any key"
                                        // prompt transition) before we apply our text-only override.
                                        optionItem.SetData(optionItem._tbSettingKeyboard, optionItem._inputActionName, optionItem._originBindingBtn, disData);
                                    }
                                }
                                catch
                                {
                                    // ignore
                                }

                                KeyboardUiSafe.TryApplyBindingTextOnly(optionItem, PatchedPressText.ToDisplayName(overrideKey));
                            }
                            else
                            {
                                // Switching back to a native-supported key: restore the game's icon mode and ensure
                                // the binding area doesn't stay blank.
                                try
                                {
                                    if (existingBinding != null)
                                    {
                                        optionItem.SetData(optionItem._tbSettingKeyboard, optionItem._inputActionName, optionItem._originBindingBtn, existingBinding);
                                    }
                                }
                                catch
                                {
                                    // ignore
                                }

                                KeyboardUiSafe.TryRestoreIconMode(optionItem);
                            }
                        }
                        else
                        {
                            KeyboardUiSafe.TryRestoreIconMode(optionItem);
                        }
                    }
                }
                catch
                {
                    // ignore
                }
                return;
            }

            EscapeGame.UIGen.Keyboard current = __instance._optionItem;
            int cnfId = 0;
            try { cnfId = current?._tbSettingKeyboard?.Id ?? 0; } catch { /* ignore */ }

            RebindingUiState.ActiveCnfId = cnfId;
            RebindingUiState.ActiveKeyboardPtr = current?.Pointer ?? IntPtr.Zero;

            // Avoid double-text overlap: if the current row previously displayed our text
            // (on the binding button), clear it while the game shows "输入任意键".
            KeyboardUiSafe.TryPrepareRebindingVisual(current);
        }
        catch
        {
            // ignore
        }
    }
}

