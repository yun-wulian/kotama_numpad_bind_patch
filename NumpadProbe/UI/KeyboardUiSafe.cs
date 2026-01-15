using FairyGUI;

namespace Kotama.NumpadRebind;

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

