using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common.ModObjects;
using OrchidMod.Utilities;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Projectiles.Gauntlets
{
	public class ThoriumDreadGauntletProjectileAlt : OrchidModGuardianProjectile
	{
		public List<Vector2> OldPosition;
		public List<float> OldRotation;

		public override void SafeSetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 60;
			Projectile.scale = 1f;
			Projectile.alpha = 96;
			Projectile.penetrate = -1;
			Projectile.alpha = 255;
			OldPosition = [];
			OldRotation = [];
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void AI()
		{
			OldPosition.Add(Projectile.Center);
			Projectile.rotation = Projectile.velocity.ToRotation();
			if (OldPosition.Count > 5) OldPosition.RemoveAt(0);

			Projectile.ai[1]++;
			if (Projectile.ai[1] > 20) Projectile.velocity *= 0.8f;
			
			Lighting.AddLight(Projectile.Center, Color.GreenYellow.ToVector3() * 1.25f);
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			SoundEngine.PlaySound(SoundID.Dig);
			return true;
		}

		public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
		{
			target.AddBuff(BuffID.CursedInferno, 180);
			if (FirstHit && !player.dead)
			{
				guardian.GuardianSlamRecharging += guardian.GauntletSlamPool;
				guardian.GauntletSlamPool *= 0.2f;
			}
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			spriteBatch.End(out SpriteBatchSnapshot spriteBatchSnapshot);
			spriteBatch.Begin(spriteBatchSnapshot with { BlendState = BlendState.Additive });

			// Draw code here

			Texture2D texture = TextureAssets.Projectile[Type].Value;
			float colorMult = 1f;
			if (Projectile.timeLeft < 10) colorMult *= Projectile.timeLeft / 10f;

			for (int i = 0; i < OldPosition.Count; i++)
			{
				Vector2 drawPositionTrail = OldPosition[i] - Main.screenPosition;
				spriteBatch.Draw(texture, drawPositionTrail, null, Color.GreenYellow * 0.2f * (i + 1) * colorMult, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * (i + 1) * 0.20f, SpriteEffects.None, 0f);
			}

			// Draw code ends here

			spriteBatch.End();
			spriteBatch.Begin(spriteBatchSnapshot);

			Vector2 drawPosition = Projectile.Center - Main.screenPosition;
			spriteBatch.Draw(texture, drawPosition, null, lightColor * colorMult, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale + Projectile.ai[0] * 0.1f, SpriteEffects.None, 0f);
			return false;
		}
	}
}