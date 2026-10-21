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
		public OrchidModGuardianGreathammer Greathammer;

		public override void Load()
		{
			TextureMain ??= ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
		}

		public override void SafeSetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.timeLeft = 40;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.extraUpdates = 1;
			Greathammer = null;
		}

		public override void AI()
		{
			if (Projectile.timeLeft == 10)
			{
				Projectile.velocity *= 0.25f;
				for (int i = 0; i < 12; i++)
				{
					Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.Next(2) switch {0 => DustID.MagicMirror, 1 => DustID.Enchanted_Gold, _ => DustID.Enchanted_Pink}); //didn't like how pink looked but leaving if i reconsider
					dust.position += dust.velocity * 16f;
					dust.velocity = dust.velocity * (0.5f + i * 0.1f) + Projectile.velocity - Vector2.UnitY;
					Gore gore = Gore.NewGoreDirect(Main.player[0].GetSource_FromThis(), Projectile.Center, Vector2.UnitY, Main.rand.NextBool(3) ? 16 : 17, Main.rand.NextFloat());
					gore.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
					gore.position += gore.velocity * 16f;
					gore.velocity = gore.velocity * (0.5f + i * 0.1f) + Projectile.velocity;
					gore.velocity.Y *= 1.5f;
				}
				Projectile.velocity = Vector2.Zero;
				Projectile.extraUpdates = 0;
			}
			else if (Projectile.timeLeft > 10)
			{
				if (Projectile.timeLeft == 40)
				{
					Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
				}
				else Projectile.rotation += 0.1f * Projectile.direction;
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

			if (Greathammer != null)
			{
				target.AddBuff(ModContent.BuffType<GreathammerStunDebuff>(), Greathammer.GetStunDuration(player, guardian));
			}
		}

		public override void ModifyDamageHitbox(ref Rectangle hitbox)
		{
			if (Projectile.timeLeft <= 10)
			{
				hitbox.X -= 75;
				hitbox.Y -= 75;
				hitbox.Width += 150;
				hitbox.Height += 150;
			}
			else
			{
				hitbox.X -= 10;
				hitbox.Y -= 10;
				hitbox.Width += 20;
				hitbox.Height += 20;
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
			Color color = new Color(255, 225, 100, 95);
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