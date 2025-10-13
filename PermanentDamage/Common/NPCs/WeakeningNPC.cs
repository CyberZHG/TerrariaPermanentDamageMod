using System;
using System.IO;
using System.Text;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Localization;
using PermanentDamage.Systems;
using PermanentDamage.Config;

namespace PermanentDamage.NPCs;

public class WeakeningNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    
    private bool initialized = false;
    private int countedDamage = 0;

    private static bool IsEligible(NPC npc)
    {
        return !npc.friendly
               && !npc.townNPC
               && !npc.immortal
               && !npc.dontTakeDamage
               && !npc.CountsAsACritter
               && npc.lifeMax > 1;
    }

    public override void PostAI(NPC npc)
    {
        if (!WeakeningSystem.Settings.Enabled || initialized || Main.netMode == NetmodeID.MultiplayerClient)
        {
            return;
        }

        initialized = true;
        if (!IsEligible(npc))
        {
            return;
        }
        
        double multiplier = WeakeningSystem.GetHealthMultiplier(npc.type);
        int newLifeMax = Math.Max(1, (int)Math.Floor(npc.lifeMax * multiplier));
        npc.life = Math.Clamp((int)Math.Ceiling((double)npc.life * newLifeMax / npc.lifeMax), 1, newLifeMax);
        npc.lifeMax = newLifeMax;
        if (Main.netMode == NetmodeID.Server)
        {
            npc.netUpdate = true;
        }
    }

	private void ShowWeakeningMessage(NPC npc)
	{
		if (!WeakeningSystem.Settings.ShowWeakeningMessage)
		{
			return;
		}
		if (WeakeningSystem.GetKillCount(npc.type) % (int)WeakeningSystem.Settings.MessageFrequency != 0)
		{
 			return;
		}

		StringBuilder message = new StringBuilder();
		message.Append(Language.GetTextValue("Mods.PermanentDamage.Messages.Prefix"));
		message.Append(npc.FullName + " (" + npc.type.ToString() + ")   ");
		if (WeakeningSystem.Settings.Metric == WeakeningMetric.KillCount)
		{
			message.Append(Language.GetTextValue("Mods.PermanentDamage.Messages.Kills"));
			message.Append(" " + WeakeningSystem.GetKillCount(npc.type).ToString());
		}
		else
		{
			message.Append(Language.GetTextValue("Mods.PermanentDamage.Messages.Damage"));
			message.Append(" " + WeakeningSystem.GetDamageSum(npc.type).ToString("0.##"));
		}
		double multiplier = WeakeningSystem.GetHealthMultiplier(npc.type) * 100.0;
		message.Append("   ");
		message.Append(Language.GetTextValue("Mods.PermanentDamage.Messages.LifeMax"));
		message.Append(" " + multiplier.ToString("0.#") + "%");
        Main.NewText(message.ToString());
	}

    public override void OnKill(NPC npc)
    {
        if (!WeakeningSystem.Settings.Enabled || Main.netMode == NetmodeID.MultiplayerClient)
        {
            return;
        }
        if (!IsEligible(npc))
        {
            return;
        }

        WeakeningSystem.AddKillCount(npc.type);
        PermanentDamage.BroadcastKillUpdate(npc.type);
		ShowWeakeningMessage(npc);
    }

	private void UpdateDamage(NPC npc, int damageDone)
	{
        if (!WeakeningSystem.Settings.Enabled || Main.netMode == NetmodeID.MultiplayerClient)
        {
            return;
        }
        if (!IsEligible(npc))
        {
            return;
        }
		
		int remaining = npc.lifeMax - countedDamage;
		if (remaining <= 0)
		{
			return;
		}

		int counted = Math.Min(damageDone, remaining);
		countedDamage += counted;

		double damage = (double)counted / npc.lifeMax;
		WeakeningSystem.AddTotalDamage(npc.type, damage);
        PermanentDamage.BroadcastDamageUpdate(npc.type, damage);
	}

	public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
		UpdateDamage(npc, damageDone);
    }

    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
		UpdateDamage(npc, damageDone);
    }
    
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        binaryWriter.Write(npc.lifeMax);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        int serverMax = binaryReader.ReadInt32();
        npc.lifeMax = Math.Max(1, serverMax);
        npc.life = Math.Min(npc.life, npc.lifeMax);
    }
}