using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using PermanentDamage.Config;

namespace PermanentDamage.Systems;

public class WeakeningSystem : ModSystem
{
    private const double BASE_MULTIPLIER = 1.0 / 10.0;
    
	private const string SAVE_KEY_SETTINGS = "Settings";
    private const string SAVE_KEY_KILL_COUNT_KEYS = "KillCountsKeys";
    private const string SAVE_KEY_KILL_COUNT_VALUES = "KillCountsValues";

	public static WeakeningSettings Settings { get; private set; }
    
    private static readonly Dictionary<string, int> KillCounts = new();
    private static readonly Dictionary<string, double> DamageSums = new();
    
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
        if (KillCounts.TryGetValue(npcKey, out int kills))
        {
            return kills;
        }

        return 0;
    }

    public static int GetKillCount(int type)
    {
        return GetKillCount(GetNPCKey(type));
    }

    public static double GetDamageSum(string npcKey)
    {
        if (DamageSums.TryGetValue(npcKey, out double totalDamage))
        {
            return totalDamage;
        }

        return 0.0;
    }

    public static double GetDamageSum(int type)
    {
        return GetDamageSum(GetNPCKey(type));
    }

    public static void AddKillCount(int type, int kills = 1)
    {
        string npcKey = GetNPCKey(type);
        KillCounts[npcKey] = GetKillCount(npcKey) + kills;
    }

    public static void AddTotalDamage(int type, int damage)
    {
        string npcKey = GetNPCKey(type);
        DamageSums[npcKey] = GetDamageSum(npcKey) + damage;
    }

    public static double GetHealthMultiplier(int type)
    {
        return Math.Max(0.001, 1.0 - GetKillCount(type) * BASE_MULTIPLIER);
    }
    
    public override void SaveWorldData(TagCompound tag)
    {
		if (Settings != null)
		{
			tag[SAVE_KEY_SETTINGS] = Settings.Save();
		}
        List<string> keys = new();
        List<int> values = new();

        foreach (var entry in KillCounts)
        {
            keys.Add(entry.Key);
            values.Add(entry.Value);
        }

        tag[SAVE_KEY_KILL_COUNT_KEYS] = keys;
        tag[SAVE_KEY_KILL_COUNT_VALUES] = values;
    }

    public override void LoadWorldData(TagCompound tag)
    {
		if (tag.ContainsKey(SAVE_KEY_SETTINGS))
		{
			Settings = WeakeningSettings.Load(tag.GetCompound(SAVE_KEY_SETTINGS));
		}
		else
		{
			Settings = WeakeningSettings.Default;
		}

        KillCounts.Clear();
        DamageSums.Clear();

        if (!tag.ContainsKey(SAVE_KEY_KILL_COUNT_KEYS) || !tag.ContainsKey(SAVE_KEY_KILL_COUNT_VALUES))
        {
            return;
        }

        IList<string> keys = tag.GetList<string>(SAVE_KEY_KILL_COUNT_KEYS);
        IList<int> values = tag.GetList<int>(SAVE_KEY_KILL_COUNT_VALUES);

        int count = Math.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
        {
            KillCounts[keys[i]] = values[i];
        }
    }

}
