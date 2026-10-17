using Terraria;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Buffs.Debuffs
{
	public class GreathammerStunDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<GuardianGlobalNPC>().StunTimer = npc.buffTime[buffIndex];
		}
	}
}