using System;
using cfg.TbCfg;
using HarmonyLib;

namespace Kotama.NumpadRebind;

[HarmonyPatch(typeof(EscapeGame.UIGen.KeyboardBindingHelper), nameof(EscapeGame.UIGen.KeyboardBindingHelper.SaveOverride))]
internal static class Patch_KeyboardBindingHelper_SaveOverride
{
    private static void Prefix(int cnfId, string inputId)
    {
        // Useful to learn what the game stores as "inputId" for normal keys vs numpad/mouse side.
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

            // Some values (e.g. "Numpad") can map to an empty string; never pass empty to the game's lookup.
            if (string.IsNullOrWhiteSpace(mapped))
            {
                mapped = "Enter";
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
                    // Avoid UI crash: ensure we return an entry that has a valid sprite resource.
                    // Use a guaranteed-safe base entry ("Enter") and override only the PressTxt.
                    InputDisData baseData = null;
                    try { baseData = EscapeGame.UIGen.KeyboardBindingHelper.GetInputDisData("Enter"); } catch { /* ignore */ }
                    if (baseData != null)
                    {
                        __result = InputDisDataClone.CloneWithPressText(baseData, normalizedKey);
                    }
                    else
                    {
                        __result = InputDisDataClone.CreateMinimal(normalizedKey);
                    }
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

[HarmonyPatch(typeof(EscapeGame.UIGen.KeyboardBindingHelper), nameof(EscapeGame.UIGen.KeyboardBindingHelper.GetOverride))]
internal static class Patch_KeyboardBindingHelper_GetOverride
{
    private static void Postfix(int cnfId, ref string __result)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(__result))
            {
                return;
            }

            if (ExternalBindingsStore.TryGet(cnfId, out ExternalBindingsStore.Entry entry) &&
                entry != null &&
                !string.IsNullOrWhiteSpace(entry.Key))
            {
                __result = entry.Key;
            }
        }
        catch
        {
            // ignore
        }
    }
}

