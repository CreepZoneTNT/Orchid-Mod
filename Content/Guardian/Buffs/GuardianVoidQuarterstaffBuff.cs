using Terraria;
using Terraria.ModLoader;
using OrchidMod.Content.Guardian.Weapons.Quarterstaves;

namespace OrchidMod.Content.Guardian.Buffs
{
	public class GuardianVoidQuarterstaffBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = false;
			Main.buffNoSave[Type] = true;
		}
	}
}