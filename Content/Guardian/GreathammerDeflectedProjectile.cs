using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Content.Guardian.Buffs.Debuffs;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian
{
	public class GreathammerDeflectedProjectile : OrchidModGuardianProjectile
	{
		private static Texture2D TextureMain;

		public override void Load()
		{
			TextureMain ??= ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
		}

		public override void SafeSetDefaults()
		{
			Projectile.width = 30;
			Projectile.height = 30;
			Projectile.friendly = true;
			Projectile.timeLeft = 50;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.extraUpdates = 1;
		}

		public override void AI()
		{
			if (Projectile.timeLeft == 10)
			{
				Projectile.velocity *= 0.5f;
				for (int i = 0; i < 10; i++)
				{
					Dust.NewDustPerfect(Projectile.Center, Main.rand.Next(3) switch {0 => DustID.MagicMirror, 1 => DustID.Enchanted_Gold, _ => DustID.Enchanted_Pink}, Projectile.velocity);
					Gore.NewGore(Main.player[0].GetSource_FromThis(), Projectile.Center, Projectile.velocity, Main.rand.NextBool(3) ? 16 : 17);
				}
				Projectile.velocity = Vector2.Zero;
				Projectile.extraUpdates = 0;
			}
			else if (Projectile.timeLeft > 10)
			{
				Projectile.rotation += 0.1f * Projectile.direction;
			}
		}

		public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
		{
			if (Projectile.timeLeft > 11)
			{
				Projectile.timeLeft = 11;
				Projectile.netUpdate = true;
				Projectile.velocity = Vector2.Zero;
			}
			if (Projectile.ai[0] == 0)
			{
				Projectile.ai[0] = 1;
				SoundEngine.PlaySound(SoundID.Item110.WithPitchOffset(-0.3f), Projectile.Center);
			}
			target.AddBuff(ModContent.BuffType<GreathammerStunDebuff>(), 90);
		}

		public override void ModifyDamageHitbox(ref Rectangle hitbox)
		{
			if (Projectile.timeLeft <= 10)
			{
				hitbox.X -= 60;
				hitbox.Y -= 60;
				hitbox.Width += 120;
				hitbox.Height += 120;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (Projectile.timeLeft > 11)
			{
				Projectile.timeLeft = 11;
				Projectile.velocity *= 0.8f;
			}
			return false;
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			float scale = 1;
			Color color = new Color(255, 225, 100, 63);
			if (Projectile.timeLeft <= 10)
			{
				scale = 6 - Projectile.timeLeft * 0.5f;
				color *= 0.1f * Projectile.timeLeft;
			}
			spriteBatch.Draw(TextureMain, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, new Vector2(17, 18), scale, SpriteEffects.None, 0);
			return false;
		}
	}
}	