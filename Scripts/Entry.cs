using System.IO;
using System.Reflection;
using System.Text.Json;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rooms;

namespace MoreEvent.Scripts;

public class MoreEventConfig
{
    public double UnknownNodeMultiplier { get; set; } = 2.0;
    public double MonsterOdds { get; set; } = 0.0;
}

[HarmonyPatch]
public static class Patch_MoreUnknownNodes
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(MapPointTypeCounts), "StandardRandomUnknownCount");
    }

    static void Postfix(ref int __result)
    {
        if (Entry.Config == null) return;
        __result = (int)(__result * Entry.Config.UnknownNodeMultiplier);
    }
}

[HarmonyPatch]
public static class Patch_FavorEvents
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(UnknownMapPointOdds), "Roll");
    }

    static void Prefix(UnknownMapPointOdds __instance)
    {
        if (Entry.Config == null) return;
        __instance.MonsterOdds = (float)Entry.Config.MonsterOdds;
    }
}

[ModInitializer("Init")]
public class Entry
{
    public static MoreEventConfig? Config { get; private set; }

    public static void Init()
    {
        LoadConfig();
        SyncFromModConfig();
        ModConfigBridge.DeferredRegister();
        var harmony = new Harmony("sts2.moreevent");
        harmony.PatchAll();
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
        Log.Debug($"MoreEvent initialized! unknownMultiplier={Config?.UnknownNodeMultiplier}, monsterOdds={Config?.MonsterOdds}");
    }

    private static void LoadConfig()
    {
        try
        {
            var dir = Path.GetDirectoryName(typeof(Entry).Assembly.Location);
            var path = Path.Combine(dir!, "MoreEvent.cfg");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                Config = JsonSerializer.Deserialize<MoreEventConfig>(json) ?? new MoreEventConfig();
            }
            else
            {
                Config = new MoreEventConfig();
            }
        }
        catch
        {
            Config = new MoreEventConfig();
        }
    }

    internal static void SyncFromModConfig()
    {
        if (!ModConfigBridge.IsAvailable) return;
        Config ??= new MoreEventConfig();
        Config.UnknownNodeMultiplier = ModConfigBridge.GetValue("unknownNodeMultiplier", (float)Config.UnknownNodeMultiplier);
        Config.MonsterOdds = ModConfigBridge.GetValue("monsterOdds", (float)Config.MonsterOdds);
    }

    internal static void SetUnknownNodeMultiplier(object val)
    {
        if (Config != null)
            Config.UnknownNodeMultiplier = Convert.ToSingle(val);
    }

    internal static void SetMonsterOdds(object val)
    {
        if (Config != null)
            Config.MonsterOdds = Convert.ToSingle(val);
    }
}
