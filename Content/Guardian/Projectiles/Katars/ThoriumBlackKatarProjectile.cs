using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Assets;
using OrchidMod.Content.Guardian.Weapons.Katars;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Projectiles.Katars;

public class ThoriumBlackKatarProjectile : OrchidModGuardianProjectile
{
	public int TimeSpent;

	public ThoriumBlackKatar Katar;

	public Player OwnerClone;
	public (Vector2, float, float) CurrentMainPosition;
	public (Vector2, float, float) CurrentOffPosition;
	public List<Player> RecordedPlayer;
	public List<(Vector2, float, float)> RecordedMainPosition;
	public List<(Vector2, float, float)> RecordedOffPosition;

	public SpriteEffects Effects = SpriteEffects.None;

	public override string Texture => OrchidAssets.InvisiblePath;

	public override void SafeSetDefaults()
	{
		Projectile.width = 20;
		Projectile.height = 42;
		Projectile.timeLeft = 150;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 20;

		RecordedPlayer = [];
		RecordedMainPosition = [];
		RecordedOffPosition = [];
	}

	public override void OnSpawn(IEntitySource source)
	{
		if (source is EntitySource_Parent parent && parent.Entity is Item item && item.ModItem is ThoriumBlackKatar katar)
			Katar = katar;
		
		for (int i = 0; i < 15; i++)
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.SomethingRed, Main.rand.NextVector2Circular(Projectile.width / 16f, Projectile.height / 16f), 45);
			dust.noGravity = true;
		}
	}

	public override void AI()
	{
		if (IsLocalOwner)
		{
			if (Projectile.ai[0] == 0)
			{
				Player record = new();
				record.CopyVisuals(Owner);
				record.compositeFrontArm = Owner.compositeFrontArm;
				record.compositeBackArm = Owner.compositeBackArm;
				record.legFrame = Owner.legFrame;
				record.bodyFrame = Owner.bodyFrame;
				record.headFrame = Owner.headFrame;
				record.wingFrame = Owner.wingFrame;
				record.ResetEffects();
				record.ResetVisibleAccessories();
				record.UpdateDyes();
				record.DisplayDollUpdate();
				record.UpdateSocialShadow();
				record.PlayerFrame();

				RecordedPlayer.Add(record);

				
				int[] anchors = Katar?.GetAnchors(Owner);

				if (anchors != null)
				{
					Projectile mainKatar = Main.projectile[anchors[1]];
					Projectile offKatar = Main.projectile[anchors[0]];
					if (mainKatar != null) RecordedMainPosition.Add((mainKatar.Center, mainKatar.rotation, mainKatar.scale));
					if (offKatar != null) RecordedOffPosition.Add((offKatar.Center, offKatar.rotation, offKatar.scale));
				}
			}
			else
			{
				OwnerClone = RecordedPlayer[TimeSpent - (int)Projectile.ai[1]];
				Projectile.position = OwnerClone.position;

				CurrentMainPosition = RecordedMainPosition[TimeSpent - (int)Projectile.ai[1]];
				CurrentOffPosition = RecordedOffPosition[TimeSpent - (int)Projectile.ai[1]];
				Dust.NewDustPerfect(Projectile.Center + Vector2.UnitY * Main.rand.NextFloat(-21f, 21f), DustID.SomethingRed, -OwnerClone.velocity * 0.5f, 45);
			}
		}

		TimeSpent++;
		if (Projectile.ai[0] == 0 && (TimeSpent == 60 || RecordedPlayer.Count == 60 || Owner.HeldItem.ModItem is not ThoriumBlackKatar || Owner.dead))
		{
			Projectile.ai[0] = 1;
			Projectile.ai[1] = TimeSpent;
			SoundEngine.PlaySound(SoundID.Item73);
		}
	}

	public override void SafeModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		modifiers.DamageVariationScale *= 0;
	}

	public override void OnKill(int timeLeft)
	{
		for (int i = 0; i < 15; i++)
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.SomethingRed, Main.rand.NextVector2Circular(Projectile.width / 16f, Projectile.height / 16f), Scale: 2f);
			dust.noGravity = true;
		}

		SoundEngine.PlaySound(SoundID.NPCDeath6, Projectile.Center);
	}

	public override bool? CanHitNPC(NPC target)
	{
		return Projectile.ai[0] == 1;
	}

	public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
	{
		int[] anchors = Katar?.GetAnchors(Owner);
		if (OwnerClone != null && anchors != null)
		{
			Projectile mainKatar = Main.projectile[anchors[1]];

			Color color = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f), Color.White);

			Texture2D texture = ModContent.Request<Texture2D>(Katar.KatarTexture).Value;

			var effect = SpriteEffects.None;
			if (OwnerClone.direction != 1 && mainKatar?.ModProjectile is GuardianKatarAnchor anchor)
			{
				if (anchor.Charging || anchor.Slamming) effect = SpriteEffects.FlipVertically;
				else effect = SpriteEffects.FlipHorizontally;
			}
			
			float drawRotation = CurrentOffPosition.Item2;
			Vector2 posproj = CurrentOffPosition.Item1;
			if (OwnerClone.gravDir == -1)
			{
				drawRotation = -drawRotation;
				posproj.Y = (OwnerClone.Bottom + OwnerClone.position).Y - posproj.Y + (posproj.Y - OwnerClone.Center.Y) * 2f;
				if (effect == SpriteEffects.FlipVertically)
					effect = SpriteEffects.None;
				else if (effect == SpriteEffects.FlipHorizontally)
				{
					effect = SpriteEffects.None;
					drawRotation += MathHelper.Pi;
				}
				else if (effect == SpriteEffects.None)
					effect = SpriteEffects.FlipVertically;
			}
			var drawPosition = Vector2.Transform(posproj - Main.screenPosition + Vector2.UnitY * OwnerClone.gfxOffY, Main.GameViewMatrix.EffectMatrix);
			
			spriteBatch.Draw(texture, drawPosition - Main.screenPosition, null, color * 0.8f, drawRotation, texture.Size() * 0.5f, CurrentOffPosition.Item3, effect, 0f);

			
			// Draw "player" themselves
			Main.PlayerRenderer.DrawPlayerHead(Main.Camera, OwnerClone, Projectile.position, 0.8f, borderColor: Color.Transparent);
			Main.PlayerRenderer.DrawPlayer(Main.Camera, OwnerClone, Projectile.position, 0f, OwnerClone.fullRotationOrigin, 0.2f);

			
			
			drawRotation = CurrentMainPosition.Item2;
			posproj = CurrentMainPosition.Item1;
			if (OwnerClone.gravDir == -1)
			{
				drawRotation = -drawRotation;
				posproj.Y = (OwnerClone.Bottom + OwnerClone.position).Y - posproj.Y + (posproj.Y - OwnerClone.Center.Y) * 2f;
				if (effect == SpriteEffects.FlipVertically)
					effect = SpriteEffects.None;
				else if (effect == SpriteEffects.FlipHorizontally)
				{
					effect = SpriteEffects.None;
					drawRotation += MathHelper.Pi;
				}
				else if (effect == SpriteEffects.None)
					effect = SpriteEffects.FlipVertically;
			}
			drawPosition = Vector2.Transform(posproj - Main.screenPosition + Vector2.UnitY * OwnerClone.gfxOffY, Main.GameViewMatrix.EffectMatrix);
			
			spriteBatch.Draw(texture, drawPosition - Main.screenPosition, null, color * 0.8f, drawRotation, texture.Size() * 0.5f, CurrentMainPosition.Item3, effect, 0f);
			
		}
		return false;

	}
}