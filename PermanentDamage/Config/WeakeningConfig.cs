using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;
using PermanentDamage.Systems;

namespace PermanentDamage.Config;


public enum WeakeningScope
{
    World,
    Player,
}

public enum WeakeningMetric
{
    KillCount,
    Damage,
}

public enum WeakeningMessageFrequency
{
    Every1 = 1,
    Every10 = 10,
    Every50 = 50,
    Every100 = 100,
}

public sealed class WeakeningConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [DefaultValue(false)]
    public bool Enabled;

    [DefaultValue(WeakeningScope.World)]
    public WeakeningScope Scope;

    [DefaultValue(WeakeningMetric.KillCount)]
    public WeakeningMetric Metric;

    [Range(100, 1000)]
    [Increment(100)]
    [DefaultValue(500)]
    [Slider]
    [DrawTicks]
    public int WeakeningThreshold;

    [DefaultValue(false)]
    public bool ShowWeakeningMessage;

    [DefaultValue(WeakeningMessageFrequency.Every50)]
    public WeakeningMessageFrequency MessageFrequency;

    public override void OnChanged()
    {
        if (!Main.gameMenu)
        {
            WeakeningSystem.ApplyFromConfig(this);
        }
    }
}
