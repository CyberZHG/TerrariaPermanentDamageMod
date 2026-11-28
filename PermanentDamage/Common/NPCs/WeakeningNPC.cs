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
    private int previousLife = 0;

    private static bool IsEligible(NPC npc)
    {
        // May be optional: !npc.dontTakeDamage && npc.lifeMax > 1
        return !npc.friendly
               && !npc.townNPC
               && !npc.immortal
               && !npc.CountsAsACritter;
    }

    public override void PostAI(NPC npc)
    {
        if (!WeakeningSystem.Settings.Enabled || Main.netMode == NetmodeID.MultiplayerClient || !IsEligible(npc))
        {
            return;
        }

        if (!initialized)
        {
            initialized = true;

            double multiplier = WeakeningSystem.GetHealthMultiplier(npc.type);
            int newLifeMax = Math.Max(1, (int)Math.Floor(npc.lifeMax * multiplier));
            npc.life = Math.Clamp((int)Math.Ceiling((double)npc.life * newLifeMax / npc.lifeMax), 1, newLifeMax);
            npc.lifeMax = newLifeMax;
            previousLife = npc.life;

            if (Main.netMode == NetmodeID.Server)
            {
                npc.netUpdate = true;
            }

            return;
        }

        if (npc.life < previousLife)
        {
            RegisterDamage(npc, previousLife - npc.life);
        }

        previousLife = npc.life;
    }

    private void RegisterDamage(NPC npc, int lifeLost)
    {
        if (lifeLost <= 0)
        {
            return;
        }

        double damage = (double)lifeLost / npc.lifeMax;
        int creditPlayer = GetCreditPlayer(npc);
        WeakeningSystem.RegisterDamage(npc.type, creditPlayer, damage);
        if (creditPlayer >= 0)
        {
            PermanentDamage.BroadcastPlayerDamage(creditPlayer, npc.type, damage);
        }
    }

    private static int GetCreditPlayer(NPC npc)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            return Main.myPlayer;
        }

        int player = npc.lastInteraction;
        if (player >= 0 && player < Main.maxPlayers && Main.player[player] != null && Main.player[player].active)
        {
            return player;
        }

        return -1;
    }

    private void ShowWeakeningMessage(NPC npc)
    {
        if (!WeakeningSystem.Settings.ShowWeakeningMessage)
        {
            return;
        }
        int killCount = WeakeningSystem.GetKillCount(npc.type);
        if (killCount % (int)WeakeningSystem.Settings.MessageFrequency != 0)
        {
            return;
        }

        if (WeakeningSystem.Settings.Metric == WeakeningMetric.KillCount)
        {
            if (killCount > WeakeningSystem.Settings.WeakeningThreshold)
            {
                return;
            }
        }
        else
        {
            double damage = WeakeningSystem.GetDamageSum(npc.type);
            if (damage > WeakeningSystem.Settings.WeakeningThreshold)
            {
                return;
            }
        }

        StringBuilder message = new StringBuilder();
        message.Append(Language.GetTextValue("Mods.PermanentDamage.Messages.Prefix"));
        message.Append(npc.FullName + " (" + npc.type.ToString() + ")  ");
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
        message.Append("  ");
        message.Append(Language.GetTextValue("Mods.PermanentDamage.Messages.LifeMax"));
        message.Append(" ");
        message.Append(multiplier.ToString("0.#") + "%");
        if (Main.netMode == NetmodeID.Server)
        {
            Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message.ToString()), Microsoft.Xna.Framework.Color.White);
        }
        else
        {
            Main.NewText(message.ToString());
        }
    }

    public override void OnKill(NPC npc)
    {
        if (!WeakeningSystem.Settings.Enabled || Main.netMode == NetmodeID.MultiplayerClient || !IsEligible(npc))
        {
            return;
        }

        RegisterDamage(npc, previousLife);

        int creditPlayer = GetCreditPlayer(npc);
        WeakeningSystem.RegisterKill(npc.type, creditPlayer);
        if (creditPlayer >= 0)
        {
            PermanentDamage.BroadcastPlayerKill(creditPlayer, npc.type);
        }
        ShowWeakeningMessage(npc);
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
