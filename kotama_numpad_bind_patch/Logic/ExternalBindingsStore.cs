using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BepInEx;

namespace Kotama.NumpadRebind;

internal static class ExternalBindingsStore
{
    private sealed class ExternalBindingsFile
    {
        public int Version { get; set; } = 1;
        public List<Entry> Entries { get; set; } = new();
    }

    internal sealed class Entry
    {
        public int CnfId { get; set; }
        public string Key { get; set; }
        public string InputActionName { get; set; }
        public string OriginKey { get; set; }
        public bool ApplyAllMaps { get; set; }
    }

    private static readonly object Gate = new();
    private static Dictionary<int, Entry> _byCnfId;
    private static bool _loaded;

    private static bool TryNormalizeKey(string rawKey, out string normalizedKey)
    {
        normalizedKey = null;
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return false;
        }

        string v = InputPathUtil.NormalizeControlPath(rawKey);
        if (string.Equals(v, "Numpad", StringComparison.OrdinalIgnoreCase))
        {
            // Ambiguous value with no digit/operator. Cannot be safely replayed.
            return false;
        }

        if (InputPathUtil.IsAnyKeyPath(v))
        {
            return false;
        }

        if (v.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) ||
            v.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedKey = v;
            return true;
        }

        if (PatchedPressText.IsPatchedRelated(v) || PatchedPressText.IsPatchedControlPath(v))
        {
            string path = PatchedPressText.NormalizeToControlPath(v);
            if (string.IsNullOrWhiteSpace(path) ||
                string.Equals(path, "Numpad", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase))
            {
                normalizedKey = path;
                return true;
            }
        }

        // External store is intended for replaying InputSystem control paths.
        return false;
    }

    public static string GetStorePath()
    {
        return Path.Combine(Paths.ConfigPath, "com.yunwulian.kotama.numpad-rebind.bindings.json");
    }

    public static bool TryGet(int cnfId, out Entry entry)
    {
        entry = null;
        if (cnfId == 0)
        {
            return false;
        }

        EnsureLoaded();
        lock (Gate)
        {
            return _byCnfId.TryGetValue(cnfId, out entry) &&
                entry != null &&
                !string.IsNullOrWhiteSpace(entry.Key);
        }
    }

    public static IReadOnlyCollection<Entry> GetAll()
    {
        EnsureLoaded();
        lock (Gate)
        {
            return _byCnfId.Values.ToArray();
        }
    }

    public static void Upsert(Entry entry)
    {
        if (entry == null || entry.CnfId == 0 || string.IsNullOrWhiteSpace(entry.Key))
        {
            return;
        }

        if (!TryNormalizeKey(entry.Key, out string normalizedKey))
        {
            NumpadRebindPlugin.LogSource?.LogWarning(
                $"External binding not saved (invalid key): cnfId={entry.CnfId} action=\"{entry.InputActionName}\" origin=\"{entry.OriginKey}\" key=\"{entry.Key}\"");
            return;
        }

        entry.Key = normalizedKey;

        EnsureLoaded();
        lock (Gate)
        {
            _byCnfId[entry.CnfId] = entry;
        }

        NumpadRebindPlugin.LogSource?.LogInfo(
            $"External binding saved: cnfId={entry.CnfId} action=\"{entry.InputActionName}\" origin=\"{entry.OriginKey}\" new=\"{entry.Key}\" allMaps={entry.ApplyAllMaps}");

        Save();
    }

    public static void Remove(int cnfId)
    {
        if (cnfId == 0)
        {
            return;
        }

        EnsureLoaded();
        bool removed;
        lock (Gate)
        {
            removed = _byCnfId.Remove(cnfId);
        }

        if (removed)
        {
            NumpadRebindPlugin.LogSource?.LogInfo($"External binding removed: cnfId={cnfId}");
            Save();
        }
    }

    private static void EnsureLoaded()
    {
        lock (Gate)
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            _byCnfId = new Dictionary<int, Entry>();
        }

        bool changed = false;
        try
        {
            string path = GetStorePath();
            if (!File.Exists(path))
            {
                return;
            }

            string json = File.ReadAllText(path);
            ExternalBindingsFile file = JsonSerializer.Deserialize<ExternalBindingsFile>(json);
            if (file?.Entries == null)
            {
                return;
            }

            lock (Gate)
            {
                foreach (Entry entry in file.Entries)
                {
                    if (entry == null || entry.CnfId == 0 || string.IsNullOrWhiteSpace(entry.Key))
                    {
                        continue;
                    }

                    if (!TryNormalizeKey(entry.Key, out string normalizedKey))
                    {
                        changed = true;
                        NumpadRebindPlugin.LogSource?.LogWarning(
                            $"ExternalBindingsStore: dropping invalid entry cnfId={entry.CnfId} key=\"{entry.Key}\"");
                        continue;
                    }

                    if (!string.Equals(entry.Key, normalizedKey, StringComparison.Ordinal))
                    {
                        entry.Key = normalizedKey;
                        changed = true;
                    }

                    _byCnfId[entry.CnfId] = entry;
                }
            }
        }
        catch (Exception ex)
        {
            NumpadRebindPlugin.LogSource?.LogWarning($"ExternalBindingsStore load failed: {ex.GetType().Name}: {ex.Message}");
        }

        if (changed)
        {
            Save();
        }
    }

    private static void Save()
    {
        try
        {
            ExternalBindingsFile file;
            lock (Gate)
            {
                file = new ExternalBindingsFile
                {
                    Version = 1,
                    Entries = _byCnfId.Values
                        .Where(e => e != null && e.CnfId != 0 && !string.IsNullOrWhiteSpace(e.Key))
                        .OrderBy(e => e.CnfId)
                        .ToList()
                };
            }

            string json = JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true });
            string path = GetStorePath();

            string dir = Path.GetDirectoryName(path) ?? Paths.ConfigPath;
            Directory.CreateDirectory(dir);

            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tmp, path);
        }
        catch (Exception ex)
        {
            NumpadRebindPlugin.LogSource?.LogWarning($"ExternalBindingsStore save failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
