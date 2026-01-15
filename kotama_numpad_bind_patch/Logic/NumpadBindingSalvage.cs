using System;
using cfg.TbCfg;

namespace Kotama.NumpadRebind;

internal static class NumpadBindingSalvage
{
    public static bool TryApply(EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl ctrl, string formatKey, EscapeGame.UIGen.Keyboard keyboard, bool playUx)
    {
        if (ctrl == null || keyboard == null)
        {
            return false;
        }

        if (PatchedPressText.IsPatchedRelated(formatKey) || PatchedPressText.IsPatchedControlPath(formatKey))
        {
            formatKey = PatchedPressText.NormalizeToControlPath(formatKey);
        }

        cfg.UI_SettingKeyboard setting = null;
        try { setting = keyboard._tbSettingKeyboard; } catch { /* ignore */ }

        if (setting == null || setting.Id == 0)
        {
            return false;
        }

        int cnfId = setting.Id;
        string inputActionName = keyboard._inputActionName ?? string.Empty;
        string originKey = keyboard._originBindingBtn ?? string.Empty;
        bool applyAllMaps = setting.Action != null && setting.Action.IsAllOverride;

        try
        {
            bool isComposite;
            EscapeGame.UIGen.Keyboard already = ctrl.GetAlreadyKeyboard(formatKey, out isComposite);
            if (already != null && already.Pointer != keyboard.Pointer)
            {
                int alreadyId = 0;
                try { alreadyId = already._tbSettingKeyboard?.Id ?? 0; } catch { /* ignore */ }

                NumpadRebindPlugin.LogSource?.LogInfo(
                    $"Key conflict: \"{formatKey}\" is already used by cnfId={alreadyId}. Resetting the existing binding first.");

                ctrl.ResetKeyboard(formatKey, already);
            }
        }
        catch (Exception exConflict)
        {
            NumpadRebindPlugin.LogSource?.LogDebug($"Conflict resolution failed: {exConflict.GetType().Name}: {exConflict.Message}");
        }

        if (!string.IsNullOrWhiteSpace(inputActionName))
        {
            EscapeGame.Input.InputManager.RebindingBtn(
                inputActionName,
                originKey,
                formatKey,
                EscapeGame.Utils.Enum.ActionMapTypes.PlayerControls,
                applyAllMaps);
        }
        EscapeGame.Input.InputManager.RebindingBtn(originKey, formatKey);

        var map = new Il2CppSystem.Collections.Generic.Dictionary<int, string>();
        map.Add(cnfId, formatKey);

        EscapeGame.UIGen.KeyboardBindingHelper.SaveOverride(cnfId, formatKey);
        EscapeGame.UIGen.KeyboardBindingHelper.FlushRemapKeyboard(map);

        try
        {
            ctrl._View?.ResetCurrentKeyboard(formatKey, keyboard);
        }
        catch (Exception exMap)
        {
            NumpadRebindPlugin.LogSource?.LogDebug($"ResetCurrentKeyboard failed: {exMap.GetType().Name}: {exMap.Message}");
        }

        try
        {
            ctrl._bindingState = EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl.RebindingState.Origin;
        }
        catch
        {
            // ignore
        }

        try
        {
            InputDisData data = PatchedDisplay.GetBindingDisData(formatKey);
            if (data != null)
            {
                keyboard.SetData(setting, inputActionName, originKey, data);
                KeyboardUiSafe.TryApplyBindingTextOnly(keyboard, PatchedPressText.ToDisplayName(formatKey));
            }
        }
        catch
        {
            // best effort; UI will be refreshed after SwitchRebindingListen(false)
        }

        NumpadRebindPlugin.LogSource?.LogInfo(
            $"Applied numpad binding: cnfId={cnfId} action=\"{inputActionName}\" origin=\"{originKey}\" new=\"{formatKey}\" allMaps={applyAllMaps} playUx={playUx}");

        return true;
    }
}

