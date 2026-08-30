using System.Collections.Generic;
using Microsoft.Xna.Framework;
using OrchidMod.Common.Attributes;
using OrchidMod.Content.Guardian.Misc;
using OrchidMod.Content.Guardian.Projectiles.FencingBlades;
using OrchidMod.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace OrchidMod.Content.Guardian.Weapons.FencingBlades;

[CrossmodContent("ThoriumMod")]
public class ThoriumTemplarFencingBlade : OrchidModGuardianFencingBlade
{
	public int HitCount = 0;
	 
	public override FencingBladeAttackProfile ChargedProfile(Projectile anchor) => FencingBladeAttackID.MultiSwing with
	{ Quantity = 3, BendAmount = 0.02f, Damage = 0.8f, ControlAngle = MathHelper.Pi / 3f };
	
	public override FencingBladeAttackProfile ReinforcedProfile(Projectile anchor) => FencingBladeAttackID.SingleSwing with
		{ Quantity = 8, UsesFocusProjectile = true };
		
	public override void SafeSetDefaults()
	{
		Item.useTime = 35;
		Item.damage = 108;
		Item.rare = ItemRarityID.Green;
		Item.width = 52;
		Item.height = 48;
		Item.knockBack = 3f;
		Item.shootSpeed = 15f;
		Item.value = Item.sellPrice(0, 3);
	}


	public override Color GetColor(Player player, OrchidGuardian guardian, Projectile anchor) => new (188, 0, 62);

	public override void OnHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit)
	{
		if (projectile.ModProjectile is FencingBladeSlashProjectile slash && slash.Strong && slash.Stab)
		{
			HitCount++;
			CombatText.NewText(player.getRect(), Color.IndianRed, HitCount);
		}
	}

	public override void ExtraAIFencingBlade(Player player, OrchidGuardian guardian, Projectile anchor)
	{
		if (anchor.ModProjectile is GuardianFencingBladeAnchor fencingBlade)
		{
			if (anchor.ai[0] is 0 or 1f)
			{
				if (HitCount > 0)
				{
					Player targetPlayer = null;
					float lowestRatio = 0.5f;
					foreach (Player activePlayer in Main.ActivePlayers)
						if (activePlayer.whoAmI != 255 && (Main.netMode != NetmodeID.MultiplayerClient || activePlayer.whoAmI != player.whoAmI) && (activePlayer.team == player.team || activePlayer.team == 0))
						{
							float ratio = activePlayer.statLife / (float)activePlayer.statLifeMax2;
							if (ratio < lowestRatio)
							{
								lowestRatio = ratio;
								targetPlayer = activePlayer;
							}
						}

					if (targetPlayer != null)
					{
						float efficacy = (targetPlayer.statLifeMax2 / 400f) * 0.5f;
						Projectile.NewProjectileDirect(anchor.GetSource_FromThis(), anchor.Center, player.DirectionTo(targetPlayer.Center), ModContent.ProjectileType<ThoriumTemplarFencingBladeProjectile>(), 1, 0f, targetPlayer.whoAmI, 0, efficacy);
					}

					HitCount--;
					if (HitCount < 0)
						HitCount = 0;
				}
			}
		}
	}

	public override bool ProjectileAI(Player player, Projectile projectile, bool charged)
	{
		if (projectile.ModProjectile is FencingBladeSlashProjectile slash && !slash.Stab)
		{
			foreach (Item heartItem in Main.ActiveItems)
			{
				if (heartItem.Hitbox.Intersects(projectile.Hitbox) && (heartItem.type == ItemID.Heart || heartItem.type == ItemID.Star || heartItem.type == ModContent.ItemType<Guard>() || heartItem.type == ModContent.ItemType<Slam>()))
				{
					int resourceType = heartItem.type == ModContent.ItemType<Slam>() ? 3 : heartItem.type == ModContent.ItemType<Guard>() ? 2 : heartItem.type == ItemID.Star ? 1 : heartItem.type == ItemID.Heart ? 0 : -1;
					
					Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), heartItem.Center, heartItem.DirectionTo(player.Center), ModContent.ProjectileType<ThoriumTemplarFencingBladeProjectile>(), 1, 0f, player.whoAmI, resourceType, 0.75f);
					heartItem.TurnToAir(true);
					
					projectile.Kill();
				}
			}
		}
		return true;
	}
}