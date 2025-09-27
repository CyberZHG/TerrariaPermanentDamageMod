using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PermanentDamage.Systems;

public class WeakeningSystem : ModSystem
{
    private static readonly Dictionary<int, int> KillCounts = new();
    private static readonly Dictionary<int, double> DamageSums = new();
    
    public static string GetNPCKey(int type)
    {
        if (type < NPCID.Count)
        {
            return "Terraria:" + type;
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
        if (KillCounts.TryGetValue(type, out int kills))
        {
            return kills;
        }

        return 0;
    }

    public static double GetDamageSum(int type)
    {
        if (DamageSums.TryGetValue(type, out double totalDamage))
        {
            return totalDamage;
        }

        return 0.0;
    }

    public static void AddKillCount(int type, int kills = 1)
    {
        KillCounts[type] = GetKillCount(type) + kills;
    }

    public static void AddTotalDamage(int type, int damage)
    {
        DamageSums[type] = GetDamageSum(type) + damage;
    }

    public static double GetHealthMultiplier(int type)
    {
        return Math.Max(0.001, 1.0 - GetKillCount(type) * 0.001);
    }
}
