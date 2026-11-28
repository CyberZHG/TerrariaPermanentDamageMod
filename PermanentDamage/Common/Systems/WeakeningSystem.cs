using System;
using System.Collections.Generic;
using System.IO;
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
            settings = new WeakeningSettings(
                config.Enabled,
                config.Scope,
                config.Metric,
                config.WeakeningThreshold,
                config.ShowWeakeningMessage,
                config.MessageFrequency);
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
        config.ShowWeakeningMessage = settings.ShowWeakeningMessage;
        config.MessageFrequency = settings.MessageFrequency;
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

    public static int GetKillCount(int type)
    {
        string npcKey = GetNPCKey(type);

        if (!UsePlayerScope)
        {
            return GetValue(WorldKillCounts, npcKey);
        }

        // We need to get the sum even in single-player mode because there may be fake players in some mods
        int sum = 0;
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player != null && player.active)
            {
                sum += GetValue(player.GetModPlayer<WeakeningPlayer>().KillCounts, npcKey);
            }
        }

        return sum;
    }

    public static double GetDamageSum(int type)
    {
        string npcKey = GetNPCKey(type);

        if (!UsePlayerScope)
        {
            return GetValue(WorldDamageSums, npcKey);
        }

        // We need to get the sum even in single-player mode because there may be fake players in some mods
        double sum = 0.0;
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player != null && player.active)
            {
                sum += GetValue(player.GetModPlayer<WeakeningPlayer>().DamageSums, npcKey);
            }
        }

        return sum;
    }

    public static void RegisterKill(int type, int creditPlayer, int kills = 1)
    {
        string npcKey = GetNPCKey(type);
        WorldKillCounts[npcKey] = GetValue(WorldKillCounts, npcKey) + kills;
        ApplyPlayerKill(creditPlayer, type, kills);
    }

    public static void RegisterDamage(int type, int creditPlayer, double damage)
    {
        string npcKey = GetNPCKey(type);
        WorldDamageSums[npcKey] = GetValue(WorldDamageSums, npcKey) + damage;
        ApplyPlayerDamage(creditPlayer, type, damage);
    }

    public static void ApplyPlayerKill(int player, int type, int kills)
    {
        WeakeningPlayer wp = GetWeakeningPlayer(player);
        if (wp == null)
        {
            return;
        }

        string npcKey = GetNPCKey(type);
        wp.KillCounts[npcKey] = GetValue(wp.KillCounts, npcKey) + kills;
    }

    public static void ApplyPlayerDamage(int player, int type, double damage)
    {
        WeakeningPlayer wp = GetWeakeningPlayer(player);
        if (wp == null)
        {
            return;
        }

        string npcKey = GetNPCKey(type);
        wp.DamageSums[npcKey] = GetValue(wp.DamageSums, npcKey) + damage;
    }

    private static WeakeningPlayer GetWeakeningPlayer(int player)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            return LocalWeakeningPlayer;
        }

        if (player < 0 || player >= Main.maxPlayers)
        {
            return null;
        }

        Player p = Main.player[player];
        if (p == null || !p.active)
        {
            return null;
        }

        return p.GetModPlayer<WeakeningPlayer>();
    }

    internal static void ReceivePlayerFullSync(int player, BinaryReader reader)
    {
        WeakeningPlayer wp = (player >= 0 && player < Main.maxPlayers && Main.player[player] != null)
            ? Main.player[player].GetModPlayer<WeakeningPlayer>()
            : null;

        Dictionary<string, int> killTarget = wp?.KillCounts ?? new Dictionary<string, int>();
        Dictionary<string, double> damageTarget = wp?.DamageSums ?? new Dictionary<string, double>();

        ReadIntDictStream(reader, killTarget);
        ReadDoubleDictStream(reader, damageTarget);
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

    internal static void WriteIntDictStream(BinaryWriter writer, Dictionary<string, int> dict)
    {
        writer.Write(dict.Count);
        foreach (var entry in dict)
        {
            writer.Write(entry.Key);
            writer.Write(entry.Value);
        }
    }

    internal static void ReadIntDictStream(BinaryReader reader, Dictionary<string, int> dict)
    {
        dict.Clear();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            string key = reader.ReadString();
            int value = reader.ReadInt32();
            dict[key] = value;
        }
    }

    internal static void WriteDoubleDictStream(BinaryWriter writer, Dictionary<string, double> dict)
    {
        writer.Write(dict.Count);
        foreach (var entry in dict)
        {
            writer.Write(entry.Key);
            writer.Write(entry.Value);
        }
    }

    internal static void ReadDoubleDictStream(BinaryReader reader, Dictionary<string, double> dict)
    {
        dict.Clear();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            string key = reader.ReadString();
            double value = reader.ReadDouble();
            dict[key] = value;
        }
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
