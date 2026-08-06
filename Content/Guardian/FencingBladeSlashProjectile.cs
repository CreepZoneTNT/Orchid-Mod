using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common;
using OrchidMod.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian
{
	public class FencingBladeSlashProjectile : OrchidModGuardianAnchor
	{
		private static Texture2D TextureMain;
		public OrchidModGuardianFencingBlade FencingBladeItem;

		public List<Vector2> OldPosition;
		public List<float> OldRotation;

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
			Projectile.localNPCHitCooldown = -1;

			OldPosition = [];
			OldRotation = [];
		}

		public override void AI()
		{
			if (!Initialized)
			{
				Initialized = true;
				if (!IsLocalOwner)
				{
					foreach (Projectile projectile in Main.projectile)
					{ // This cannot be reliably synced with packets (?)
						if (projectile.ModProjectile is GuardianFencingBladeAnchor anchor && projectile.owner == Projectile.owner && projectile.active)
							FencingBladeItem = anchor.FencingBladeItem.ModItem as OrchidModGuardianFencingBlade;
					}
				}

				float scale = Owner.GetModPlayer<OrchidGuardian>().GuardianWeaponScale;
				if (scale != 1f)
				{ // re-centers and adjusts projectiles scale + hitbox to match the players
					Vector2 oldCenter = Projectile.Center;
					Projectile.scale = scale;
					Projectile.width = (int)(Projectile.width * scale);
					Projectile.height = (int)(Projectile.height * scale);
					Projectile.Center = oldCenter;
				}
			}
			else if (FencingBladeItem.ProjectileAI(Owner, Projectile, Strong))
			{
				Projectile.velocity *= 0.94574f;
				if (Strong)
				{
					OldPosition.Add(Projectile.Center);
					OldRotation.Add(Projectile.rotation);

					if (OldPosition.Count > 10)
					{
						OldPosition.RemoveAt(0);
						OldRotation.RemoveAt(0);
					}
					
					Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[0]);
					Projectile.rotation += Projectile.ai[0];
				}
			}
		}

		public override void SafeModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			FencingBladeItem.FencingBladeModifyHitNPC(Owner, Owner.GetModPlayer<OrchidGuardian>(), target, Projectile, ref modifiers, FirstHit);
		}

		public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
		{
			{
				if (FirstHit)
				{
					FencingBladeItem.OnHitFirst(Owner, guardian, target, Projectile, hit);
					
				}
				FencingBladeItem.OnHit(Owner, guardian, target, Projectile, hit);
			}
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			if (FencingBladeItem != null)
			{
				spriteBatch.End(out SpriteBatchSnapshot spriteBatchSnapshot);
				spriteBatch.Begin(spriteBatchSnapshot with { BlendState = BlendState.Additive });

				// Draw code here
				float colorMult = 0.8f;
				if (Projectile.timeLeft < 10) colorMult *= Projectile.timeLeft / 10f;
				SpriteEffects effect = SpriteEffects.None;
				if (Projectile.velocity.X < 0f) effect = SpriteEffects.FlipHorizontally;

				for (int i = 0; i < OldPosition.Count; i++)
				{
					spriteBatch.Draw(TextureMain, OldPosition[i] - Main.screenPosition, null, FencingBladeItem.GetColor() * colorMult * (i / 11f), OldRotation[i], TextureMain.Size() * 0.5f, Projectile.scale, effect, 0f);
				}
				spriteBatch.Draw(TextureMain, Projectile.Center - Main.screenPosition, null, FencingBladeItem.GetColor() * colorMult, Projectile.rotation, TextureMain.Size() * 0.5f, Projectile.scale, effect, 0f);

				// Draw code ends here

				spriteBatch.End();
				spriteBatch.Begin(spriteBatchSnapshot);
			}
			return false;
		}
	}
}	