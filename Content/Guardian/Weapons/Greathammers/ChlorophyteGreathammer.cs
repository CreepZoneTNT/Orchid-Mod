using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

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
			Item.useTime = 20;
			Item.damage = 349;
			GuardStacks = 2;
			SlamChargeGain = 0.5f;
			WaitChargeGain = 0.5f;
			Range = 30;
			AuraScale = 1.1f;
			AuraPrimary = new(0, 80, 0, 60);
			AuraSecondary = new(10, 40, 10, 60);
			ThrowDamage = 0.4f;
			ThrowHitCooldown = 8;
		}

		public override bool ModifyHit(Player player, OrchidGuardian guardian, Projectile projectile, GuardianGreathammerAnchor anchor, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (anchor.IsThrown && anchor.Anim > -80)
			{
				projectile.velocity *= 0.5f;
			}
			return true;
		}

		public override void ExtraAI(Player player, OrchidGuardian guardian, Projectile projectile, GuardianGreathammerAnchor anchor)
		{
			if (anchor.IsSlamming && anchor.Anim <= 25)
			{
				//double swing recovery speed
				anchor.Anim -= SlamSpeed * player.GetTotalAttackSpeed(DamageClass.Melee);
			}
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
