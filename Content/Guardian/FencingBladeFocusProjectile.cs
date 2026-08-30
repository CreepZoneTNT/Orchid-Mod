using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common;
using OrchidMod.Utilities;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian
{
	public class FencingBladeFocusProjectile : OrchidModGuardianAnchor
	{
		private static Asset<Texture2D> TextureMain;
		public OrchidModGuardianFencingBlade FencingBladeItem;
		public FencingBladeAttackProfile FencingBladeProfile;

		public List<Vector2> OldPosition;
		public List<float> OldRotation;
		public List<int> OldFrame;
		
		public int Frame = 0;
		public int TimeSpent = 0;
		public float StabTimer = 0;
		public int DamageReset = 0;
		
		public static float[] OrdinalAngles = [MathHelper.PiOver4, 3 * MathHelper.PiOver4, 5 * MathHelper.PiOver4, 7 * MathHelper.PiOver4];
		public List<float> NextDirection;

		public override void Load()
		{
			TextureMain ??= ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad);
		}

		public override void SafeSetDefaults()
		{
			Projectile.width = 30;
			Projectile.height = 30;
			Projectile.friendly = true;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 151;
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
			OldFrame = [];
			NextDirection = [];
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

				for (int i = 0; i < 4; i++)
					for (int t = 0; t < 20; t++)
					{
						int index = Main.rand.Next(4);
						if (!NextDirection.Contains(OrdinalAngles[index])) 
							NextDirection.Add(OrdinalAngles[index]); 
					}
			}
			else
			{
				Projectile.velocity *= Strong ? 0.98f : 0.95f;

				Projectile.rotation = Projectile.velocity.ToRotation();
				
				if (TimeSpent % 4 == 0)
				{
					OldPosition.Add(Projectile.Center);
					OldRotation.Add(Projectile.rotation);
					OldFrame.Add(Frame);
				}
			
				if (OldPosition.Count > (Strong ? 10 : 5))
				{
					OldPosition.RemoveAt(0);
					OldRotation.RemoveAt(0);
					OldFrame.RemoveAt(0);
				}
				
				if ((int)StabTimer + 1 > DamageReset && DamageReset <= FencingBladeProfile.Quantity)
				{
					float nextAngle = NextDirection[0] + Main.rand.NextFloat(-FencingBladeProfile.BendAmount, FencingBladeProfile.BendAmount);
					if (FencingBladeProfile.FocusRotates) nextAngle = MathHelper.WrapAngle(nextAngle + Projectile.rotation);
					Vector2 velocity = Vector2.UnitX.RotatedBy(nextAngle) * 10f;
				
					Projectile newProj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FencingBladeSlashProjectile>(), 1, 1f, Owner.whoAmI);
					if (newProj.ModProjectile is FencingBladeSlashProjectile slashProj)
					{
						slashProj.FencingBladeItem = FencingBladeItem;
						slashProj.Strong = Strong;
						slashProj.Scale = FencingBladeProfile.Scale;
						slashProj.ScaleMult = FencingBladeProfile.ScaleChange;
						slashProj.Stab = true;
						newProj.Center += velocity;
						newProj.velocity = -velocity;
						newProj.rotation = newProj.velocity.ToRotation();
						newProj.damage = Projectile.damage;
						newProj.CritChance = (int)(Owner.GetCritChance<GuardianDamageClass>() + Owner.GetCritChance<GenericDamageClass>() + Projectile.CritChance);
						newProj.knockBack = Projectile.knockBack;

						newProj.netUpdate = true;
					}
					else
						newProj.Kill();
					
					NextDirection.RemoveAt(0);
					
					for (int t = 0; t < 20; t++)
					{
						int index = Main.rand.Next(4);
						if (!NextDirection.Contains(OrdinalAngles[index])) 
							NextDirection.Add(OrdinalAngles[index]); 
					}
							
					Projectile.netUpdate = true;
					DamageReset++;
				}
				
				if (TimeSpent % 2 == 0)
				{
					Frame++;
					if (Frame > 3) Frame = 0;
				}
				
				TimeSpent++;
				StabTimer += FencingBladeProfile.Quantity/120f;
			}
		}

		public override void SafeModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.FinalDamage *= 0.2f;
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			if (FencingBladeItem != null)
			{
				spriteBatch.End(out SpriteBatchSnapshot spriteBatchSnapshot);
				spriteBatch.Begin(spriteBatchSnapshot with { BlendState = BlendState.Additive });

				// Draw code here
				float colorMult = 0.8f;
				if (Projectile.timeLeft < 20) colorMult *= Projectile.timeLeft / 20f;
				
				Color itemColor = FencingBladeItem.GetColor(Owner, Guardian, Projectile);

				Texture2D texture = TextureMain.Value;
				Rectangle frame = texture.Frame(1, 4, 0, Frame);

				for (int i = 0; i < OldPosition.Count; i++)
				{
					Rectangle oldFrame = texture.Frame(1, 4, 0, OldFrame[i]);
					spriteBatch.Draw(texture, OldPosition[i] - Main.screenPosition, oldFrame, itemColor * colorMult * ((i + 1) / 10f), OldRotation[i], oldFrame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
				}
				spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, frame, itemColor * colorMult * 1.5f, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale * 1.1f, SpriteEffects.None, 0f);
				spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, frame, itemColor * colorMult, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);

				// Draw code ends here

				spriteBatch.End();
				spriteBatch.Begin(spriteBatchSnapshot);
			}
			return false;
		}
	}
}	