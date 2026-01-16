using System;

namespace Kotama.NumpadRebind;

internal static class ExternalBindingsApplier
{
    public static void ApplyAll(string reason)
    {
        try
        {
            int applied = 0;
            foreach (ExternalBindingsStore.Entry entry in ExternalBindingsStore.GetAll())
            {
                if (entry == null || entry.CnfId == 0 || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                string key = entry.Key;
                if (PatchedPressText.IsPatchedRelated(key) || PatchedPressText.IsPatchedControlPath(key))
                {
                    key = PatchedPressText.NormalizeToControlPath(key);
                }

                if (string.IsNullOrWhiteSpace(key) ||
                    string.Equals(key, "Numpad", StringComparison.OrdinalIgnoreCase) ||
                    !(key.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) || key.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase)))
                {
                    NumpadRebindPlugin.LogSource?.LogWarning(
                        $"External apply skipped (invalid key): cnfId={entry.CnfId} action=\"{entry.InputActionName}\" origin=\"{entry.OriginKey}\" key=\"{entry.Key}\"");
                    continue;
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(entry.InputActionName))
                    {
                        EscapeGame.Input.InputManager.RebindingBtn(
                            entry.InputActionName,
                            entry.OriginKey ?? string.Empty,
                            key,
                            EscapeGame.Utils.Enum.ActionMapTypes.PlayerControls,
                            entry.ApplyAllMaps);
                    }

                    EscapeGame.Input.InputManager.RebindingBtn(entry.OriginKey ?? string.Empty, key);
                    applied++;
                }
                catch (Exception exOne)
                {
                    NumpadRebindPlugin.LogSource?.LogWarning(
                        $"External apply failed: cnfId={entry.CnfId} action=\"{entry.InputActionName}\" origin=\"{entry.OriginKey}\" new=\"{key}\": {exOne.GetType().Name}: {exOne.Message}");
                }
            }

            if (applied > 0)
            {
                NumpadRebindPlugin.LogSource?.LogInfo($"External bindings applied: {applied} ({reason})");
            }
        }
        catch (Exception ex)
        {
            NumpadRebindPlugin.LogSource?.LogWarning($"External bindings apply failed ({reason}): {ex.GetType().Name}: {ex.Message}");
        }
    }
}
