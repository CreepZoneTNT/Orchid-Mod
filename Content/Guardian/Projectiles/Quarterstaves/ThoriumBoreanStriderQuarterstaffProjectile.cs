using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Content.Guardian.Weapons.Quarterstaves;
using OrchidMod.Utilities;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using SpriteBatchSnapshot = OrchidMod.Utilities.SpriteBatchSnapshot;

namespace OrchidMod.Content.Guardian.Projectiles.Quarterstaves;

public class ThoriumBoreanStriderQuarterstaffProjectile : OrchidModGuardianProjectile
{
	// public override string Texture => Assets.OrchidAssets.MiscPath + "Extra_14";
	
	public List<Vector2> OldPosition;
	public List<float> OldRotation;

	public Projectile QuarterstaffProj;
	public Vector2 OrigVelocity;
	public float Scale = 0f;
	public int HitCount;
	/// <summary></summary>
	public bool Ready;
	
	public override void SafeSetDefaults()
	{
		Projectile.width = 36;
		Projectile.height = 36;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.aiStyle = -1;
		Projectile.timeLeft = 180;
		Projectile.scale = 1f;
		Projectile.penetrate = -1;
		Projectile.extraUpdates = 1;
		Projectile.alpha = 255;
		Projectile.tileCollide = false;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 30;
		OldPosition = [];
		OldRotation = [];
		HitCount = 0;
	}
	
	public override void OnSpawn(IEntitySource source)
	{
	}

	public override void AI()
	{
		Projectile.direction = Projectile.velocity.X > 0 ? 1 : -1;
		Projectile.spriteDirection = Projectile.direction;
		
		if (!Ready)
		{
			// Always called on first tick
			if (!Initialized)
			{
				Initialized = true;
				Ready = false;
				
				OrigVelocity = Projectile.velocity;
				Projectile.velocity *= float.Epsilon;
				
				Projectile parent = Main.projectile[(int)Projectile.ai[0]];
				if (parent.active && parent.owner == Projectile.owner && parent.ModProjectile is GuardianQuarterstaffAnchor anchor && anchor.QuarterstaffItem.ModItem is ThoriumBoreanStriderQuarterstaff)
					QuarterstaffProj = parent;
				
				SoundEngine.PlaySound(SoundID.Item30, Projectile.Center);
				
				Gore smoke = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), Projectile.Center, Main.rand.NextVector2CircularEdge(0.2f, 0.2f), Main.rand.Next(375, 378), Main.rand.NextFloat(1f, 2f));
				smoke.rotation = Main.rand.NextFloat();
				smoke.alpha = 225;
				
				if (Strong)
				{
					Projectile.penetrate = 20;
				}
				else
				{
					Projectile.penetrate = 5;
					Projectile.timeLeft = 61;
				}
			}
			
			if (Strong)
			{
				Projectile.alpha -= 5;

				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
				Projectile.Center = Owner.MountedCenter;
				if (QuarterstaffProj != null && Projectile.ai[1] == 1)
					Projectile.Center += Vector2.UnitY.RotatedBy(QuarterstaffProj.ai[1]) * QuarterstaffProj.width * 0.1f;
				

				if (Main.rand.NextBool(3))
				{
					Gore smoke = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), Projectile.Center, Main.rand.NextVector2CircularEdge(0.2f, 0.2f), Main.rand.Next(375, 378), Main.rand.NextFloat(1f, 2f));
					smoke.rotation = Main.rand.NextFloat();
					smoke.alpha = 225;
				}
				
				Scale = Projectile.Opacity * 1.5f;
				SetSize(Scale);

				Projectile.timeLeft++;
			}
			else
			{
				SetSize(0.4f);
				Projectile.alpha = 0;
			}

			if (Projectile.alpha <= 0)
			{
				Ready = true;
				Projectile.velocity = OrigVelocity;
			}
		}
		else
		{
			if (IsLocalOwner)
			{
				// Skew code borrowed from DungeonQuarterstaffProjectile.cs
				Vector2 skew = Main.MouseWorld - Projectile.Center;
				float toMouse = MathHelper.WrapAngle(Projectile.velocity.ToRotation() - skew.ToRotation());

				if ((Strong && Projectile.velocity.Length() > 0.1f) || !Strong)
				{
					if (Projectile.ai[2] != 0)
					{
						if (Math.Abs(toMouse + Projectile.ai[2]) > Math.Abs(toMouse) || Math.Abs(toMouse) > MathHelper.TwoPi / 3)
						{
							Projectile.ai[0] = 0;
							Projectile.netUpdate = true;
						}
					}

					if (Projectile.ai[2] == 0)
					{
						if (Math.Abs(toMouse) < MathHelper.PiOver2 && Math.Abs(toMouse) > 0.15f)
						{
							float bendAmount = Strong ? 0.0063f : 0.0157f;
							Projectile.ai[2] = bendAmount * -(toMouse > 0).ToDirectionInt();
							Projectile.netUpdate = true;
						}
					}
				}
				else
					Projectile.ai[2] = 0;
			}
			
			if (Strong)
			{
				Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[2]) * 0.98f;
				
				if (Projectile.velocity.Length() is <= 0.1f and > float.Epsilon)
					Projectile.velocity = OrigVelocity * float.Epsilon;
				
				Lighting.AddLight(Projectile.Center, Color.LightBlue.ToVector3() * Projectile.Opacity * 1.5f);
			}
			else
			{
				if (QuarterstaffProj?.ai[0] == 0 || Guardian.GuardianItemCharge == 0)
					Projectile.ai[1] = 0;
			
				Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[2]) * (Projectile.timeLeft < 30 ? 0.8f : 1.05f);
				
				Lighting.AddLight(Projectile.Center, Color.LightBlue.ToVector3() * Projectile.Opacity * 0.5f);
			}
		}
		
		if (Projectile.timeLeft < 18)
		{
			Projectile.alpha += 15;
			if (Projectile.alpha > 255)
				Projectile.alpha = 255;
		}
		
		Projectile.rotation += 0.03f * Projectile.direction;

		if (Projectile.timeLeft % 4 == 0)
		{
			OldPosition.Add(Projectile.Center);
			OldRotation.Add(Projectile.rotation);
		}
		
		if (OldPosition.Count > 10)
		{
			OldPosition.RemoveAt(0);
			OldRotation.RemoveAt(0);
		}
	}

	public override bool? CanHitNPC(NPC target) => Ready;

	public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
	{
		Mod thoriumMod = OrchidMod.ThoriumMod;
		if (thoriumMod != null)
		{
			int debuffType = thoriumMod.Find<ModBuff>("Freezing").Type;
			target.AddBuff(debuffType, 120);
		}

		if (Strong)
		{
			int toAdd = 2;
			if (target.aiStyle == NPCAIStyleID.Worm && target.type != NPCID.SolarCrawltipedeTail && target.type != NPCID.StardustWormHead) toAdd = 1;
			HitCount += toAdd;
			
			if (HitCount < 24)
				SetSize(1.5f * (1 - 0.0333f * HitCount));
			else Projectile.Kill();

			SoundEngine.PlaySound(SoundID.Item50, Projectile.Center);

			if (FirstHit && Projectile.ai[1] == 1)
			{
				int guardsToAdd = 2;
				int slamsToAdd = 2;
				
				if (QuarterstaffProj?.ModProjectile is GuardianQuarterstaffAnchor anchor && anchor.QuarterstaffItem.ModItem is ThoriumBoreanStriderQuarterstaff frostWalker)
				{
					guardsToAdd = frostWalker.GuardStacks;
					slamsToAdd = frostWalker.SlamStacks;
				}
				Guardian.AddGuard(guardsToAdd);
				Guardian.AddSlam(slamsToAdd);
				
				SoundEngine.PlaySound(SoundID.Item4);
				for (int i = 0; i < 15; i++)
				{
					Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.FrostHydra, Main.rand.NextVector2CircularEdge(3f, 3f), Scale: 2f);
					dust.noGravity = true;
				}
			}
		}
		else 
		{
			if (FirstHit && Projectile.ai[1] == 1)
			{
				Guardian.GuardianItemCharge += 30f;
				if (Guardian.GuardianItemCharge >= 180f)
					Guardian.GuardianItemCharge = 180f;
			}
		}
		
		for (int i = 0; i < 15; i++)
		{
			Dust dust = Dust.NewDustDirect(target.Center, 10, 10, DustID.MagicMirror, Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(-8f, 8f), 175, Scale: 1.5f);
			dust.noGravity = true;
		}

		if (FirstHit)
			FirstHit = false;
	}

	public override void SafeModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		modifiers.FinalDamage *= 1 - 0.0333f * HitCount;
		if (Strong && Projectile.ai[1] == 0)
			modifiers.FinalDamage *= 0.75f;

		if (target.aiStyle == NPCAIStyleID.Worm && target.type != NPCID.SolarCrawltipedeTail && target.type != NPCID.StardustWormHead)
		{
			// attacking a worm, exception for crawltipedes and milkyway weavers
			modifiers.FinalDamage *= 0.5f;
		}
	}

	public override void OnKill(int timeLeft)
	{
	}

	public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
	{
		spriteBatch.End(out SpriteBatchSnapshot snapshot);
		spriteBatch.Begin(snapshot with { BlendState = BlendState.Additive, SamplerState = SamplerState.PointClamp});
		
		Texture2D projTexture = TextureAssets.Projectile[Projectile.type].Value;
		Texture2D glowTexture = ModContent.Request<Texture2D>(Assets.OrchidAssets.MiscPath + "Extra_14").Value;
		SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		
		for (int i = 0; i < OldRotation.Count; i++)
		{
			Vector2 drawPosition = OldPosition[i] - Main.screenPosition;
			spriteBatch.Draw(glowTexture, drawPosition, null, Color.CadetBlue * (0.5f + 0.05f * (i + 1)) * Projectile.Opacity, OldRotation[i], glowTexture.Size() * 0.5f, Projectile.scale * 1.1f, effects, 0f);
		}
		
		spriteBatch.Draw(glowTexture, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, glowTexture.Size() * 0.5f, Projectile.scale * 1.05f, effects, 0f);
		spriteBatch.Draw(projTexture, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, projTexture.Size() * 0.5f, Projectile.scale, effects, 0f);
		
		spriteBatch.End();
		spriteBatch.Begin(snapshot);
		
		return false;
	}

	public void SetSize(float scale)
	{
		Vector2 oldCenter = Projectile.Center;
		// scale /= 2f;
		Projectile.scale = scale;
		Projectile.width = (int)(80 * scale);
		Projectile.height = (int)(80 * scale);
		Projectile.Center = oldCenter;
	}
}