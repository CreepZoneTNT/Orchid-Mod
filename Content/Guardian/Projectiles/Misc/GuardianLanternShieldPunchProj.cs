using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Content.Guardian.Weapons.Misc;
using OrchidMod.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Projectiles.Misc
{
	public class GuardianLanternShieldPunchProj : OrchidModGuardianAnchor
	{
		public override string Texture => "OrchidMod/Content/Guardian/GauntletPunchProjectile";
		private static Texture2D TextureMain;
		public GuardianLanternShield GuardianItem;
		public bool ChargedHit => Projectile.ai[0] == 1f;
		public bool FirstFrame = false;

		public override void Load()
		{
			TextureMain ??= ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
		}

		public override void SafeSetDefaults()
		{
			Projectile.width = 30;
			Projectile.height = 30;
			Projectile.friendly = true;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 81;
			Projectile.tileCollide = false;
			Projectile.scale = 1f;
			Projectile.alpha = 96;
			Projectile.penetrate = -1;
			Projectile.alpha = 255;
			Projectile.extraUpdates = 3;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 90;
		}

		public override void AI()
		{
			if (!Initialized)
			{
				Initialized = true;
				Projectile.rotation = (Projectile.velocity - Owner.velocity * 0.375f).ToRotation();
				if (ChargedHit) Strong = true;
				if (!IsLocalOwner)
				{
					foreach (Projectile projectile in Main.projectile)
					{ // This cannot be reliably synced with packets (?)
						if (projectile.ModProjectile is GuardianLanternShieldAnchor anchor && projectile.owner == Projectile.owner && projectile.active)
						{
							GuardianItem = anchor.GuardianItem.ModItem as GuardianLanternShield;
						}
					}

					Owner.GetModPlayer<OrchidGuardian>().GuardianItemCharge = 0; // probably not the best place to put this but it works. (fixes a minor visual issue)
					// SoundEngine.PlaySound(ChargedHit ? SoundID.DD2_MonkStaffGroundMiss : SoundID.DD2_MonkStaffSwing, owner.Center);
				}

				
				// Projectile.position += Projectile.velocity * 0.5f;
				Projectile.width = 40;
				Projectile.height = 40;
				Projectile.position.X += 10;
				Projectile.position.Y += 10;
			}
			else
			{
				Projectile.velocity *= 0.97164f;
				
				// Item torchItem = Owner.inventory[GuardianItem.TorchIndex];
				// Dust.NewDustDirect(Projectile.Center - new Vector2(4), 8, 8, torchItem != null ? torchItem.createTile == TileID.Torches ? TorchID.Dust[torchItem.placeStyle] : TileLoader.GetTile(torchItem.createTile).DustType : DustID.Torch);
			}
		}

		public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
		{
			var owner = Main.player[Projectile.owner];
			if (owner.active && !owner.dead && GuardianItem != null)
			{
				if (FirstHit && ChargedHit)
					guardian.AddGuard();
			}
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			if (!FirstFrame) return false;
			
			spriteBatch.End(out SpriteBatchSnapshot spriteBatchSnapshot);
			spriteBatch.Begin(spriteBatchSnapshot with { BlendState = BlendState.Additive });

			// Draw code here
			float colorMult = 0.8f;
			Vector2 offsetVector = new Vector2(0f, 12f).RotatedBy(Projectile.rotation - MathHelper.PiOver2);
			if (Projectile.timeLeft < 10) colorMult *= Projectile.timeLeft / 10f;
			SpriteEffects effect = SpriteEffects.None;
			if (Projectile.velocity.X < 0f) effect = SpriteEffects.FlipVertically;

			float scale = Projectile.scale * (ChargedHit ? 1.5f : 1.2f);
			Vector2 drawPosition = Projectile.Center - offsetVector - Main.screenPosition;
			spriteBatch.Draw(TextureMain, drawPosition, null, Color.DarkSlateGray * colorMult, Projectile.rotation, TextureMain.Size() * 0.5f, scale, effect, 0f);

			// Draw code ends here

			spriteBatch.End();
			spriteBatch.Begin(spriteBatchSnapshot);
			return false;
		}
	}
}	