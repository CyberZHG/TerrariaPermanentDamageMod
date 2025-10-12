using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using PermanentDamage.Systems;

namespace PermanentDamage.Players;

public class WeakeningPlayer : ModPlayer
{
    private const string SAVE_KEY_KILL_COUNT_KEYS = "KillCountsKeys";
    private const string SAVE_KEY_KILL_COUNT_VALUES = "KillCountsValues";
    private const string SAVE_KEY_DAMAGE_SUM_KEYS = "DamageSumsKeys";
    private const string SAVE_KEY_DAMAGE_SUM_VALUES = "DamageSumsValues";

    public readonly Dictionary<string, int> KillCounts = new();
    public readonly Dictionary<string, double> DamageSums = new();

    public override void SaveData(TagCompound tag)
    {
        WeakeningSystem.SaveIntDict(tag, SAVE_KEY_KILL_COUNT_KEYS, SAVE_KEY_KILL_COUNT_VALUES, KillCounts);
        WeakeningSystem.SaveDoubleDict(tag, SAVE_KEY_DAMAGE_SUM_KEYS, SAVE_KEY_DAMAGE_SUM_VALUES, DamageSums);
    }

    public override void LoadData(TagCompound tag)
    {
        WeakeningSystem.LoadIntDict(tag, SAVE_KEY_KILL_COUNT_KEYS, SAVE_KEY_KILL_COUNT_VALUES, KillCounts);
        WeakeningSystem.LoadDoubleDict(tag, SAVE_KEY_DAMAGE_SUM_KEYS, SAVE_KEY_DAMAGE_SUM_VALUES, DamageSums);
    }
}
