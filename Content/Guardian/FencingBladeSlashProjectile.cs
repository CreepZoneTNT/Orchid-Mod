using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Utilities;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian
{
	public class FencingBladeSlashProjectile : OrchidModGuardianAnchor
	{
		private static Asset<Texture2D> TextureSlash;
		private static Asset<Texture2D> TextureStab;
		public OrchidModGuardianFencingBlade FencingBladeItem;
		public GuardianFencingBladeAnchor FencingBladeAnchor;
		
		public bool Stab = false;

		public int TimeSpent = 0;
		public float Scale = 1f;
		public float ScaleMult = 1f;
		public float Stretch;

		public List<Vector2> OldPosition;
		public List<float> OldRotation;
		
		public int HitCount = 0;

		public override void Load()
		{
			TextureSlash ??= ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad);
			TextureStab ??= ModContent.Request<Texture2D>(Texture + "_Stab", AssetRequestMode.ImmediateLoad);
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
				if (!Stab) Projectile.rotation = (Projectile.velocity - Owner.velocity * 0.375f).ToRotation();
				Projectile.localAI[0] = Stab ? 0 : (Main.rand.Next(10) + 1) * 0.001f;
				// Projectile.scale = Scale;
				if (!IsLocalOwner)
				{
					foreach (Projectile projectile in Main.projectile)
					{ // This cannot be reliably synced with packets (?)
						if (projectile.ModProjectile is GuardianFencingBladeAnchor anchor && projectile.owner == Projectile.owner && projectile.active)
						{
							FencingBladeItem = anchor.FencingBladeItem.ModItem as OrchidModGuardianFencingBlade;
							FencingBladeAnchor ??= anchor;
						}
					}
				}
				
				Projectile.netUpdate = true;
			}
			else if (FencingBladeItem.ProjectileAI(Owner, Projectile, Strong))
			{
				Projectile.velocity *= 0.94574f;
				
				if (TimeSpent % 4 == 0)
				{
					OldPosition.Add(Projectile.Center);
					OldRotation.Add(Projectile.rotation);
				}
				
				if (OldPosition.Count > 10)
				{
					OldPosition.RemoveAt(0);
					OldRotation.RemoveAt(0);
				}
				
				if (!Stab)
				{
					Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[0]);
					Projectile.rotation += Projectile.ai[0];
				}
				
			}
			
			float weaponScale = Owner.GetModPlayer<OrchidGuardian>().GuardianWeaponScale * Scale;
			if (weaponScale != 1f)
			{ // re-centers and adjusts projectiles scale + hitbox to match the players
				Vector2 oldCenter = Projectile.Center;
				Projectile.scale = weaponScale;
				Projectile.width = (int)(30 * weaponScale);
				Projectile.height = (int)(30 * weaponScale);
				Projectile.Center = oldCenter;
			}
			
			if (!Stab)
			{
				Stretch += Projectile.localAI[0];
				if (Stretch > 0.5f) Stretch = 0.5f;
			}

			TimeSpent++;
			Scale *= ScaleMult;
		}

		public override void SafeModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.FinalDamage *= 1 - 0.05f * HitCount;
			FencingBladeItem.FencingBladeModifyHitNPC(Owner, Owner.GetModPlayer<OrchidGuardian>(), target, Projectile, ref modifiers, FirstHit);
		}

		public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
		{
			if (Owner.active && !Owner.dead && FencingBladeItem != null) 
			{
				if (FirstHit)
				{
					if (FencingBladeAnchor != null && !FencingBladeAnchor.FirstHit)
					{
						FencingBladeAnchor.FirstHit = true;
						Guardian.AddSlam();
					}
					FencingBladeItem.OnHitFirst(Owner, guardian, target, Projectile, hit);
				}
				FencingBladeItem.OnHit(Owner, guardian, target, Projectile, hit);
				
				HitCount++;
			}
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(Stab);
			writer.Write(Scale);
			writer.Write(ScaleMult);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			Stab = reader.ReadBoolean();
			Scale = reader.ReadSingle();
			ScaleMult = reader.ReadSingle();
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			if (FencingBladeItem != null)
			{
				Texture2D texture = Stab ? TextureStab.Value : TextureSlash.Value;
				spriteBatch.End(out SpriteBatchSnapshot spriteBatchSnapshot);
				spriteBatch.Begin(spriteBatchSnapshot with { BlendState = BlendState.Additive });

				// Draw code here
				float colorMult = 0.8f;
				if (Projectile.timeLeft < 20) colorMult *= Projectile.timeLeft / 20f;
				SpriteEffects effect = SpriteEffects.None;
				if (Projectile.velocity.X < 0f) effect = SpriteEffects.FlipVertically;
				
				Color itemColor = FencingBladeItem.GetColor(Owner, Guardian, Projectile);
				
				Vector2 stretch = (Stab ? Vector2.One : new Vector2(1 + Stretch, 1 - Stretch)) * Scale;
				
				for (int i = 0; i < OldPosition.Count; i++)
				{
					spriteBatch.Draw(texture, OldPosition[i] - Main.screenPosition, null, itemColor * colorMult * ((i + 1) / 10f), OldRotation[i], texture.Size() * 0.5f, stretch, effect, 0f);
				}
				spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, itemColor * colorMult * 1.5f, Projectile.rotation, texture.Size() * 0.5f, stretch * 1.1f, effect, 0f);
				spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, itemColor * colorMult, Projectile.rotation, texture.Size() * 0.5f, stretch, effect, 0f);

				// Draw code ends here

				spriteBatch.End();
				spriteBatch.Begin(spriteBatchSnapshot);
			}
			return false;
		}
	}
}	