using System;
using cfg;
using HarmonyLib;

namespace Kotama.NumpadRebind;

[HarmonyPatch(typeof(EscapeGame.UIGen.Keyboard), nameof(EscapeGame.UIGen.Keyboard.SetData))]
internal static class Patch_Keyboard_SetData
{
    private static void Prefix(UI_SettingKeyboard settingData, ref cfg.TbCfg.InputDisData bindingData)
    {
        try
        {
            if (settingData == null || settingData.Id == 0)
            {
                return;
            }

            string overrideKey;
            try
            {
                overrideKey = EscapeGame.UIGen.KeyboardBindingHelper.GetOverride(settingData.Id);
            }
            catch
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(overrideKey))
            {
                return;
            }

            if (!PatchedPressText.IsPatchedControlPath(overrideKey) && !PatchedPressText.IsPatchedRelated(overrideKey))
            {
                return;
            }

            bindingData = PatchedDisplay.GetBindingDisData(overrideKey);
        }
        catch (Exception ex)
        {
            NumpadRebindPlugin.LogSource?.LogDebug($"Keyboard.SetData Prefix failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void Postfix(EscapeGame.UIGen.Keyboard __instance)
    {
        try
        {
            if (!KeyboardUiSafe.TryApplyNumpadOverrideTextOnly(__instance))
            {
                KeyboardUiSafe.TryRestoreIconMode(__instance);
            }
        }
        catch
        {
            // never break UI rendering
        }
    }
}

[HarmonyPatch(typeof(EscapeGame.UIGen.Keyboard), nameof(EscapeGame.UIGen.Keyboard.SetCurrentBtn))]
internal static class Patch_Keyboard_SetCurrentBtn
{
    private static bool Prefix(EscapeGame.UIGen.Keyboard __instance, ref cfg.TbCfg.InputDisData changeData, ref string __state)
    {
        __state = string.Empty;
        try
        {
            // During active rebinding, let the game show "输入任意键" without our override text.
            UI_SettingKeyboard setting = null;
            try { setting = __instance?._tbSettingKeyboard; } catch { /* ignore */ }

            int settingId = 0;
            try { settingId = setting?.Id ?? 0; } catch { settingId = 0; }

            bool isRebinding = RebindingUiState.IsActiveFor(__instance) ||
                (settingId != 0 && RebindingUiState.IsActiveFor(settingId));
            if (isRebinding)
            {
                return true;
            }

            // Prefer the key being applied right now; it is what the game's UI is trying to display.
            // For patched keys (numpad / mouse side), the native icon table typically lacks sprites,
            // causing FairyGUI.NTexture..ctor(sprite:null) -> NullReferenceException.
            string candidateKey = string.Empty;
            try { candidateKey = changeData?.InputID ?? string.Empty; } catch { candidateKey = string.Empty; }
            if (string.IsNullOrWhiteSpace(candidateKey))
            {
                try { candidateKey = changeData?.PressTxt ?? string.Empty; } catch { candidateKey = string.Empty; }
            }
            if (string.IsNullOrWhiteSpace(candidateKey) && setting != null && setting.Id != 0)
            {
                try { candidateKey = EscapeGame.UIGen.KeyboardBindingHelper.GetOverride(setting.Id) ?? string.Empty; } catch { candidateKey = string.Empty; }
            }

            if (string.IsNullOrWhiteSpace(candidateKey))
            {
                return true;
            }

            if (!PatchedPressText.IsPatchedControlPath(candidateKey) && !PatchedPressText.IsPatchedRelated(candidateKey))
            {
                return true;
            }

            // For patched keys, do not run the original SetCurrentBtn at all. It may try to build
            // an icon sprite that doesn't exist and throw, leaving the UI stuck.
            __state = PatchedPressText.NormalizeToControlPath(candidateKey) ?? candidateKey;

            cfg.TbCfg.InputDisData patchedDisData = null;
            try { patchedDisData = PatchedDisplay.GetBindingDisData(__state); } catch { patchedDisData = null; }
            if (patchedDisData != null)
            {
                changeData = patchedDisData;
            }

            try { __instance._bindingData = changeData; } catch { /* ignore */ }
            try { __instance.CleanCurrentBtn(); } catch { /* ignore */ }

            KeyboardUiSafe.TryApplyBindingTextOnly(__instance, PatchedPressText.ToDisplayName(__state));
            return false;
        }
        catch
        {
            // never break UI rendering
            return true;
        }
    }

    private static void Postfix(EscapeGame.UIGen.Keyboard __instance, string __state)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(__state))
            {
                return;
            }

            UI_SettingKeyboard setting = __instance._tbSettingKeyboard;
            if (RebindingUiState.IsActiveFor(__instance) || (setting != null && RebindingUiState.IsActiveFor(setting.Id)))
            {
                return;
            }

            KeyboardUiSafe.TryApplyBindingTextOnly(__instance, PatchedPressText.ToDisplayName(__state));
        }
        catch
        {
            // ignore
        }
    }

    private static Exception Finalizer(EscapeGame.UIGen.Keyboard __instance, cfg.TbCfg.InputDisData changeData, string __state, Exception __exception)
    {
        if (__exception == null)
        {
            return null;
        }

        try
        {
            string exceptionText = __exception.ToString();
            if (!exceptionText.Contains("FairyGUI.NTexture..ctor", StringComparison.Ordinal) ||
                !exceptionText.Contains("NullReferenceException", StringComparison.Ordinal))
            {
                return __exception;
            }

            string candidateKey = __state;
            if (string.IsNullOrWhiteSpace(candidateKey))
            {
                try
                {
                    UI_SettingKeyboard setting = __instance?._tbSettingKeyboard;
                    if (setting != null && setting.Id != 0)
                    {
                        candidateKey = EscapeGame.UIGen.KeyboardBindingHelper.GetOverride(setting.Id);
                    }
                }
                catch
                {
                    // ignore
                }
            }

            if (string.IsNullOrWhiteSpace(candidateKey))
            {
                try
                {
                    candidateKey = changeData?.PressTxt ?? changeData?.InputID ?? string.Empty;
                }
                catch
                {
                    candidateKey = string.Empty;
                }
            }

            if (!PatchedPressText.IsPatchedControlPath(candidateKey) && !PatchedPressText.IsPatchedRelated(candidateKey))
            {
                return __exception;
            }

            try
            {
                __instance?.CleanCurrentBtn();
            }
            catch
            {
                // ignore
            }

            KeyboardUiSafe.TryApplyBindingTextOnly(__instance, PatchedPressText.ToDisplayName(candidateKey));
            return null;
        }
        catch
        {
            // If we fail to match/handle, don't hide the original exception.
            return __exception;
        }
    }
}

