using System;
using Microsoft.Xna.Framework;
using OrchidMod.Common.ModObjects;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Assets;
using OrchidMod.Content.Guardian.Weapons.Quarterstaves;
using OrchidMod.Utilities;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Projectiles.Quarterstaves
{
	public class ThoriumNagaQuarterstaffProjectileAlt : OrchidModGuardianProjectile
	{
		public List<Vector2> OldPosition;
		public List<float> OldRotation;

		public override void SafeSetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 81;
			Projectile.scale = 1f;
			Projectile.penetrate = -1;
			Projectile.alpha = 255;
			Projectile.extraUpdates = 1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 60;
			Projectile.tileCollide = false;
			Strong = true;

			OldPosition = [];
			OldRotation = [];
		}

		public override void AI()
		{
			Projectile.velocity *= 0.95f;
			
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
			if (Projectile.ai[0] == 0)
			{
				foreach (Projectile buble in Main.ActiveProjectiles)
				{
					if (buble.type == ModContent.ProjectileType<ThoriumNagaQuarterstaffProjectile>() && buble.ai[2] == 0 && buble.owner == Projectile.owner && (buble.Center - Projectile.Center).Length() <= 18f * buble.scale + 20f * Projectile.scale)
					{
						buble.ai[2] = 1;
						buble.timeLeft = 10;

						Projectile.ai[0] = 1;
						break;
					}
				}
			}

			if (Main.rand.NextBool(3))
			{
				Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.1f, Projectile.width * 0.5f), DustID.GreenFairy, Projectile.velocity * Main.rand.NextFloat(0.25f), Scale: 1.5f, newColor: Color.DarkCyan);
				dust.noGravity = true;
			}

			OldPosition.Add(Projectile.Center);
			OldRotation.Add(Projectile.rotation);
			if (OldPosition.Count > 10)
			{
				OldPosition.RemoveAt(0);
				OldRotation.RemoveAt(0);
			}
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			spriteBatch.End(out SpriteBatchSnapshot snapshot);
			spriteBatch.Begin(snapshot with {BlendState = BlendState.Additive, SortMode = SpriteSortMode.Immediate});

			float colorMult = 0.8f;
			if (Projectile.timeLeft < 20) colorMult *= Projectile.timeLeft / 20f;
			
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Rectangle frame1 = texture.Frame(1, 2, 0, 0);
			Rectangle frame2 = texture.Frame(1, 2, 0, 1);

			for (int i = 0; i < OldPosition.Count; i++)
			{
				spriteBatch.Draw(texture, OldPosition[i] - Main.screenPosition, frame1, Color.Teal * colorMult * (0.5f + 0.02f * i), OldRotation[i], frame1.Size() * 0.5f, Projectile.scale * (0.8f + 0.02f * i), SpriteEffects.None, 0f);
				spriteBatch.Draw(texture, OldPosition[i] - Main.screenPosition, frame2, Color.Gold * colorMult * (0.5f + 0.02f * i), OldRotation[i], frame1.Size() * 0.5f, Projectile.scale * (0.8f + 0.02f * i), SpriteEffects.None, 0f);
			}
			spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + Projectile.velocity * 0.1f, frame1, Color.Teal * colorMult * 0.9f, Projectile.rotation, frame1.Size() * 0.5f, Projectile.scale * 1.1f, SpriteEffects.None, 0f);
			spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, frame1, Color.Teal * colorMult, Projectile.rotation, frame1.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
			spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, frame2, Color.Gold * colorMult, Projectile.rotation, frame2.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
			
			spriteBatch.End();
			spriteBatch.Begin(snapshot);
			return false;
		}
	}
}