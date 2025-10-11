using Terraria.ModLoader.IO;

namespace PermanentDamage.Config;

public sealed record WeakeningSettings(bool Enabled, WeakeningBasedOn BasedOn, int KillsCap)
{
	public static WeakeningSettings Default => new(false, WeakeningBasedOn.World, 500);

	public TagCompound Save() => new() 
	{
		["enabled"] = Enabled,
		["basedOn"] = (int)BasedOn,
		["KillsCap"] = KillsCap,
	};

	public static WeakeningSettings Load(TagCompound tag) => 
		new(tag.GetBool("enabled"), (WeakeningBasedOn)tag.GetInt("basedOn"), tag.GetInt("KillsCap"));
}
