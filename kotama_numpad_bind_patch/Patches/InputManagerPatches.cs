using HarmonyLib;

namespace Kotama.NumpadRebind;

[HarmonyPatch(typeof(EscapeGame.Input.InputManager), nameof(EscapeGame.Input.InputManager.Init))]
internal static class Patch_InputManager_Init
{
    private static void Postfix()
    {
        ExternalBindingsApplier.ApplyAll("InputManager.Init");
    }
}

[HarmonyPatch(typeof(EscapeGame.Input.InputManager), nameof(EscapeGame.Input.InputManager.ReloadInputEvents))]
internal static class Patch_InputManager_ReloadInputEvents
{
    private static void Postfix()
    {
        ExternalBindingsApplier.ApplyAll("InputManager.ReloadInputEvents");
    }
}

