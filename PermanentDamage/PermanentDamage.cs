using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using PermanentDamage.Systems;

namespace PermanentDamage;

public class PermanentDamage : Mod
{
    internal const byte KillUpdatePacket = 1;
    internal const byte DamageUpdatePacket = 2;

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        byte packetType = reader.ReadByte();

        switch (packetType)
        {
            case KillUpdatePacket:
            {
                int npcType = reader.ReadInt32();
                int kills = reader.ReadInt32();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    return;
                }
                WeakeningSystem.AddKillCount(npcType, kills);
                break;
            }
            case DamageUpdatePacket:
            {
                int npcType = reader.ReadInt32();
                double damage = reader.ReadDouble();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    return;
                }
                WeakeningSystem.AddTotalDamage(npcType, damage);
                break;
            }


            default:
                Logger.Warn($"Unknown packet type: {packetType}");
                break;
        }
    }

    internal static void BroadcastKillUpdate(int npcType, int kills = 1)
    {
        if (Main.netMode != NetmodeID.Server)
        {
            return;
        }
        ModPacket packet = ModContent.GetInstance<PermanentDamage>().GetPacket();
        packet.Write(KillUpdatePacket);
        packet.Write(npcType);
        packet.Write(kills);
        packet.Send();
    }

    internal static void BroadcastDamageUpdate(int npcType, double damage)
    {
        if (Main.netMode != NetmodeID.Server)
        {
            return;
        }
        ModPacket packet = ModContent.GetInstance<PermanentDamage>().GetPacket();
        packet.Write(DamageUpdatePacket);
        packet.Write(npcType);
        packet.Write(damage);
        packet.Send();
    }
}
