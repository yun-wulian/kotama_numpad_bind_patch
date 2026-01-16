using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace Kotama.NumpadRebind;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class NumpadRebindPlugin : BasePlugin
{
    public const string PluginGuid = "com.yunwulian.kotama.numpad-rebind";
    public const string PluginName = "Kotama Numpad Rebind";
    public const string PluginVersion = "0.3.0";

    internal static ManualLogSource LogSource;

    public override void Load()
    {
        LogSource = Log;

        Harmony harmony = new(PluginGuid);
        harmony.PatchAll(typeof(NumpadRebindPlugin).Assembly);

        Log.LogInfo($"Loaded v{PluginVersion}. Strategy2 patches active (InputSystem rebinding + UI binding display).");
    }
}

