using System.Collections.Generic;
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
	public List<Vector2> OldPosition;
	public List<float> OldRotation;
	
	public Vector2 OrigVelocity;
	public float ChargeSpeed;
	public int HitCount;
	
	public override void SafeSetDefaults()
	{
		Projectile.width = 82;
		Projectile.height = 82;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.aiStyle = -1;
		Projectile.timeLeft = 270;
		Projectile.scale = 1f;
		Projectile.penetrate = -1;
		Projectile.alpha = 255;
		Projectile.tileCollide = false;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 30;
		OldPosition = [];
		OldRotation = [];
		HitCount = 0;
	}

	public static readonly SoundStyle WindChimes = new("ThoriumMod/Sounds/Item/WindChimes_Sound");

	SlotId soundSlot;

	public override void OnSpawn(IEntitySource source)
	{
		OrigVelocity = Projectile.velocity;
		Projectile.velocity *= float.Epsilon;
	}

	public override void AI()
	{
		Projectile.direction = Projectile.velocity.X > 0 ? 1 : -1;
		Projectile.spriteDirection = Projectile.direction;

		OldPosition.Add(Projectile.Center);
		OldRotation.Add(Projectile.rotation);

		switch (Projectile.ai[1])
		{
			case 0:
				Projectile.alpha -= 5;
				if (Projectile.alpha <= 0)
				{
					Projectile.alpha = 0;
					SoundEngine.PlaySound(SoundID.Item30, Projectile.Center);
					Projectile.ai[1]++;
				}

				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;

				if (Main.rand.NextBool(3))
				{
					Gore smoke = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), Projectile.Center, Main.rand.NextVector2CircularEdge(0.2f, 0.2f), Main.rand.Next(375, 378), Main.rand.NextFloat(1f, 2f));
					smoke.rotation = Main.rand.NextFloat();
					smoke.alpha = 225;
				}

				Projectile parent = Main.projectile[(int)Projectile.ai[0]];
				if (parent.active && parent.owner == Main.myPlayer && parent.ModProjectile is GuardianQuarterstaffAnchor anchor && anchor.QuarterstaffItem.ModItem is ThoriumBoreanStriderQuarterstaff frostWalker && parent.ai[0] > 1f)
					Projectile.Center = frostWalker.Tip;
				
				Vector2 oldCenter = Projectile.Center;
				float scale = Projectile.Opacity;
				Projectile.scale = scale;
				Projectile.width = (int)(82 * scale);
				Projectile.height = (int)(82 * scale);
				Projectile.Center = oldCenter;

				Projectile.timeLeft++;
				break;
			case 1:
				Projectile.velocity = OrigVelocity;
				Projectile.rotation += 0.05f * Projectile.direction;
				if (Main.rand.NextBool(4))
				{
					float direction = Projectile.velocity.ToRotation();
					Dust.NewDustPerfect(Projectile.Center, DustID.IceRod, Main.rand.NextVector2Unit(direction - MathHelper.Pi / 6f, MathHelper.Pi / 3f) * 10.25f * Projectile.scale);
				}
				
				if (Projectile.soundDelay == 0)
				{
					SoundEngine.PlaySound(WindChimes with { Volume = 0.4f, PitchVariance = 0.8f }, Projectile.Center);
					Projectile.soundDelay = 20;
				}
				SoundEngine.TryGetActiveSound(soundSlot, out ActiveSound activeSound);
				if (activeSound == null)
				{
					var tracker = new ProjectileAudioTracker(Projectile);
					soundSlot = SoundEngine.PlaySound(SoundID.BlizzardStrongLoop, Projectile.Center, sound =>
					{
						sound.Position = Projectile.Center;
						sound.Pitch = -0.8f;
						sound.Volume = Projectile.Opacity;
						return tracker.IsActiveAndInGame();
					});
				}
				
				if (Projectile.timeLeft < 50)
				{
					Projectile.alpha += 5;
					if (Projectile.alpha > 255) Projectile.Kill();
				}
				
				break;
		}
		
		Lighting.AddLight(Projectile.Center, Color.LightBlue.ToVector3() * Projectile.Opacity * 2f);
		
		if (OldPosition.Count > 20)
		{
			OldPosition.RemoveAt(0);
			OldRotation.RemoveAt(0);
		}
	}

	public override bool? CanHitNPC(NPC target)
	{
		return Projectile.ai[1] >= 1;
	}

	public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
	{
		Mod thoriumMod = OrchidMod.ThoriumMod;
		if (thoriumMod != null)
		{
			int debuffType = thoriumMod.Find<ModBuff>("Freezing").Type;
			target.AddBuff(debuffType, 120);
		}

		if (HitCount < 40)
		{
			int toAdd = 2;
			if (target.aiStyle == NPCAIStyleID.Worm && target.type != NPCID.SolarCrawltipedeTail && target.type != NPCID.StardustWormHead) toAdd = 1;
			HitCount += toAdd;
		}
	}

	public override void SafeModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		modifiers.FinalDamage *= 1 - 0.02f * HitCount;

		if (target.aiStyle == NPCAIStyleID.Worm && target.type != NPCID.SolarCrawltipedeTail && target.type != NPCID.StardustWormHead)
		{
			// attacking a worm, exception for crawltipedes and milkyway weavers
			modifiers.FinalDamage *= 0.5f;
		}
	}

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.Item50, Projectile.Center);
	}

	public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
	{
		spriteBatch.End(out SpriteBatchSnapshot snapshot);
		spriteBatch.Begin(snapshot with { BlendState = BlendState.Additive, SamplerState = SamplerState.PointClamp});
		
		Texture2D projTexture = TextureAssets.Projectile[Projectile.type].Value;
		SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		lightColor = Lighting.GetColor((int)(Projectile.Center.X / 16), (int)(Projectile.Center.Y / 16), Color.White);
		
		for (int i = 0; i < OldRotation.Count; i++)
		{
			Vector2 drawPosition = OldPosition[i] - Main.screenPosition;
			spriteBatch.Draw(projTexture, drawPosition, null, lightColor * 0.05f * (i + 1) * Projectile.Opacity, OldRotation[i], projTexture.Size() * 0.5f, Projectile.scale * 0.75f, effects, 0f);
		}
		
		spriteBatch.Draw(projTexture, Projectile.Center - Main.screenPosition, null, Color.CadetBlue * Projectile.Opacity, Projectile.rotation, projTexture.Size() * 0.5f, Projectile.scale * 0.75f, effects, 0f);
		spriteBatch.Draw(projTexture, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, projTexture.Size() * 0.5f, Projectile.scale * 0.5f, effects, 0f);
		
		spriteBatch.End();
		spriteBatch.Begin(snapshot);
		
		return false;
	}
}