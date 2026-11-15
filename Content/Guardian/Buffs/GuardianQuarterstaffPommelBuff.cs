using Terraria;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Buffs
{
	public class GuardianQuarterstaffPommelBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = false;
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			OrchidGuardian guardian = player.Guardian();
			if (guardian.GuardianQuarterstaffPommelBuff != -1) // Cannot set timer if already autoparried
				guardian.GuardianQuarterstaffPommelBuff = player.buffTime[buffIndex];
		}

		public override bool ReApply(Player player, int time, int buffIndex)
		{
			player.Guardian().GuardianQuarterstaffPommelBuff = time;
			return false;
		}
	}
}