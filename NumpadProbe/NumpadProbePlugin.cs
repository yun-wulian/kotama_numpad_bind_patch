using System;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using FairyGUI;
using cfg.TbCfg;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using UnityEngine.InputSystem;

namespace NumpadProbe;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class NumpadRebindPlugin : BasePlugin
{
    public const string PluginGuid = "com.yunwulian.kotama.numpad-rebind";
    public const string PluginName = "Kotama Numpad Rebind";
    public const string PluginVersion = "0.2.17";

    internal static ManualLogSource LogSource;

    public override void Load()
    {
        LogSource = Log;

        Harmony harmony = new(PluginGuid);
        harmony.PatchAll(typeof(NumpadRebindPlugin).Assembly);

        Log.LogInfo($"Loaded v{PluginVersion}. Strategy2 patches active (InputSystem rebinding + UI binding display).");
    }
}

internal static class NumpadPressText
{
    private static readonly Regex NumpadDigitRegex = new(
        @"(?ix)\b(?:num(?:pad)?|keypad|kp)[\s\-_]*([0-9])\b");

    public static bool IsNumpadRelated(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string v = value.Trim();
        return v.Contains("numpad", StringComparison.OrdinalIgnoreCase)
            || v.Contains("keypad", StringComparison.OrdinalIgnoreCase)
            || NumpadDigitRegex.IsMatch(v);
    }

    public static bool IsNumpadControlPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string v = value.Trim();
        return v.Contains("/numpad", StringComparison.OrdinalIgnoreCase)
            || v.Contains("numpad", StringComparison.OrdinalIgnoreCase)
            || v.Contains("keypad", StringComparison.OrdinalIgnoreCase)
            || v.Contains("kp", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryMapToNonNumpadLookup(string pressTxt, out string mapped)
    {
        mapped = pressTxt;
        if (string.IsNullOrWhiteSpace(pressTxt))
        {
            return false;
        }

        string v = pressTxt.Trim();

        Match digit = NumpadDigitRegex.Match(v);
        if (digit.Success)
        {
            mapped = digit.Groups[1].Value;
            return true;
        }

        if (v.Contains("numpad", StringComparison.OrdinalIgnoreCase) ||
            v.Contains("keypad", StringComparison.OrdinalIgnoreCase))
        {
            string normalized = v
                .Replace("Keypad", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Numpad", "", StringComparison.OrdinalIgnoreCase)
                .Replace("NumPad", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Num Pad", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            mapped = normalized switch
            {
                "Enter" or "Return" => "Enter",
                "Plus" or "+" => "+",
                "Minus" or "-" => "-",
                "Multiply" or "*" => "*",
                "Divide" or "/" => "/",
                "Period" or "." => ".",
                "Equals" or "=" => "=",
                "NumLock" or "Num Lock" => "NumLock",
                _ => normalized,
            };

            return !string.Equals(mapped, pressTxt, StringComparison.Ordinal);
        }

        if (v.Contains("<Keyboard>/numpad", StringComparison.OrdinalIgnoreCase))
        {
            // If the game passes through control paths, map to a basic printable key.
            // Examples: "<Keyboard>/numpad1" => "1"
            int idx = v.LastIndexOf("numpad", StringComparison.OrdinalIgnoreCase);
            string suffix = v[(idx + "numpad".Length)..].TrimStart();
            if (suffix.Length > 0 && char.IsDigit(suffix[0]))
            {
                mapped = suffix[0].ToString();
                return true;
            }

            if (suffix.StartsWith("enter", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "Enter";
                return true;
            }

            if (suffix.StartsWith("plus", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "+";
                return true;
            }

            if (suffix.StartsWith("minus", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "-";
                return true;
            }

            if (suffix.StartsWith("multiply", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "*";
                return true;
            }

            if (suffix.StartsWith("divide", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "/";
                return true;
            }

            if (suffix.StartsWith("period", StringComparison.OrdinalIgnoreCase))
            {
                mapped = ".";
                return true;
            }

            if (suffix.StartsWith("equals", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "=";
                return true;
            }

            if (suffix.StartsWith("numlock", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "NumLock";
                return true;
            }

            mapped = "Enter";
            return true; // fallback
        }

        return false;
    }

    public static string ToDisplayName(string pressTxt)
    {
        if (string.IsNullOrWhiteSpace(pressTxt))
        {
            return pressTxt;
        }

        string v = pressTxt.Trim();
        if (!IsNumpadRelated(v))
        {
            return v;
        }

        Match digit = NumpadDigitRegex.Match(v);
        if (digit.Success)
        {
            return $"Numpad {digit.Groups[1].Value}";
        }

        if (v.Contains("Enter", StringComparison.OrdinalIgnoreCase) ||
            v.Contains("Return", StringComparison.OrdinalIgnoreCase))
        {
            return "Numpad Enter";
        }

        if (v.Contains("Plus", StringComparison.OrdinalIgnoreCase) || v.Contains("+"))
        {
            return "Numpad +";
        }

        if (v.Contains("Minus", StringComparison.OrdinalIgnoreCase) || v.Contains("-"))
        {
            return "Numpad -";
        }

        if (v.Contains("Multiply", StringComparison.OrdinalIgnoreCase) || v.Contains("*"))
        {
            return "Numpad *";
        }

        if (v.Contains("Divide", StringComparison.OrdinalIgnoreCase) || v.Contains("/"))
        {
            return "Numpad /";
        }

        if (v.Contains("Period", StringComparison.OrdinalIgnoreCase) || v.Contains("."))
        {
            return "Numpad .";
        }

        if (v.Contains("Equals", StringComparison.OrdinalIgnoreCase) || v.Contains("="))
        {
            return "Numpad =";
        }

        if (v.Contains("NumLock", StringComparison.OrdinalIgnoreCase) || v.Contains("Num Lock", StringComparison.OrdinalIgnoreCase))
        {
            return "NumLock";
        }

        return v;
    }

    public static string NormalizeToKeyboardControlPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string v = value.Trim();

        // Already a control path.
        if (v.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase))
        {
            return v;
        }

        if (v.StartsWith("/numpad", StringComparison.OrdinalIgnoreCase))
        {
            return "<Keyboard>" + v;
        }

        Match digit = NumpadDigitRegex.Match(v);
        if (digit.Success)
        {
            return "<Keyboard>/numpad" + digit.Groups[1].Value;
        }

        if (v.Contains("Enter", StringComparison.OrdinalIgnoreCase) ||
            v.Contains("Return", StringComparison.OrdinalIgnoreCase))
        {
            return "<Keyboard>/numpadEnter";
        }

        if (v.Contains("Plus", StringComparison.OrdinalIgnoreCase) || v.Contains("+"))
        {
            return "<Keyboard>/numpadPlus";
        }

        if (v.Contains("Minus", StringComparison.OrdinalIgnoreCase) || v.Contains("-"))
        {
            return "<Keyboard>/numpadMinus";
        }

        if (v.Contains("Multiply", StringComparison.OrdinalIgnoreCase) || v.Contains("*"))
        {
            return "<Keyboard>/numpadMultiply";
        }

        if (v.Contains("Divide", StringComparison.OrdinalIgnoreCase) || v.Contains("/"))
        {
            return "<Keyboard>/numpadDivide";
        }

        if (v.Contains("Period", StringComparison.OrdinalIgnoreCase) || v.Contains("."))
        {
            return "<Keyboard>/numpadPeriod";
        }

        if (v.Contains("Equals", StringComparison.OrdinalIgnoreCase) || v.Contains("="))
        {
            return "<Keyboard>/numpadEquals";
        }

        if (v.Contains("NumLock", StringComparison.OrdinalIgnoreCase) || v.Contains("Num Lock", StringComparison.OrdinalIgnoreCase))
        {
            return "<Keyboard>/numLock";
        }

        return v;
    }
}

internal static class MouseSidePressText
{
    private static readonly Regex MouseButtonRegex = new(
        @"(?ix)\b(?:mb|mouse)\s*([45])\b");

    private static readonly Regex XButtonRegex = new(
        @"(?ix)\bxbutton\s*([12])\b");

    public static bool IsMouseSideRelated(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string v = value.Trim();
        return v.Contains("<Mouse>/backButton", StringComparison.OrdinalIgnoreCase)
            || v.Contains("<Mouse>/forwardButton", StringComparison.OrdinalIgnoreCase)
            || v.Contains("backButton", StringComparison.OrdinalIgnoreCase)
            || v.Contains("forwardButton", StringComparison.OrdinalIgnoreCase)
            || MouseButtonRegex.IsMatch(v)
            || XButtonRegex.IsMatch(v);
    }

    public static bool IsMouseSideControlPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string v = value.Trim();
        if (!v.Contains("<Mouse>/", StringComparison.OrdinalIgnoreCase) && !v.StartsWith("/backButton", StringComparison.OrdinalIgnoreCase) &&
            !v.StartsWith("/forwardButton", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return v.Contains("backButton", StringComparison.OrdinalIgnoreCase) || v.Contains("forwardButton", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeToMouseControlPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string v = value.Trim();
        if (v.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase))
        {
            return v;
        }

        if (v.StartsWith("/backButton", StringComparison.OrdinalIgnoreCase))
        {
            return "<Mouse>" + v;
        }

        if (v.StartsWith("/forwardButton", StringComparison.OrdinalIgnoreCase))
        {
            return "<Mouse>" + v;
        }

        if (v.Contains("backButton", StringComparison.OrdinalIgnoreCase))
        {
            return "<Mouse>/backButton";
        }

        if (v.Contains("forwardButton", StringComparison.OrdinalIgnoreCase))
        {
            return "<Mouse>/forwardButton";
        }

        Match mb = MouseButtonRegex.Match(v);
        if (mb.Success)
        {
            return mb.Groups[1].Value == "4" ? "<Mouse>/backButton" : "<Mouse>/forwardButton";
        }

        Match xb = XButtonRegex.Match(v);
        if (xb.Success)
        {
            return xb.Groups[1].Value == "1" ? "<Mouse>/backButton" : "<Mouse>/forwardButton";
        }

        return v;
    }

    public static string ToDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string v = value.Trim();
        if (v.Contains("backButton", StringComparison.OrdinalIgnoreCase) ||
            MouseButtonRegex.IsMatch(v) && MouseButtonRegex.Match(v).Groups[1].Value == "4" ||
            XButtonRegex.IsMatch(v) && XButtonRegex.Match(v).Groups[1].Value == "1")
        {
            return "MB4";
        }

        if (v.Contains("forwardButton", StringComparison.OrdinalIgnoreCase) ||
            MouseButtonRegex.IsMatch(v) && MouseButtonRegex.Match(v).Groups[1].Value == "5" ||
            XButtonRegex.IsMatch(v) && XButtonRegex.Match(v).Groups[1].Value == "2")
        {
            return "MB5";
        }

        return v;
    }
}

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

internal static class PatchedDisplay
{
    public static InputDisData GetBindingDisData(string bindingKey)
    {
        if (MouseSidePressText.IsMouseSideRelated(bindingKey) || MouseSidePressText.IsMouseSideControlPath(bindingKey))
        {
            string normalizedKey = MouseSidePressText.NormalizeToMouseControlPath(bindingKey);

            // Prefer a known-safe base lookup and keep PressTxt as the logical remap key.
            try
            {
                InputDisData baseData = EscapeGame.UIGen.KeyboardBindingHelper.GetInputDisData("Enter");
                if (baseData != null)
                {
                    return InputDisDataClone.CloneWithPressText(baseData, normalizedKey);
                }
            }
            catch
            {
                // ignore
            }

            return InputDisDataClone.CreateMinimal(normalizedKey);
        }

        return NumpadDisplay.GetBindingDisData(bindingKey);
    }
}

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

internal static class RebindingUiState
{
    public static bool IsRebinding;
    public static int ActiveCnfId;
    public static IntPtr ActiveKeyboardPtr;

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
}

internal static class KeyboardUiSafe
{
    public static bool TryApplyBindingTextOnly(EscapeGame.UIGen.Keyboard keyboard, string display)
    {
        if (keyboard == null || string.IsNullOrWhiteSpace(display))
        {
            return false;
        }

        bool changed = false;

        try
        {
            // The binding area is a sub-button (field name: "list") that normally shows either:
            // - an icon (loader), or
            // - a title text such as "输入任意键".
            // We must NOT touch the left action label.
            GButton bindingBtn = keyboard.list;
            if (bindingBtn != null)
            {
                bindingBtn.title = display;
                try
                {
                    // Ensure icon mode is cleared too; otherwise the old key icon/text may keep rendering.
                    bindingBtn.icon = string.Empty;
                    bindingBtn.selectedIcon = string.Empty;
                }
                catch
                {
                    // ignore
                }
                changed = true;
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            // Some states render an overlay text on the root row button itself (e.g. "输入任意键").
            // Clear it to avoid double-text overlap when we draw our own binding text.
            // Keep the root row title intact; it can participate in selection/animation state.
            // Only the binding button (keyboard.list) should be overridden.
            changed = true;
        }
        catch
        {
            // ignore
        }

        try
        {
            GLoader loader = keyboard.Btn;
            if (loader != null)
            {
                // Hide icon mode while we show plain text, but keep the underlying url intact.
                // This allows switching back to native-supported keys without ending up blank.
                loader.visible = false;
                changed = true;
            }
        }
        catch
        {
            // ignore
        }

        return changed;
    }

    public static void TryRestoreIconMode(EscapeGame.UIGen.Keyboard keyboard)
    {
        try
        {
            // Clear any leftover custom binding text from numpad mode.
            TryClearBindingText(keyboard);

            GLoader loader = keyboard?.Btn;
            if (loader != null)
            {
                loader.visible = true;
            }
        }
        catch
        {
            // ignore
        }
    }

    public static void TryPrepareRebindingVisual(EscapeGame.UIGen.Keyboard keyboard)
    {
        try
        {
            keyboard?.CleanCurrentBtn();
        }
        catch
        {
            // ignore
        }

        // Clear any custom binding text so it doesn't stack with the game's "输入任意键" prompt.
        TryClearBindingText(keyboard);
    }

    public static bool TryApplyNumpadOverrideTextOnly(EscapeGame.UIGen.Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return false;
        }

        cfg.UI_SettingKeyboard setting = keyboard._tbSettingKeyboard;
        if (setting == null || setting.Id == 0)
        {
            return false;
        }

        if (RebindingUiState.IsActiveFor(keyboard) || RebindingUiState.IsActiveFor(setting.Id))
        {
            // While rebinding, the UI intentionally shows "输入任意键" with animations.
            // Do not draw our override text on top of that.
            return false;
        }

        string overrideKey;
        try
        {
            overrideKey = EscapeGame.UIGen.KeyboardBindingHelper.GetOverride(setting.Id);
        }
        catch
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(overrideKey))
        {
            return false;
        }

        if (!PatchedPressText.IsPatchedControlPath(overrideKey) && !PatchedPressText.IsPatchedRelated(overrideKey))
        {
            return false;
        }

        return TryApplyBindingTextOnly(keyboard, PatchedPressText.ToDisplayName(overrideKey));
    }

    public static void TryClearBindingText(EscapeGame.UIGen.Keyboard keyboard)
    {
        try
        {
            GButton bindingBtn = keyboard?.list;
            if (bindingBtn != null)
            {
                bindingBtn.title = string.Empty;
            }
        }
        catch
        {
            // ignore
        }
    }
}

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

internal static class InputDisDataClone
{
    public static InputDisData CloneWithPressText(InputDisData source, string pressTxt)
    {
        IntPtr ptr = IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<InputDisData>.NativeClassPtr);
        InputDisData clone = new(ptr)
        {
            InputID = source.InputID,
            ClickImage = source.ClickImage,
            PressBotImage = source.PressBotImage,
            PressMidImage = source.PressMidImage,
            PressTopImage = source.PressTopImage,
            PressTxt = pressTxt,
        };
        return clone;
    }

    public static InputDisData CreateMinimal(string pressTxt)
    {
        IntPtr ptr = IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<InputDisData>.NativeClassPtr);
        InputDisData data = new(ptr)
        {
            InputID = "Numpad",
            ClickImage = string.Empty,
            PressBotImage = string.Empty,
            PressMidImage = string.Empty,
            PressTopImage = string.Empty,
            PressTxt = pressTxt,
        };
        return data;
    }
}

[HarmonyPatch(typeof(EscapeGame.UIGen.KeyboardBindingHelper), nameof(EscapeGame.UIGen.KeyboardBindingHelper.SaveOverride))]
internal static class Patch_KeyboardBindingHelper_SaveOverride
{
    private static void Prefix(int cnfId, string inputId)
    {
        // Useful to learn what the game stores as "inputId" for normal keys vs numpad.
        if (NumpadRebindPlugin.LogSource == null)
        {
            return;
        }

        if (PatchedPressText.IsPatchedRelated(inputId) || PatchedPressText.IsPatchedControlPath(inputId))
        {
            NumpadRebindPlugin.LogSource.LogInfo($"SaveOverride cnfId={cnfId} inputId=\"{inputId}\"");
        }
        else
        {
            NumpadRebindPlugin.LogSource.LogDebug($"SaveOverride cnfId={cnfId} inputId=\"{inputId}\"");
        }
    }
}

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

[HarmonyPatch(typeof(EscapeGame.Input.InputBindingHelper), nameof(EscapeGame.Input.InputBindingHelper.SwapBindingPreHookFilterKey))]
internal static class Patch_InputBindingHelper_SwapBindingPreHookFilterKey
{
    private static void Postfix(string inputRedirectPresetsKey, cfg.TbCfg.InputDeviceType deviceType, ref Il2CppSystem.Collections.Generic.List<string> __result)
    {
        if (__result == null || __result.Count == 0)
        {
            return;
        }

        int removed = 0;
        for (int i = __result.Count - 1; i >= 0; i--)
        {
            string item = __result[i];
            if (PatchedPressText.IsPatchedRelated(item) || PatchedPressText.IsPatchedControlPath(item))
            {
                __result.RemoveAt(i);
                removed++;
            }
        }

        if (removed > 0)
        {
            NumpadRebindPlugin.LogSource?.LogInfo($"Removed {removed} numpad filter entries from SwapBindingPreHookFilterKey(\"{inputRedirectPresetsKey}\", {deviceType}).");
        }
    }
}

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
        // If it blocks numpad, force-allow.
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

            NumpadRebindPlugin.LogSource?.LogWarning($"Cannot salvage numpad binding for \"{formatKey}\" (missing UI state).");
        }
        catch (Exception ex)
        {
            NumpadRebindPlugin.LogSource?.LogWarning($"Failed to salvage numpad binding for \"{formatKey}\": {ex.GetType().Name}: {ex.Message}");
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

        try
        {
            // The game rejects numpad here (returns false) and therefore does NOT call its rebinding pipeline.
            // We need to:
            // - apply rebinding to the actual InputAction(s)
            // - persist overrides
            // - update the UI item if possible
            cfg.UI_SettingKeyboard setting = keyboard._tbSettingKeyboard;
            if (setting == null)
            {
                NumpadRebindPlugin.LogSource?.LogWarning($"Cannot salvage numpad binding (setting is null) for \"{formatKey}\".");
                return;
            }

            int cnfId = setting.Id;
            if (cnfId == 0)
            {
                NumpadRebindPlugin.LogSource?.LogWarning($"Cannot salvage numpad binding (cnfId=0) for \"{formatKey}\".");
                return;
            }

            string inputActionName = keyboard._inputActionName ?? string.Empty;
            string originKey = keyboard._originBindingBtn ?? string.Empty;
            bool applyAllMaps = setting.Action != null && setting.Action.IsAllOverride;

            // Conflict resolution: if another action already uses this key, reset it first
            // (mimics the game's own behavior for non-numpad keys).
            try
            {
                bool isComposite;
                EscapeGame.UIGen.Keyboard already = __instance.GetAlreadyKeyboard(formatKey, out isComposite);
                if (already != null && already.Pointer != keyboard.Pointer)
                {
                    int alreadyId = 0;
                    try { alreadyId = already._tbSettingKeyboard?.Id ?? 0; } catch { /* ignore */ }
                    NumpadRebindPlugin.LogSource?.LogInfo(
                        $"Key conflict: \"{formatKey}\" is already used by cnfId={alreadyId}. Resetting the existing binding first.");
                    __instance.ResetKeyboard(formatKey, already);
                }
            }
            catch (Exception exConflict)
            {
                NumpadRebindPlugin.LogSource?.LogDebug($"Conflict resolution failed: {exConflict.GetType().Name}: {exConflict.Message}");
            }

            // Apply to the game's runtime input system.
            // Use both overloads to cover cases where the game routes through originKey-only mapping.
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

            // Persist + broadcast mapping.
            var map = new Il2CppSystem.Collections.Generic.Dictionary<int, string>();
            map.Add(cnfId, formatKey);

            EscapeGame.UIGen.KeyboardBindingHelper.SaveOverride(cnfId, formatKey);
            EscapeGame.UIGen.KeyboardBindingHelper.FlushRemapKeyboard(map);

            // Keep SettingsMenuKeyboard's internal key->row mapping in sync (prevents selection glitches).
            try
            {
                __instance._View?.ResetCurrentKeyboard(formatKey, keyboard);
            }
            catch (Exception exMap)
            {
                NumpadRebindPlugin.LogSource?.LogDebug($"ResetCurrentKeyboard failed: {exMap.GetType().Name}: {exMap.Message}");
            }

            // End rebinding state (otherwise UI can get stuck on "输入任意键").
            try
            {
                __instance.SwitchRebindingListen(false);
                __instance._bindingState = EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl.RebindingState.Origin;
            }
            catch (Exception exState)
            {
                NumpadRebindPlugin.LogSource?.LogDebug($"Failed to exit rebinding state: {exState.GetType().Name}: {exState.Message}");
            }

            // Refresh the current UI item safely (text-only), avoiding missing-sprite exceptions.
            try
            {
                keyboard?.CleanCurrentBtn();
            }
            catch
            {
                // ignore
            }
            KeyboardUiSafe.TryApplyBindingTextOnly(keyboard, PatchedPressText.ToDisplayName(formatKey));

            NumpadRebindPlugin.LogSource?.LogInfo(
                $"Applied numpad binding: cnfId={cnfId} action=\"{inputActionName}\" origin=\"{originKey}\" new=\"{formatKey}\" allMaps={applyAllMaps} playUx={playUx}");
            __result = true;
        }
        catch (Exception ex)
        {
            NumpadRebindPlugin.LogSource?.LogWarning($"Failed to salvage numpad binding for \"{formatKey}\": {ex.GetType().Name}: {ex.Message}");
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

            // Avoid double-text overlap: if the current row previously displayed our numpad text
            // (on the binding button), clear it while the game shows "输入任意键".
            KeyboardUiSafe.TryPrepareRebindingVisual(current);
        }
        catch
        {
            // ignore
        }
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

[HarmonyPatch(typeof(EscapeGame.UIGen.KeyboardBindingHelper), nameof(EscapeGame.UIGen.KeyboardBindingHelper.ContainsKey))]
internal static class Patch_KeyboardBindingHelper_ContainsKey
{
    private static bool Prefix(string pressTxt, ref bool __result)
    {
        if (!PatchedPressText.IsPatchedRelated(pressTxt) && !PatchedPressText.IsPatchedControlPath(pressTxt))
        {
            return true;
        }

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(EscapeGame.UIGen.KeyboardBindingHelper), nameof(EscapeGame.UIGen.KeyboardBindingHelper.GetInputDisData))]
internal static class Patch_KeyboardBindingHelper_GetInputDisData
{
    private static bool Prefix(ref string pressTxt, ref string __state)
    {
        try
        {
            __state = pressTxt;
            if (!PatchedPressText.IsPatchedRelated(pressTxt) && !PatchedPressText.IsPatchedControlPath(pressTxt))
            {
                return true;
            }

            if (MouseSidePressText.IsMouseSideRelated(pressTxt) || MouseSidePressText.IsMouseSideControlPath(pressTxt))
            {
                // Use a safe base entry so the UI can render without relying on a mouse icon table entry.
                pressTxt = "Enter";
                return true;
            }

            if (!NumpadPressText.TryMapToNonNumpadLookup(pressTxt, out string mapped))
            {
                return true;
            }

            pressTxt = mapped;
            return true;
        }
        catch (Exception ex)
        {
            NumpadRebindPlugin.LogSource?.LogDebug($"GetInputDisData Prefix failed: {ex.GetType().Name}: {ex.Message}");
            return true;
        }
    }

    private static void Postfix(string __state, ref InputDisData __result)
    {
        try
        {
            string normalizedKey = __state;
            if (PatchedPressText.IsPatchedRelated(__state) || PatchedPressText.IsPatchedControlPath(__state))
            {
                normalizedKey = PatchedPressText.NormalizeToControlPath(__state);
            }

            if (__result == null)
            {
                if (PatchedPressText.IsPatchedRelated(__state) || PatchedPressText.IsPatchedControlPath(__state))
                {
                    // Avoid hard failure in UI if their table has no entry for a given key.
                    __result = InputDisDataClone.CreateMinimal(normalizedKey);
                }

                return;
            }

            if (!PatchedPressText.IsPatchedRelated(__state) && !PatchedPressText.IsPatchedControlPath(__state))
            {
                return;
            }

            // Return a cloned entry so we don't mutate shared table objects.
            __result = InputDisDataClone.CloneWithPressText(__result, normalizedKey);
        }
        catch (Exception ex)
        {
            NumpadRebindPlugin.LogSource?.LogDebug($"GetInputDisData Postfix failed (state=\"{__state}\"): {ex.GetType().Name}: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(EscapeGame.UIGen.Keyboard), nameof(EscapeGame.UIGen.Keyboard.SetData))]
internal static class Patch_Keyboard_SetData
{
    private static void Prefix(cfg.UI_SettingKeyboard settingData, ref InputDisData bindingData)
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
    private static bool Prefix(EscapeGame.UIGen.Keyboard __instance, ref InputDisData changeData, ref string __state)
    {
        __state = string.Empty;
        try
        {
            cfg.UI_SettingKeyboard setting = __instance._tbSettingKeyboard;
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

            cfg.UI_SettingKeyboard setting = __instance._tbSettingKeyboard;
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

    private static Exception Finalizer(EscapeGame.UIGen.Keyboard __instance, InputDisData changeData, string __state, Exception __exception)
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
                    cfg.UI_SettingKeyboard setting = __instance?._tbSettingKeyboard;
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
