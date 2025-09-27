using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using PermanentDamage.Systems;

namespace PermanentDamage.NPCs;

public class WeakeningNPC : GlobalNPC
{
    private static bool IsEligible(NPC npc)
    {
        return !npc.friendly
               && !npc.townNPC
               && !npc.immortal
               && !npc.dontTakeDamage
               && !npc.CountsAsACritter
               && npc.lifeMax > 1;
    }

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
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
        Main.NewText(WeakeningSystem.GetNPCKey(npc.type) + " x " + multiplier.ToString());
    }

    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            return;
        }
        if (!IsEligible(npc))
        {
            return;
        }

        WeakeningSystem.AddKillCount(npc.type);
        PermanentDamage.BroadcastKillUpdate(npc.type);
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