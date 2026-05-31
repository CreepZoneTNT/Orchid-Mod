using Terraria;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Buffs {
	public class GuardianDreadGauntletBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = false;
			Main.buffNoSave[Type] = true;
		}

		// public override void Update(Player player, ref int buffIndex) 
		// {
		// 	player.GetAttackSpeed(DamageClass.Melee) += 0.8f;
		// 	player.moveSpeed += 0.25f;
		// 	player.statDefense *= 0.5f;
		// }
	}
}