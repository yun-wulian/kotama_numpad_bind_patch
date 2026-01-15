using System;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using cfg.TbCfg;

namespace Kotama.NumpadRebind;

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

