using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Projectiles.Misc
{
	public class ConsolariaLepusEggNestProjectile : OrchidModGuardianProjectile
	{
		public override void SafeSetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 180;
			Projectile.scale = 1f;
			Projectile.alpha = 96;
			Projectile.penetrate = 1;
			Projectile.alpha = 255;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void AI()
		{
			if (Projectile.timeLeft < 170)
			{
				Projectile.velocity.X *= 0.95f;
				Projectile.velocity.Y += 0.4f;
			}

			Projectile.rotation += Projectile.velocity.Length() / 30f * (Projectile.velocity.X > 0 ? 1 : -1);
		}

		public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
		{
			if (Projectile.ai[1] == 1f)
			{
				if (guardian.GuardianSlam < guardian.GuardianGuard)
				{
					guardian.AddSlam(1);
				}
				else
				{
					guardian.AddGuard(1);
				}
			}
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item50.WithPitchOffset(Main.rand.NextFloat(1.2f, 1.5f)), Projectile.Center);

			if (OrchidMod.Consolaria != null)
			{
				int goreType = OrchidMod.Consolaria.Find<ModGore>("EggShell").Type;
				for (int index = 0; index < 2; ++index)
				{
					Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, new Vector2(Main.rand.Next(-2, 2), 0.0f), goreType, 1f);
				}
			}
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			Texture2D projTexture = TextureAssets.Projectile[Projectile.type].Value;
			float colorMult = 1f;
			if (Projectile.timeLeft < 5) colorMult *= Projectile.timeLeft / 5f;
			Vector2 drawPosition = Projectile.Center - Main.screenPosition;
			spriteBatch.Draw(projTexture, drawPosition, null, lightColor * colorMult, Projectile.rotation, projTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);

			return false;
		}
	}
}