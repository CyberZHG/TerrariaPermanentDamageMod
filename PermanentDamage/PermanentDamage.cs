using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using PermanentDamage.Systems;

namespace PermanentDamage;

public class PermanentDamage : Mod
{
    internal const byte PlayerFullSync = 1;
    internal const byte PlayerKillUpdate = 2;
    internal const byte PlayerDamageUpdate = 3;

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        byte packetType = reader.ReadByte();

        switch (packetType)
        {
            case PlayerFullSync:
            {
                int player = reader.ReadByte();
                WeakeningSystem.ReceivePlayerFullSync(player, reader);
                break;
            }
            case PlayerKillUpdate:
            {
                int player = reader.ReadByte();
                int npcType = reader.ReadInt32();
                int kills = reader.ReadInt32();
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    WeakeningSystem.ApplyPlayerKill(player, npcType, kills);
                }
                break;
            }
            case PlayerDamageUpdate:
            {
                int player = reader.ReadByte();
                int npcType = reader.ReadInt32();
                double damage = reader.ReadDouble();
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    WeakeningSystem.ApplyPlayerDamage(player, npcType, damage);
                }
                break;
            }
            default:
                Logger.Warn($"Unknown packet type: {packetType}");
                break;
        }
    }

    internal static void BroadcastPlayerKill(int player, int npcType, int kills = 1)
    {
        if (Main.netMode != NetmodeID.Server || player < 0)
        {
            return;
        }
        ModPacket packet = ModContent.GetInstance<PermanentDamage>().GetPacket();
        packet.Write(PlayerKillUpdate);
        packet.Write((byte)player);
        packet.Write(npcType);
        packet.Write(kills);
        packet.Send();
    }

    internal static void BroadcastPlayerDamage(int player, int npcType, double damage)
    {
        if (Main.netMode != NetmodeID.Server || player < 0)
        {
            return;
        }
        ModPacket packet = ModContent.GetInstance<PermanentDamage>().GetPacket();
        packet.Write(PlayerDamageUpdate);
        packet.Write((byte)player);
        packet.Write(npcType);
        packet.Write(damage);
        packet.Send();
    }
}
