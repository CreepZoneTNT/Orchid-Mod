using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace OrchidMod.Content.Guardian.Weapons.Greathammers
{
	public class ChlorophyteGreathammer : OrchidModGuardianGreathammer
	{
		public override void SafeSetDefaults()
		{
			Item.width = 38;
			Item.height = 36;
			Item.value = Item.sellPrice(0, 0, 3, 50);
			Item.rare = ItemRarityID.White;
			Item.knockBack = 5f;
			Item.shootSpeed = 20f;
			Item.useTime = 32;
			Item.damage = 349;
			GuardStacks = 1;
			Range = 30;
			AuraScale = 1.1f;
			AuraPrimary = new(0, 80, 0, 60);
			AuraSecondary = new(10, 40, 10, 60);
			ThrowDamage = 0.4f;
			ThrowHitCooldown = 8;
		}

		public override bool PreSound(Player player, OrchidGuardian guardian, Projectile projectile, bool justThrown)
		{
			switch(projectile.ai[0])
			{
				case 2:
					SoundEngine.PlaySound(SoundID.Item71.WithPitchOffset(-0.5f), player.Center);
					return false;
				case 5:
					if (justThrown) SoundEngine.PlaySound(SoundID.Item116, projectile.Center);
					SoundEngine.PlaySound(SoundID.Item71.WithVolumeScale(0.4f), projectile.Center);
					return false;
				default:
				return true;
			}
		}
	}
}
