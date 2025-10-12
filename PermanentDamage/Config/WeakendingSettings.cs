using Terraria.ModLoader.IO;

namespace PermanentDamage.Config;

public sealed record WeakeningSettings(bool Enabled, WeakeningScope Scope, WeakeningMetric Metric, int WeakeningThreshold)
{
	public static WeakeningSettings Default => new(false, WeakeningScope.World, WeakeningMetric.KillCount, 500);

	public TagCompound Save() => new()
	{
		["enabled"] = Enabled,
		["scope"] = (int)Scope,
		["metric"] = (int)Metric,
		["threshold"] = WeakeningThreshold,
	};

	public static WeakeningSettings Load(TagCompound tag) =>
		new(
			tag.GetBool("enabled"),
			(WeakeningScope)tag.GetInt("scope"),
			(WeakeningMetric)tag.GetInt("metric"),
			tag.GetInt("threshold"));
}
