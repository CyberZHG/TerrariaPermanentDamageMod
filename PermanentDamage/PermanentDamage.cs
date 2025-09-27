using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using PermanentDamage.Systems;

namespace PermanentDamage;

public class PermanentDamage : Mod
{
    internal const byte KillUpdatePacket = 1;

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
}
