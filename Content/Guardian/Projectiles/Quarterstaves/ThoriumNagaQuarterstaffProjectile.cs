using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common;
using OrchidMod.Utilities;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace OrchidMod.Content.Guardian.Projectiles.Quarterstaves
{
	public class ThoriumNagaQuarterstaffProjectile : OrchidModGuardianProjectile
	{
		public int TimeSpent = 0;
		
		public override void SafeSetDefaults()
		{
			Projectile.width = 36;
			Projectile.height = 36;
			Projectile.timeLeft = 600;
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
			if (Main.player[Projectile.owner].ownedProjectileCounts[Type] >= 5)
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

			if (Projectile.scale < 1f)
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
				Projectile.Center += Vector2.UnitY * MathF.Sin(TimeSpent * MathHelper.Pi / 135f) * 0.25f;
			}

			Projectile.ai[0] = 15f + MathF.Sin(TimeSpent * MathHelper.Pi / 180f) * 3f;
			Projectile.ai[1] = 15f + MathF.Sin((TimeSpent + 90) * MathHelper.Pi / 180f) * 3f;

			Projectile.rotation = 0.06f * MathF.Sin(TimeSpent * MathHelper.Pi / 150f);

			foreach (Projectile proj in Main.ActiveProjectiles)
			{
				if (proj.type == Type && proj.owner == Projectile.owner && proj.whoAmI != Projectile.whoAmI && proj.Hitbox.Intersects(Projectile.Hitbox))
				{
					proj.velocity -= proj.DirectionTo(Projectile.Center) * proj.Distance(Projectile.Center) * 0.25f;
					Projectile.velocity -= Projectile.DirectionTo(proj.Center) * Projectile.Distance(proj.Center) * 0.25f;
					SoundEngine.PlaySound(SoundID.Item154, Projectile.Center);
				}
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			Bounce(oldVelocity, 0.95f);
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 10; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.BubbleBlock, Main.rand.NextVector2Unit() * Main.rand.NextFloat(8f), newColor: Color.MediumAquamarine, Scale: Main.rand.NextFloat(0.5f, 1f))
					.noGravity = true;
			// for (int i = 0; i < Main.rand.Next(4); i++)
			// {
			// 	Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), Projectile.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 6f), 411);
			// 	gore.type = 411;
			// }

			SoundEngine.PlaySound(SoundID.Item54, Projectile.Center);
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			spriteBatch.End(out SpriteBatchSnapshot snapshot);
			spriteBatch.Begin(snapshot with {BlendState = BlendState.Additive});
			
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.Lerp(lightColor, Main.ColorOfTheSkies, 0.5f), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * new Vector2(Projectile.ai[0] / 15f, Projectile.ai[1] / 15f), SpriteEffects.None, 0f);
			
			spriteBatch.End();
			spriteBatch.Begin(snapshot);
			
			return true;
		}
	}
}