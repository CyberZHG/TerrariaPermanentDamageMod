using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using PermanentDamage.Config;
using PermanentDamage.Players;

namespace PermanentDamage.Systems;

public class WeakeningSystem : ModSystem
{
    private const string SAVE_KEY_SETTINGS = "Settings";
    private const string SAVE_KEY_KILL_COUNT_KEYS = "KillCountsKeys";
    private const string SAVE_KEY_KILL_COUNT_VALUES = "KillCountsValues";
    private const string SAVE_KEY_DAMAGE_SUM_KEYS = "DamageSumsKeys";
    private const string SAVE_KEY_DAMAGE_SUM_VALUES = "DamageSumsValues";

    private static WeakeningSettings settings = WeakeningSettings.Default;
    private static bool settingsLoaded;

    public static WeakeningSettings Settings => settings;

    private static readonly Dictionary<string, int> WorldKillCounts = new();
    private static readonly Dictionary<string, double> WorldDamageSums = new();

    private static WeakeningPlayer LocalWeakeningPlayer =>
        Main.LocalPlayer.GetModPlayer<WeakeningPlayer>();

    private static bool UsePlayerScope =>
        Settings.Scope == WeakeningScope.Player;

    internal static void ApplyFromConfig(WeakeningConfig config)
    {
        if (config != null)
        {
            settings = new WeakeningSettings(config.Enabled, config.Scope, config.Metric, config.WeakeningThreshold);
        }
    }

    private static void WriteToConfig()
    {
        WeakeningConfig config = ModContent.GetInstance<WeakeningConfig>();
        if (config == null)
        {
            return;
        }

        config.Enabled = settings.Enabled;
        config.Scope = settings.Scope;
        config.Metric = settings.Metric;
        config.WeakeningThreshold = settings.WeakeningThreshold;
    }

    public static string GetNPCKey(int type)
    {
        if (type < NPCID.Count)
        {
            return type.ToString();
        }

        ModNPC modNPC = NPCLoader.GetNPC(type);

        if (modNPC != null)
        {
            return modNPC.Mod.Name + ":" + modNPC.Name;
        }

        return "";
    }

    public static int GetKillCount(string npcKey)
    {
        Dictionary<string, int> kills = UsePlayerScope ? LocalWeakeningPlayer.KillCounts : WorldKillCounts;
        return GetValue(kills, npcKey);
    }

    public static int GetKillCount(int type)
    {
        return GetKillCount(GetNPCKey(type));
    }

    public static double GetDamageSum(string npcKey)
    {
        Dictionary<string, double> damage = UsePlayerScope ? LocalWeakeningPlayer.DamageSums : WorldDamageSums;
        return GetValue(damage, npcKey);
    }

    public static double GetDamageSum(int type)
    {
        return GetDamageSum(GetNPCKey(type));
    }

    public static void AddKillCount(int type, int kills = 1)
    {
        string npcKey = GetNPCKey(type);

        WorldKillCounts[npcKey] = GetValue(WorldKillCounts, npcKey) + kills;

        Dictionary<string, int> playerKills = LocalWeakeningPlayer.KillCounts;
        playerKills[npcKey] = GetValue(playerKills, npcKey) + kills;
    }

    public static void AddTotalDamage(int type, int damage)
    {
        string npcKey = GetNPCKey(type);

        WorldDamageSums[npcKey] = GetValue(WorldDamageSums, npcKey) + damage;

        Dictionary<string, double> playerDamage = LocalWeakeningPlayer.DamageSums;
        playerDamage[npcKey] = GetValue(playerDamage, npcKey) + damage;
    }

    public static double GetHealthMultiplier(int type)
    {
        if (Settings.WeakeningThreshold <= 0)
        {
            return 1.0;
        }

        double accumulated = Settings.Metric == WeakeningMetric.Damage
            ? GetDamageSum(type)
            : GetKillCount(type);

        return Math.Max(0.001, 1.0 - accumulated / Settings.WeakeningThreshold);
    }

    public override void SaveWorldData(TagCompound tag)
    {
        tag[SAVE_KEY_SETTINGS] = settings.Save();

        SaveIntDict(tag, SAVE_KEY_KILL_COUNT_KEYS, SAVE_KEY_KILL_COUNT_VALUES, WorldKillCounts);
        SaveDoubleDict(tag, SAVE_KEY_DAMAGE_SUM_KEYS, SAVE_KEY_DAMAGE_SUM_VALUES, WorldDamageSums);
    }

    public override void LoadWorldData(TagCompound tag)
    {
        settings = tag.ContainsKey(SAVE_KEY_SETTINGS)
            ? WeakeningSettings.Load(tag.GetCompound(SAVE_KEY_SETTINGS))
            : WeakeningSettings.Default;
        settingsLoaded = true;
        WriteToConfig();

        LoadIntDict(tag, SAVE_KEY_KILL_COUNT_KEYS, SAVE_KEY_KILL_COUNT_VALUES, WorldKillCounts);
        LoadDoubleDict(tag, SAVE_KEY_DAMAGE_SUM_KEYS, SAVE_KEY_DAMAGE_SUM_VALUES, WorldDamageSums);
    }

    public override void OnWorldLoad()
    {
        if (!settingsLoaded)
        {
            ApplyFromConfig(ModContent.GetInstance<WeakeningConfig>());
        }

        WriteToConfig();
        settingsLoaded = false;
    }

    public override void OnWorldUnload()
    {
        settingsLoaded = false;
        WorldKillCounts.Clear();
        WorldDamageSums.Clear();
    }

    private static int GetValue(Dictionary<string, int> dict, string key)
    {
        return dict.TryGetValue(key, out int value) ? value : 0;
    }

    private static double GetValue(Dictionary<string, double> dict, string key)
    {
        return dict.TryGetValue(key, out double value) ? value : 0.0;
    }

    internal static void SaveIntDict(TagCompound tag, string keysKey, string valuesKey, Dictionary<string, int> dict)
    {
        List<string> keys = new();
        List<int> values = new();

        foreach (var entry in dict)
        {
            keys.Add(entry.Key);
            values.Add(entry.Value);
        }

        tag[keysKey] = keys;
        tag[valuesKey] = values;
    }

    internal static void LoadIntDict(TagCompound tag, string keysKey, string valuesKey, Dictionary<string, int> dict)
    {
        dict.Clear();

        if (!tag.ContainsKey(keysKey) || !tag.ContainsKey(valuesKey))
        {
            return;
        }

        IList<string> keys = tag.GetList<string>(keysKey);
        IList<int> values = tag.GetList<int>(valuesKey);

        int count = Math.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
        {
            dict[keys[i]] = values[i];
        }
    }

    internal static void SaveDoubleDict(TagCompound tag, string keysKey, string valuesKey, Dictionary<string, double> dict)
    {
        List<string> keys = new();
        List<double> values = new();

        foreach (var entry in dict)
        {
            keys.Add(entry.Key);
            values.Add(entry.Value);
        }

        tag[keysKey] = keys;
        tag[valuesKey] = values;
    }

    internal static void LoadDoubleDict(TagCompound tag, string keysKey, string valuesKey, Dictionary<string, double> dict)
    {
        dict.Clear();

        if (!tag.ContainsKey(keysKey) || !tag.ContainsKey(valuesKey))
        {
            return;
        }

        IList<string> keys = tag.GetList<string>(keysKey);
        IList<double> values = tag.GetList<double>(valuesKey);

        int count = Math.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
        {
            dict[keys[i]] = values[i];
        }
    }
}
