using System;
using Microsoft.Xna.Framework;
using OrchidMod.Assets;
using OrchidMod.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;

namespace OrchidMod.Content.Guardian.Projectiles.FencingBlades;

public class ThoriumTemplarFencingBladeProjectile : OrchidModGuardianProjectile
{
	public override string Texture => OrchidAssets.InvisiblePath;

	public override void SafeSetDefaults()
	{
		Projectile.width = 10;
		Projectile.height = 10;
		Projectile.timeLeft = 1500;
		Projectile.extraUpdates = 29;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
	}

	public override void AI()
	{
		if (Owner.active && !Owner.dead)
		{
			Vector2 newVelocity = Vector2.Normalize(Owner.Center - Projectile.Center);
			Projectile.velocity = Projectile.velocity * 0.95f + newVelocity * 0.8f;
		}
		else if (Projectile.timeLeft > 41) Projectile.timeLeft--;

		int dustType = (int)Projectile.ai[0] switch
		{
			0 => DustID.HealingPlus,
			1 => DustID.ManaRegeneration,
			2 => 161,
			3 => DustID.Cloud,
			_ => DustID.Smoke
		};
		Dust trail = Dust.NewDustPerfect(Projectile.Center, dustType, -Projectile.velocity * 0.1f, Scale: 1.5f);
		trail.noGravity = true;

		if (Main.rand.NextBool(4))
			Projectile.Center += Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f);

		if (Projectile.Distance(Owner.Center) <= 30f)
		{
			int toAdd = 0;
			switch ((int)Projectile.ai[0])
			{ 
				case 0:
					toAdd = (int)MathF.Round(20 * Projectile.ai[1]);
					Owner.statLife += toAdd;
					if (Owner.statLife > Owner.statLifeMax2) Owner.statLife = Owner.statLifeMax2;
					else if (Owner.statLife < 0) Owner.KillMe(PlayerDeathReason.ByProjectile(Owner.whoAmI, Projectile.whoAmI), MathF.Abs(toAdd), 1);
					Owner.HealEffect(toAdd);
					break;
				case 1:
					toAdd = (int)MathF.Round(100 * Projectile.ai[1]);
					Owner.statMana += toAdd;
					if (Owner.statMana > Owner.statManaMax2) Owner.statMana = Owner.statManaMax2;
					else if (Owner.statMana < 0) 
						Owner.statMana = 0;
					Owner.ManaEffect(toAdd);
					break;
				case 2:
					Guardian.GuardianGuardRecharging += Projectile.ai[1];
					Guardian.GuardianGuard += (int)Guardian.GuardianGuardRecharging;
					Guardian.GuardianGuardRecharging -= (int)Guardian.GuardianGuardRecharging;
					if (Guardian.GuardianGuard > Guardian.GuardianGuardMax) Guardian.GuardianGuard = Guardian.GuardianGuardMax;
					else if (Guardian.GuardianGuard < 0) Guardian.GuardianGuard = 0;
					
					Rectangle rect = Owner.Hitbox;
					rect.Y -= 64;
					CombatText.NewText(rect, Color.LightSkyBlue, "+" + Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Guard", Projectile.ai[1]), false, true);
					break;
				case 3:
					Guardian.GuardianSlamRecharging += Projectile.ai[1];
					Guardian.GuardianSlam += (int)Guardian.GuardianSlamRecharging;
					Guardian.GuardianSlamRecharging -= (int)Guardian.GuardianSlamRecharging;
					if (Guardian.GuardianSlam > Guardian.GuardianSlamMax) Guardian.GuardianSlam = Guardian.GuardianSlamMax;
					else if (Guardian.GuardianSlam < 0) Guardian.GuardianSlam = 0;
					
					Rectangle rect2 = Owner.Hitbox;
					rect2.Y -= 64;
					CombatText.NewText(rect2, Color.LightCyan, "+" + Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Slam", Projectile.ai[1]), false, true);
					break;
			}
			
			Projectile.Kill();
		}
	}

	public override void OnKill(int timeLeft)
	{
		int dustType = (int)Projectile.ai[0] switch
		{
			0 => DustID.HealingPlus,
			1 => DustID.ManaRegeneration,
			2 => 161,
			3 => DustID.Cloud,
			_ => DustID.Smoke
		};
		
		OrchidUtils.SpawnDustCircle(Projectile.Center, 10f, 20, dustType, dust => dust.noGravity = true);
		SoundEngine.PlaySound(SoundID.Item4, Projectile.Center);
	}
}