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
using OrchidMod.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace OrchidMod.Content.Guardian.Weapons.Gauntlets
{
	[CrossmodContent("ThoriumMod")]
	public class ThoriumDreadGauntlet : OrchidModGuardianGauntlet
	{
		
		public bool PullOnKill;

		public Projectile HookProjectile;

		public int ChainCooldown;
		
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
			StrikeVelocity = 20f;
			ParryDuration = 90;
		}

		public override Color GetColor(bool offHand)
		{
			return offHand ? new Color(83, 134, 33) : new Color(156, 239, 72);
		}

		public override void HoldItemFrame(Player player)
		{
			ChainCooldown--;
			if (ChainCooldown <= 0)
			{
				hasShot = false;
				ChainCooldown = 0;
			}

		}

		// public override bool OnPunch(Player player, OrchidGuardian guardian, Projectile projectile, bool offHandGauntlet, bool manuallyFullyCharged, ref bool charged, ref int damage)
		// {
		// 	if (player.HasBuff<GuardianDreadGauntletBuff>())
		// 	{
		// 		SoundEngine.PlaySound(SoundID.Item63, projectile.Center);
		// 		if (manuallyFullyCharged) SoundEngine.PlaySound(SoundID.Item71, projectile.Center);
		// 		for (int i = manuallyFullyCharged ? -2 : 0; i <= (manuallyFullyCharged ? 2 : 0); i++)
		// 			Projectile.NewProjectileDirect(
		// 				projectile.GetSource_FromAI(),
		// 				projectile.Center,
		// 				Vector2.Normalize(Main.MouseWorld - player.MountedCenter)
		// 					.RotatedBy(MathHelper.ToRadians(2.5f * i + Main.rand.NextFloat(-0.5f, 0.5f))) 
		// 					* Item.shootSpeed * 0.5f * Main.rand.NextFloat(0.85f, 1.15f),
		// 				ModContent.ProjectileType<ThoriumDreadGauntletProjectileAlt>(),
		// 				guardian.GetGuardianDamage(Item.damage * (!manuallyFullyCharged ? 0.75f : 0.4f)),
		// 				4f,
		// 				Main.myPlayer
		// 			);
		// 	}
		// 	return !player.HasBuff<GuardianDreadGauntletBuff>();
		// }

		public override void OnHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit, bool charged)
		{
			target.AddBuff(BuffID.CursedInferno, player.HasBuff<GuardianDreadGauntletBuff>() ? 180 : 60);
		}

		bool hasShot = false;
		public override bool PreGuard(Player player, OrchidGuardian guardian, Projectile anchor)
		{
			// if (guardian.UseGuard(2, true))
			if (anchor.ModProjectile is GuardianGauntletAnchor gauntlet && !hasShot && guardian.UseGuard(1, true))
			{
				int projType = ModContent.ProjectileType<ThoriumDreadGauntletProjectile>();
				if (HookProjectile != null && HookProjectile.type == projType && HookProjectile.active && HookProjectile.owner == player.whoAmI && ((HookProjectile.ai[0] == 1f && !gauntlet.OffHandGauntlet) || (HookProjectile.ai[0] == 2f && gauntlet.OffHandGauntlet)))
				{
					HookProjectile.Kill();
					HookProjectile = null;
				}

				Vector2 velocity = Vector2.UnitY.RotatedBy((Main.MouseWorld - player.MountedCenter).ToRotation() - MathHelper.PiOver2) * Item.shootSpeed;
				Projectile hookProj = Projectile.NewProjectileDirect(player.GetSource_ItemUse(Item), anchor.Center + velocity, velocity, projType, guardian.GetGuardianDamage(Item.damage * 0.25f), 5f, player.whoAmI, gauntlet.OffHandGauntlet ? 2f : 1f, -1f, anchor.whoAmI);
				hookProj.CritChance = (int)(player.GetCritChance<GuardianDamageClass>() + player.GetCritChance<GenericDamageClass>() + Item.crit);
				HookProjectile = hookProj;
				ChainCooldown = 20;
				guardian.UseGuard(1);
				// guardian.UseGuard(2);
				hasShot = true;
			}
			return false;
		}
		
		public override bool PreDrawGauntlet(SpriteBatch spriteBatch, Projectile projectile, Player player, bool offHandGauntlet, ref Color lightColor)
		{
			int projType = ModContent.ProjectileType<ThoriumDreadGauntletProjectile>();
			if (HookProjectile != null && HookProjectile.type == projType && HookProjectile.active && HookProjectile.owner == player.whoAmI && ((HookProjectile.ai[0] == 1f && !offHandGauntlet) || (HookProjectile.ai[0] == 2f && offHandGauntlet)))
			{ // Draw chain between hook and gauntlet
				Texture2D chainTexture = ModContent.Request<Texture2D>(Texture + "_Chain", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
				Vector2 chainDirection = HookProjectile.Center - (projectile.Center + Vector2.UnitY * player.gfxOffY);
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

			return base.PreDrawGauntlet(spriteBatch, projectile, player, offHandGauntlet, ref lightColor);
		}
		
		public override bool CanRightClick() => true;

		public override bool ConsumeItem(Player player) => false;

		public override void RightClick(Player player)
		{
			PullOnKill = !PullOnKill;
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
		public override void SaveData(TagCompound tag)
		{
			tag.Add("PullOnKill", PullOnKill);
		}

		public override void LoadData(TagCompound tag)
		{
			PullOnKill = tag.GetBool("PullOnKill");
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write(PullOnKill);
			int index = (HookProjectile != null && HookProjectile.active && HookProjectile.owner == Main.myPlayer && HookProjectile.type == ModContent.ProjectileType<ThoriumDreadGauntletProjectile>() ? HookProjectile.whoAmI : 255); 
			writer.Write((byte)index);
		}

		public override void NetReceive(BinaryReader reader)
		{
			PullOnKill = reader.ReadBoolean();
			var index = reader.Read();
			if (index != -1 && index < Main.maxProjectiles) {
				var proj = Main.projectile[index];
				if (proj != null && proj.active && proj.owner == Main.myPlayer && proj.type == ModContent.ProjectileType<ThoriumDreadGauntletProjectile>())
					HookProjectile = proj;
			}
			
		}

		public override void SafeModifyTooltips(List<TooltipLine> tooltips)
		{
			TooltipLine ttip = tooltips.FirstOrDefault(x => x.Mod.Equals("OrchidMod") && x.Name.Equals("ClickInfo"));
			string click = ModContent.GetInstance<OrchidClientConfig>().GuardianSwapGauntletImputs ? Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.LeftClick") : Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.RightClick");
			if (ttip != null)
			{
				ttip.Text = Language.GetTextValue(Mod.GetLocalizationKey("Items.ThoriumDreadGauntlet.Parry"), click);
				ttip.OverrideColor = new Color(175, 255, 175);
			}
			
			int index = tooltips.FindIndex(tt => tt.Mod.Equals("Terraria") && tt.Name.Equals("Knockback"));
			string pull = PullOnKill ? Language.GetTextValue("Mods.OrchidMod.Items.ThoriumYewGauntlet.PullOnKill") : Language.GetTextValue("Mods.OrchidMod.Items.ThoriumYewGauntlet.NoPullOnKill");
			tooltips.Insert(index + 5, new TooltipLine(Mod, "ClickInfo2", pull)
			{
				OverrideColor = Color.Gray
			});
		}
	}
}
