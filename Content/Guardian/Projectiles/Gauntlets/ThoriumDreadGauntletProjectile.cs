using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Content.Guardian.Weapons.Gauntlets;
using OrchidMod.Content.Shapeshifter.Accessories;
using System.IO;
using System.Linq;
using OrchidMod.Content.Guardian.Buffs;
using OrchidMod.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Projectiles.Gauntlets
{
	// Modified version of ThoriumYewGauntletProjectile.cs
	public class ThoriumDreadGauntletProjectile : OrchidModGuardianProjectile
	{
		public Vector2 InitialVelocity = Vector2.Zero;
		public Vector2 NPCImpactPoint = Vector2.Zero;
		public Vector2 NPCImpactVelocity = Vector2.Zero;
		public int TimeSpent;
		public bool Flip;
		public bool QuickPull;

		public override void SafeSetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 120;
			Projectile.penetrate = -1;
			Projectile.friendly = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.alpha = 255;
		}

		public override bool? CanHitNPC(NPC target)
		{
			if (Projectile.ai[1] > -1 || TimeSpent > 25) return false;

			return base.CanHitNPC(target);
		}

		NPC HitNPC;
		public override void AI()
		{
			TimeSpent++;

			if (!Initialized)
			{
				Initialized = true;
				InitialVelocity = Projectile.velocity;
				Flip = Projectile.velocity.X < 0;
				SoundEngine.PlaySound(SoundID.DD2_BallistaTowerShot.WithPitchOffset(0.5f), Projectile.Center);
			}

			Vector2 target = Owner.Center;
			Projectile gauntlet = Main.projectile[(int)Projectile.ai[2]];
			if (gauntlet.active && gauntlet.ModProjectile is GuardianGauntletAnchor && gauntlet.owner == Owner.whoAmI)
			{
				target = gauntlet.Center;
			}
			else if (IsLocalOwner)
				Projectile.Kill();
;
			if (QuickPull)
			{ // this field exists for netsync purposes
				QuickPull = false;
				Owner.velocity = Vector2.Normalize(Projectile.Center - Owner.MountedCenter) * InitialVelocity.Length();
			}

			if (Projectile.ai[1] >= 0)
			{
				Projectile.tileCollide = false;
				HitNPC = Main.npc[(int)Projectile.ai[1]];
				if (HitNPC.active && !HitNPC.friendly && HitNPC.life > 0)
				{
					if (NPCImpactPoint == Vector2.Zero)
					{
						SoundEngine.PlaySound(SoundID.DD2_JavelinThrowersAttack, Projectile.Center);
						NPCImpactPoint = Projectile.Center - HitNPC.Center;
						NPCImpactVelocity = Projectile.velocity;
						Projectile.timeLeft = 60;
						Projectile.friendly = false;

						if (IsLocalOwner)
							Main.SetCameraLerp(0.1f, 10);
					}

					Projectile.Center = NPCImpactPoint - NPCImpactVelocity + HitNPC.Center;
					NPCImpactVelocity *= 0.75f;
					Owner.RemoveAllGrapplingHooks();

					Owner.velocity = Vector2.Normalize(Projectile.Center - Owner.MountedCenter) * InitialVelocity.Length();

					if (Owner.Center.Distance(Projectile.Center) < 32f)
					{
						Projectile.ai[1] = -1;
						TimeSpent = 35;
						Projectile.velocity = Vector2.Normalize(target - Projectile.Center) * InitialVelocity.Length();
						Owner.velocity.X *= 0.5f;
						Owner.velocity.Y *= 0.75f;
					}
				}
				else
				{
					Projectile.ai[1] = -1;
					TimeSpent = 35;
					Projectile.velocity = Vector2.Normalize(target - Projectile.Center) * InitialVelocity.Length();
				}
			}
			else if (TimeSpent > 20)
			{
				if (TimeSpent <= 35)
					Projectile.velocity -= InitialVelocity * (TimeSpent - 20) * 0.02f;
				else
				{
					Projectile.velocity = Vector2.Normalize(target - Projectile.Center) * Projectile.velocity.Length();
					Projectile.tileCollide = false;
				}

				if (target.Distance(Projectile.Center) < 48f)
				{
					OrchidGuardian guardian = Owner.GetModPlayer<OrchidGuardian>();
					if (HitNPC != null && OrchidUtils.CheckCircularvCircularCollision(HitNPC.Center + HitNPC.velocity, HitNPC.GetLargestDimension(), Owner.Center + Owner.velocity, Owner.GetLargestDimension() * 1.33333333f))
					{
						guardian.DoParryItemParry(HitNPC);
						Owner.ApplyDamageToNPC(HitNPC, guardian.GetGuardianDamage(Projectile.damage * 4f), 4f, (Owner.Center.X > target.X).ToDirectionInt(), damageType: Projectile.DamageType);
						HitNPC.AddBuff(BuffID.CursedInferno, 120);
						Owner.AddBuff(ModContent.BuffType<GuardianDreadGauntletBuff>(), 360);
						// guardian.AddGuard();
						//
						// var thoriumMod = OrchidMod.ThoriumMod;
						// if (thoriumMod != null)
						// {
						// 	int projType = thoriumMod.Find<ModProjectile>("DreadParticle").Type;
						// 	Vector2 velocity = Main.rand.NextVector2Unit();
						// 	for (int i = 0; i < 4; i++) Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Owner.Center + velocity, velocity * Main.rand.NextFloat(2f, 6f), projType, guardian.GetGuardianDamage(Projectile.damage * 0.2f), 4f, Owner.whoAmI, 1f);
						// }
					}
					Projectile.Kill();
				}
			}
			else
			{
				Projectile.rotation = Projectile.velocity.ToRotation();
				
				if (Main.rand.NextBool(2))
					Dust.NewDustPerfect(Projectile.Center, DustID.CursedTorch, -Projectile.velocity.RotatedByRandom(MathHelper.Pi / 12f) * Main.rand.NextFloat(0.1f, 0.5f));
			}
		}

		public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
		{
			if (Projectile.timeLeft > 20 && !target.CountsAsACritter)
			{
				if (target.life > 0)
				{
					Projectile.ai[1] = target.whoAmI;
					Projectile.netUpdate = true;
				}
				else if (Owner.HeldItem.ModItem is ThoriumDreadGauntlet gauntlet && gauntlet.PullOnKill)
				{
					QuickPull = true;
					Projectile.netUpdate = true;
				}
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (TimeSpent < 35)
			{
				TimeSpent = 35;
				SoundEngine.PlaySound(SoundID.Dig, Projectile.position);

				Vector2 target = Owner.Center;
				Projectile gauntlet = Main.projectile[(int)Projectile.ai[2]];
				if (gauntlet.active && gauntlet.ModProjectile is GuardianGauntletAnchor && gauntlet.owner == Owner.whoAmI)
					target = gauntlet.Center;
				else if (IsLocalOwner)
					Projectile.Kill();

				Projectile.velocity = Vector2.Normalize(target - Projectile.Center) * InitialVelocity.Length();
			}

			return false;
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(QuickPull);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			QuickPull = reader.ReadBoolean();
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			SpriteEffects spriteEffects = Flip ? SpriteEffects.FlipVertically : SpriteEffects.None;

			Texture2D projTexture = TextureAssets.Projectile[Projectile.type].Value;
			spriteBatch.Draw(projTexture, Projectile.Center - Main.screenPosition, null, Color.GreenYellow, Projectile.rotation, projTexture.Size() * 0.5f, Projectile.scale * 1.1f, spriteEffects, 0f);
			spriteBatch.Draw(projTexture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, projTexture.Size() * 0.5f, Projectile.scale, spriteEffects, 0f);

			return false;
		}
	}
}