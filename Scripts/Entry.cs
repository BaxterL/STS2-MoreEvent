using System.IO;
using System.Text.Json;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace MoreEvent.Scripts;

public class MoreEventConfig
{
    public double UnknownNodeMultiplier { get; set; } = 2.0;
    public double MonsterOdds { get; set; } = 0.0;
}

[HarmonyPatch(typeof(MapPointTypeCounts), MethodType.Constructor, [typeof(Rng)])]
public static class Patch_MoreUnknownNodes
{
    static void Postfix(MapPointTypeCounts __instance)
    {
        if (Entry.Config == null) return;
        var tr = Traverse.Create(__instance);
        int cur = tr.Property<int>("NumOfUnknowns").Value;
        tr.Property<int>("NumOfUnknowns").Value = (int)(cur * Entry.Config.UnknownNodeMultiplier);
    }
}

[HarmonyPatch(typeof(UnknownMapPointOdds), MethodType.Constructor, [typeof(Rng)])]
public static class Patch_FavorEvents
{
    static void Postfix(UnknownMapPointOdds __instance)
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
            var path = Path.Combine(dir!, "config.json");
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
}
