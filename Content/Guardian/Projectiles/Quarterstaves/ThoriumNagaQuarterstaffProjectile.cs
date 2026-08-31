using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Text;
using Humanizer;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common;
using OrchidMod.Common.ModObjects;
using OrchidMod.Content.Guardian.Weapons.Quarterstaves;
using OrchidMod.Utilities;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;

namespace OrchidMod.Content.Guardian.Projectiles.Quarterstaves
{
	public class ThoriumNagaQuarterstaffProjectile : OrchidModGuardianProjectile
	{
		public int TimeSpent = 0;
		
		public override void SafeSetDefaults()
		{
			Projectile.width = 36;
			Projectile.height = 36;
			Projectile.timeLeft = 900;
			Projectile.scale = 1f;
			Projectile.penetrate = -1;
			Projectile.alpha = 255;
			Projectile.friendly = true;
			Projectile.usesIDStaticNPCImmunity = true;
			Projectile.idStaticNPCHitCooldown = 10;
			Projectile.tileCollide = true;
		}

		public override void OnSpawn(IEntitySource source)
		{
			Projectile.scale = 0f;
			Projectile.originalDamage = Projectile.damage;
			Projectile.damage = (int)(Projectile.originalDamage * 0.1f);
			if (Main.player[Projectile.owner].ownedProjectileCounts[Type] >= 10)
			{
				Projectile oldest = null;
				int maxTimeSpent = 0;
				foreach (Projectile proj in Main.ActiveProjectiles)
				{
					if (proj.type == Type && proj.owner == Projectile.owner && proj.whoAmI != Projectile.whoAmI && proj.ModProjectile is ThoriumNagaQuarterstaffProjectile bubble && bubble.TimeSpent > maxTimeSpent)
					{
						oldest = proj;
						maxTimeSpent = bubble.TimeSpent;
					}
				}
				oldest?.Kill();
			}
		}

		public override void AI()
		{
			TimeSpent++;

			if (Projectile.scale < 1.5f)
			{
				Vector2 oldCenter = Projectile.Center;
				Projectile.scale += 0.05f;
				Projectile.width = (int)(36 * Projectile.scale);
				Projectile.height = (int)(36 * Projectile.scale);
				Projectile.Center = oldCenter;
			}

			Projectile.velocity *= 0.95f;
			if (Projectile.velocity.Length() < 0.1f)
			{
				Projectile.velocity = Vector2.Zero;
				Projectile.Center += Vector2.UnitY * MathF.Sin(TimeSpent * MathHelper.Pi / 135f) * 0.1f;
			}

			Projectile.ai[0] = 15f + MathF.Sin(TimeSpent * MathHelper.Pi / 180f) * 3f;
			Projectile.ai[1] = 15f + MathF.Sin((TimeSpent + 90) * MathHelper.Pi / 180f) * 3f;

			Projectile.rotation = 0.06f * MathF.Sin(TimeSpent * MathHelper.Pi / 150f);

			foreach (Projectile beble in Main.ActiveProjectiles)
			{
				if (beble.type == Type && beble.owner == Projectile.owner && beble.whoAmI != Projectile.whoAmI && (beble.Center - Projectile.Center).Length() <= 18f * (Projectile.scale + beble.scale))
				{
					beble.velocity -= beble.DirectionTo(Projectile.Center) * beble.Distance(Projectile.Center) * 0.25f;
					Projectile.velocity -= Projectile.DirectionTo(beble.Center) * Projectile.Distance(beble.Center) * 0.25f;
					SoundEngine.PlaySound(SoundID.Item154, Projectile.Center);
				}
			}

			if (Projectile.timeLeft == 10 && Projectile.ai[2] == 1)
			{
				Projectile.ResetImmunity();
				Projectile.damage = Projectile.originalDamage;
				SoundEngine.PlaySound(SoundID.Item21, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
				for (int i = 0; i < 3; i++)
				{
					float dustRot = Main.rand.NextFloat(MathHelper.TwoPi);
					bool ccw = Main.rand.NextBool();
					for (int j = 40; j > 0; j--)
					{
						Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GreenFairy);
						dust.velocity = Vector2.UnitX.RotatedBy(dustRot + MathHelper.TwoPi * j / 3f) * (j * 0.30f);
						dust.scale *= 2f - j * 0.012f;
						dust.noGravity = true;
						if (ccw) dustRot -= 0.1f - j * 0.0005f;
						else dustRot += 0.1f - j * 0.00005f;
						dust.alpha = 127;
					}
				}
				// for (int i = 15; i > 0; i--)
				// {
				// 	Dust dust = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.GreenTorch);
				// 	dust.scale *= 0.5f + Main.rand.NextFloat(0.8f);
				// 	dust.velocity.Y *= 2.5f;
				// 	dust.velocity.X *= 4f;
				// }
				
				foreach (Projectile boble in Main.ActiveProjectiles)
				{
					if (boble.type == Type && boble.ai[2] == 0 && boble.owner == Projectile.owner && boble.identity != Projectile.identity && (boble.Center - Projectile.Center).Length() <= 108f * Projectile.scale + 18f * boble.scale)
					{
						boble.ai[2] = 1;
						boble.timeLeft = 10;
					}
				}
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			Bounce(oldVelocity, 0.95f);
			return false;
		}

		public override bool? CanHitNPC(NPC target)
		{
			float radius = 18f * Projectile.scale;
			if (Projectile.ai[2] == 1) radius *= 6f;
			return target.Distance(Projectile.Center) <= radius;
		}

		public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
		{
			if (Projectile.ai[2] != 1f)
			{
				hit.HideCombatText = true;
				CombatText.NewText(target.getRect(), hit.Crit ? CombatText.DamagedHostileCrit : CombatText.DamagedHostile, hit.Damage, hit.Crit, true);
			}
		}

		public override void ModifyDamageHitbox(ref Rectangle hitbox)
		{
			if (Projectile.ai[2] != 1) return;
			
			int size = 90;
			hitbox.X -= size;
			hitbox.Y -= size;
			hitbox.Width += size * 2;
			hitbox.Height += size * 2;
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < (Projectile.ai[2] == 1 ? 40 : 10); i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.BubbleBlock, Main.rand.NextVector2Unit() * Main.rand.NextFloat(Projectile.ai[2] == 1 ? 32f : 8f), newColor: Color.MediumAquamarine, Scale: Main.rand.NextFloat(0.5f, 1f))
					.noGravity = true;

			SoundEngine.PlaySound(SoundID.Item54, Projectile.Center);
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			spriteBatch.End(out SpriteBatchSnapshot snapshot);
			spriteBatch.Begin(snapshot with {SortMode = SpriteSortMode.Immediate, BlendState = BlendState.Additive});
			
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, GetOwnerColor(Projectile.owner, ref lightColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * new Vector2(Projectile.ai[0] / 15f, Projectile.ai[1] / 15f), SpriteEffects.None, 0f);
			
			spriteBatch.End();
			spriteBatch.Begin(snapshot);
			
			return false;
		}

		public static Color GetOwnerColor(int whoAmI, ref Color lightColor)
		{
			if (Main.netMode == NetmodeID.SinglePlayer) return Main.ColorOfTheSkies;
			
			Color playerColor = Color.White;
			
			Player player = Main.player[whoAmI]; 
			Main.rand.SetSeed(player.name.GetHashCode());
			playerColor = Color.Lerp(new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256)), lightColor, 0.5f);
		
			if (player.team != 0)
				playerColor = Color.Lerp(playerColor, Main.teamColor[player.team], 0.6f);
		
			return playerColor;
		}
	}
}