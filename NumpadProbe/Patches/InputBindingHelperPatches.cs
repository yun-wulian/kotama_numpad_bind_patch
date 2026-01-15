using HarmonyLib;

namespace Kotama.NumpadRebind;

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

