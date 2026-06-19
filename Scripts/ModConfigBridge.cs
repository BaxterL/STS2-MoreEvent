using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace MoreEvent.Scripts;

internal static class ModConfigBridge
{
    private static bool _available;
    private static bool _registered;
    private static Type? _apiType;
    private static Type? _entryType;
    private static Type? _configTypeEnum;

    internal static bool IsAvailable => _available;

    internal static void DeferredRegister()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.ProcessFrame += OnNextFrame;
    }

    private static void OnNextFrame()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.ProcessFrame -= OnNextFrame;
        Detect();
        if (_available) Register();
    }

    private static void Detect()
    {
        try
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .ToArray();

            _apiType = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ModConfigApi");
            _entryType = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ConfigEntry");
            _configTypeEnum = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ConfigType");
            _available = _apiType != null && _entryType != null && _configTypeEnum != null;
        }
        catch
        {
            _available = false;
        }
    }

    private static void Register()
    {
        if (_registered) return;
        _registered = true;

        try
        {
            var entries = BuildEntries();

            var displayNames = new Dictionary<string, string>
            {
                ["en"] = "MoreEvent",
                ["zhs"] = "更多事件",
            };

            var registerMethod = _apiType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "Register")
                .OrderByDescending(m => m.GetParameters().Length)
                .First();

            if (registerMethod.GetParameters().Length == 4)
            {
                registerMethod.Invoke(null, new object[]
                {
                    "MoreEvent",
                    displayNames["en"],
                    displayNames,
                    entries
                });
            }
            else
            {
                registerMethod.Invoke(null, new object[]
                {
                    "MoreEvent",
                    displayNames["en"],
                    entries
                });
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[MoreEvent] ModConfig registration failed: {e}");
        }
    }

    internal static T GetValue<T>(string key, T fallback)
    {
        if (!_available) return fallback;
        try
        {
            var result = _apiType!.GetMethod("GetValue", BindingFlags.Public | BindingFlags.Static)
                ?.MakeGenericMethod(typeof(T))
                ?.Invoke(null, new object[] { "MoreEvent", key });
            return result != null ? (T)result : fallback;
        }
        catch { return fallback; }
    }

    internal static void SetValue(string key, object value)
    {
        if (!_available) return;
        try
        {
            _apiType!.GetMethod("SetValue", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new object[] { "MoreEvent", key, value });
        }
        catch { }
    }

    private static Array BuildEntries()
    {
        var list = new List<object>();

        list.Add(MakeEntry(cfg =>
        {
            Set(cfg, "Label", "Unknown Node Count");
            Set(cfg, "Labels", L("Unknown Node Count", "未知节点数量"));
            Set(cfg, "Type", EnumVal("Header"));
        }));

        list.Add(MakeEntry(cfg =>
        {
            Set(cfg, "Key", "unknownNodeMultiplier");
            Set(cfg, "Label", "Multiplier");
            Set(cfg, "Labels", L("Multiplier", "倍率"));
            Set(cfg, "Type", EnumVal("Slider"));
            Set(cfg, "DefaultValue", (object)2.0f);
            Set(cfg, "Min", 0.5f);
            Set(cfg, "Max", 5.0f);
            Set(cfg, "Step", 0.5f);
            Set(cfg, "Format", "F1");
            Set(cfg, "Description", "Multiplier for the number of ? event nodes on the map");
            Set(cfg, "Descriptions", L("Multiplier for the number of ? event nodes on the map", "地图上 ? 事件节点数量的倍率"));
            Set(cfg, "OnChanged", new Action<object>(Entry.SetUnknownNodeMultiplier));
        }));

        list.Add(MakeEntry(cfg =>
        {
            Set(cfg, "Label", "Monster Odds");
            Set(cfg, "Labels", L("Monster Odds", "怪物概率"));
            Set(cfg, "Type", EnumVal("Header"));
        }));

        list.Add(MakeEntry(cfg =>
        {
            Set(cfg, "Key", "monsterOdds");
            Set(cfg, "Label", "Monster Odds");
            Set(cfg, "Labels", L("Monster Odds", "怪物概率"));
            Set(cfg, "Type", EnumVal("Slider"));
            Set(cfg, "DefaultValue", (object)0.0f);
            Set(cfg, "Min", 0.0f);
            Set(cfg, "Max", 1.0f);
            Set(cfg, "Step", 0.1f);
            Set(cfg, "Format", "P0");
            Set(cfg, "Description", "Chance of revealing monsters on ? nodes (0 = all events)");
            Set(cfg, "Descriptions", L("Chance of revealing monsters on ? nodes (0 = all events)", "? 节点揭示为怪物的概率 (0 = 全为事件)"));
            Set(cfg, "OnChanged", new Action<object>(Entry.SetMonsterOdds));
        }));

        var result = Array.CreateInstance(_entryType!, list.Count);
        for (int i = 0; i < list.Count; i++)
            result.SetValue(list[i], i);
        return result;
    }

    private static object MakeEntry(Action<object> configure)
    {
        var inst = Activator.CreateInstance(_entryType!)!;
        configure(inst);
        return inst;
    }

    private static void Set(object obj, string name, object value)
        => obj.GetType().GetProperty(name)?.SetValue(obj, value);

    private static Dictionary<string, string> L(string en, string zhs)
        => new() { ["en"] = en, ["zhs"] = zhs };

    private static object EnumVal(string name)
        => Enum.Parse(_configTypeEnum!, name);
}
