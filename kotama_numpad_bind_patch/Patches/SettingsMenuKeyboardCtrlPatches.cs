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
    private static bool ShouldUseExternalStore(string formatKey)
    {
        if (string.IsNullOrWhiteSpace(formatKey))
        {
            return false;
        }

        if (PatchedPressText.IsPatchedRelated(formatKey) || PatchedPressText.IsPatchedControlPath(formatKey))
        {
            return true;
        }

        // Known unsafe key for the game's setting serializer (breaks Setting.json).
        if (formatKey.Contains("Shift", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool Prefix(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl __instance, string formatKey, EscapeGame.UIGen.Keyboard keyboard, ref bool playUx, ref bool __result)
    {
        if (string.IsNullOrWhiteSpace(formatKey))
        {
            __result = false;
            playUx = false;
            return false;
        }

        if (!ShouldUseExternalStore(formatKey))
        {
            return true;
        }

        try
        {
            // External-store strategy: apply at runtime, store in our own file, avoid touching Setting.json.
            string effectiveKey = formatKey;
            try
            {
                // The game sometimes gives ambiguous labels like "Numpad" (no digit/operator).
                // Prefer the actual InputSystem control path we observed during ListenRebindingAnyKey.
                string lastPath = RebindingUiState.LastControlPath;
                if (!string.IsNullOrWhiteSpace(lastPath) && !InputPathUtil.IsAnyKeyPath(lastPath))
                {
                    if (string.Equals(effectiveKey, "Numpad", StringComparison.OrdinalIgnoreCase) ||
                        effectiveKey.Contains("Shift", StringComparison.OrdinalIgnoreCase))
                    {
                        effectiveKey = lastPath;
                    }
                }
            }
            catch
            {
                // ignore
            }

            effectiveKey = InputPathUtil.NormalizeControlPath(effectiveKey);

            if (NumpadBindingSalvage.TryApply(__instance, effectiveKey, keyboard, playUx, persistToSettings: false))
            {
                cfg.UI_SettingKeyboard setting = null;
                try { setting = keyboard?._tbSettingKeyboard; } catch { /* ignore */ }

                int cnfId = setting?.Id ?? 0;
                string inputActionName = keyboard?._inputActionName ?? string.Empty;
                string originKey = keyboard?._originBindingBtn ?? string.Empty;
                bool applyAllMaps = setting != null && setting.Action != null && setting.Action.IsAllOverride;

                if (cnfId != 0)
                {
                    string key = effectiveKey;
                    if (PatchedPressText.IsPatchedRelated(key) || PatchedPressText.IsPatchedControlPath(key))
                    {
                        key = PatchedPressText.NormalizeToControlPath(key);
                    }

                    ExternalBindingsStore.Upsert(new ExternalBindingsStore.Entry
                    {
                        CnfId = cnfId,
                        Key = key,
                        InputActionName = inputActionName,
                        OriginKey = originKey,
                        ApplyAllMaps = applyAllMaps
                    });
                }

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

    private static void Postfix(string formatKey, EscapeGame.UIGen.Keyboard keyboard, ref bool __result)
    {
        // If the user binds a *native* key successfully, drop any external mapping for this cnfId.
        if (!__result)
        {
            return;
        }

        if (ShouldUseExternalStore(formatKey))
        {
            return;
        }

        try
        {
            int cnfId = keyboard?._tbSettingKeyboard?.Id ?? 0;
            if (cnfId != 0)
            {
                ExternalBindingsStore.Remove(cnfId);
            }
        }
        catch
        {
            // ignore
        }
    }

    private static void Postfix_NoOp(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl __instance, string formatKey, EscapeGame.UIGen.Keyboard keyboard, bool playUx, ref bool __result)
    {
        if (__result)
        {
            return;
        }

        if (!ShouldUseExternalStore(formatKey))
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
                RebindingUiState.ClearLastControl();

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
            RebindingUiState.ClearLastControl();

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

