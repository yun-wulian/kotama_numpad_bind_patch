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
            UI_SettingKeyboard setting = __instance._tbSettingKeyboard;
            if (setting == null || setting.Id == 0)
            {
                return true;
            }

            // During active rebinding, let the game show "输入任意键" without our override text.
            bool isRebinding = RebindingUiState.IsActiveFor(__instance) || RebindingUiState.IsActiveFor(setting.Id);
            if (isRebinding)
            {
                return true;
            }

            // Prefer the persisted override key (stable); if it's absent, fall back to `changeData`
            // because the game may call SetCurrentBtn before SaveOverride/FlushRemapKeyboard.
            string candidateKey = string.Empty;
            try
            {
                candidateKey = EscapeGame.UIGen.KeyboardBindingHelper.GetOverride(setting.Id);
            }
            catch
            {
                // ignore
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

            if (string.IsNullOrWhiteSpace(candidateKey))
            {
                return true;
            }

            if (!PatchedPressText.IsPatchedControlPath(candidateKey) && !PatchedPressText.IsPatchedRelated(candidateKey))
            {
                return true;
            }

            // Skip the original implementation for patched keys (numpad / mouse side buttons) to avoid:
            // FairyGUI.NTexture..ctor(sprite:null) -> NullReferenceException
            // which causes the UI to remain stuck on "输入任意键".
            __state = candidateKey;
            changeData = PatchedDisplay.GetBindingDisData(candidateKey);
            return true;
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

