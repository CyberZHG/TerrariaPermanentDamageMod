using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace PermanentDamage.Config;


public enum WeakeningBasedOn
{
    World,
    Player,
}

public sealed class WeakeningConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [DefaultValue(false)]
    public bool Enabled;

    [DefaultValue(WeakeningBasedOn.World)]
    public WeakeningBasedOn BasedOn;

    [Range(100, 1000)]
    [Increment(100)]
    [DefaultValue(500)]
    [Slider]
    [DrawTicks]
    public int KillsCap;
}
