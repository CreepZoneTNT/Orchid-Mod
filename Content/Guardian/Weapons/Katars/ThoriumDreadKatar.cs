using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common;
using OrchidMod.Common.Attributes;
using OrchidMod.Content.General.Prefixes;
using OrchidMod.Content.Guardian.Buffs;
using OrchidMod.Content.Guardian.Projectiles.Gauntlets;
using OrchidMod.Content.Guardian.Projectiles.Katars;
using OrchidMod.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace OrchidMod.Content.Guardian.Weapons.Katars
{
	[CrossmodContent("ThoriumMod")]
	public class ThoriumDreadKatar : OrchidModGuardianKatar
	{
		public List<int> HookProjectiles;

		public int ChainCooldown;

		public static int ProjType => ModContent.ProjectileType<ThoriumDreadKatarProjectile>();
		
		public override void SafeSetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.knockBack = 3f;
			Item.damage = 180;
			Item.value = Item.sellPrice(0, 2, 16);
			Item.rare = ItemRarityID.Yellow;
			Item.useTime = 20;
			Item.shootSpeed = 24f;
			ChargedAttackDoT = 0;
			ParryDuration = 40;

			HookProjectiles = [];
		}

		public override Color GetColor() => new (156, 239, 72);

		public override void HoldItemFrame(Player player)
		{
			ChainCooldown--;
			if (ChainCooldown <= 0)
			{
				hasShot = false;
				ChainCooldown = 0;
			}
		}

		public override void OnHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit, bool charged)
		{
			target.AddBuff(BuffID.CursedInferno, player.HasBuff<GuardianDreadGauntletBuff>() ? 180 : 60);
		}

		bool hasShot = false;
		public override bool OnJab(Player player, OrchidGuardian guardian, Projectile projectile, bool offHandKatar, bool manuallyFullyCharged, ref bool charged, ref int damage)
		{
			if (charged)
			{
				if (HookProjectiles.Count > 0)
				{
					foreach (int index in HookProjectiles)
					{
						Projectile hookProjectile = Main.projectile[index];
						if (hookProjectile != null && hookProjectile.type == ProjType && hookProjectile.active && hookProjectile.owner == player.whoAmI)
							hookProjectile.Kill();
					}
					HookProjectiles.Clear();
				}

				for (int i = -2; i <= 2; i++)
				{
					int hookProj = Projectile.NewProjectile(projectile.GetSource_FromAI(), projectile.Center, Vector2.UnitY.RotatedBy(projectile.ai[1] + Main.rand.NextFloat(0.01f) * i) * Item.shootSpeed, ProjType, player.GetWeaponDamage(Item), 2f);
					HookProjectiles.Add(hookProj);
				}
			}
			else
			{
				if (HookProjectiles.Count == 5)
				{
					foreach (int index in HookProjectiles)
					{
						Projectile hookProjectile = Main.projectile[index];
						if (hookProjectile != null && hookProjectile.type == ProjType && hookProjectile.active && hookProjectile.owner == player.whoAmI && (int)hookProjectile.ai[0] != -1)
						{
							NPC hookedNPC = Main.npc[(int)hookProjectile.ai[0]];
						}
					}
				}
			}

			return HookProjectiles.Count == 0;
		}

		public override bool PreDrawKatar(SpriteBatch spriteBatch, Projectile projectile, Player player, bool offHandKatar, ref Color lightColor)
		{
			foreach (int index in HookProjectiles)
			{
				Projectile hookProjectile = Main.projectile[index];
				if (hookProjectile != null && hookProjectile.type == ProjType && hookProjectile.active && hookProjectile.owner == player.whoAmI)
				{ // Draw chain between hook and gauntlet
					Texture2D chainTexture = ModContent.Request<Texture2D>(Texture + "_Chain", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
					Vector2 chainDirection = hookProjectile.Center - (projectile.Center + Vector2.UnitY * player.gfxOffY);
					Vector2 segment = Vector2.Normalize(chainDirection) * chainTexture.Height * 0.66f;

					int nbSegments = 0;

					while(chainDirection.Length() > (segment * nbSegments).Length())
						nbSegments++;

					while (nbSegments > 0)
					{
						nbSegments--;
						chainDirection -= segment;
						Vector2 chainPos = projectile.Center + chainDirection - Main.screenPosition;
						Lighting.AddLight(chainPos, Color.GreenYellow.ToVector3() * 0.25f);
						spriteBatch.Draw(chainTexture, chainPos, null, lightColor, 0f, chainTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
					}

				}		
			}
			

			return base.PreDrawKatar(spriteBatch, projectile, player, offHandKatar, ref lightColor);
		}
		
		public override void AddRecipes()
		{
			var thoriumMod = OrchidMod.ThoriumMod;
			if (thoriumMod != null)
			{
				CreateRecipe()
				.AddIngredient(thoriumMod, "DreadSoul", 8)
				.AddTile(thoriumMod, "SoulForgeNew")
				.Register();
			}
		}
	}
}
