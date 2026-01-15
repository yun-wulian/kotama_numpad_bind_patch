using System;
using cfg.TbCfg;

namespace Kotama.NumpadRebind;

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

