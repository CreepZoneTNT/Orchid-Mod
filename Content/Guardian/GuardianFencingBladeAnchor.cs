using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common;
using OrchidMod.Common.ModObjects;
using OrchidMod.Content.General.Prefixes;
using OrchidMod.Content.Guardian.Misc;
using OrchidMod.Utilities;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian;

public class GuardianFencingBladeAnchor : OrchidModGuardianParryAnchor
{
	public bool NeedNetUpdate;

	public int DamageReset = 0;
	public bool? Ding; // Unlike most other `Ding` flags, I use a bool? for the Fencing Blade's `Ding` as a "ternary boolean": either no ding (null), semi-ding (false), or ding (true) 
	
	public int SelectedItem { get; set; } = -1;
	public Item FencingBladeItem => Owner.inventory[SelectedItem];
	
	public List<Vector2> OldPosition;
	public List<float> OldRotation;

	public Texture2D BladeTexture;
	public Texture2D SheathTexture;

	public bool Visible = false;
	
	/// <summary>The vertical frame of the </summary>
	public int UseFrame { get; set; } = 0;

	public int AnimFrame = 0;
	
	public override void SafeSetDefaults()
	{
		Projectile.width = 10;
		Projectile.height = 10;
		Projectile.friendly = false;
		Projectile.aiStyle = -1;
		Projectile.penetrate = -1;
		Projectile.scale = 1f;
		Projectile.timeLeft = 600;
		Projectile.alpha = 255;
		Projectile.tileCollide = false;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.ContinuouslyUpdateDamageStats = true;
		
		OldPosition = new List<Vector2>();
		OldRotation = new List<float>();
	}
	
	public void OnChangeSelectedItem(Player owner)
	{
		SelectedItem = owner.selectedItem;
		Projectile.ai[0] = 0f;
		Projectile.ai[1] = 0f;
		Projectile.ai[2] = 0f;
		Projectile.localAI[1] = 0;
		Projectile.netUpdate = true;
		owner.Guardian().GuardianItemCharge = 0;

		if (FencingBladeItem.ModItem is OrchidModGuardianFencingBlade guardianItem)
		{
			BladeTexture = ModContent.Request<Texture2D>(guardianItem.BladeTexture, AssetRequestMode.ImmediateLoad).Value;
			if (guardianItem.DrawSheath && ModContent.RequestIfExists(guardianItem.SheathTexture, out Asset<Texture2D> sheathTexture, AssetRequestMode.ImmediateLoad))
				SheathTexture = sheathTexture.Value;
		}
	}

	public override void AI()
	{
		/* AI State Key */
		// ai[0]: Attacking state
		// ai[1]: Parry duration
		// ai[2]: Networked rotation
		
		if (SelectedItem < 0 || FencingBladeItem == null || FencingBladeItem.ModItem is not OrchidModGuardianFencingBlade guardianItem || Owner.HeldItem.ModItem is not OrchidModGuardianFencingBlade || !Owner.active || Owner.dead)
		{ // Kill the projectile if something goes wrong (selected item is invalid, held item is not a Fencing Blade, owner is dead)
			Projectile.Kill();
			return;
		}
		
		bool ChargeInput = OrchidMod.OrchidClientConfig.GuardianSwapGauntletInputs ? Main.mouseRight : Main.mouseLeft; // we should probably add a macro for this
		bool DashInput = OrchidMod.OrchidClientConfig.GuardianSwapGauntletInputs ? Main.mouseLeft : Main.mouseRight; // this too
		
		if (IsLocalOwner)
		{ // Player rotation & Item netupdate
			Owner.heldProj = Projectile.whoAmI; // Set heldProj so the anchor is sandwiched between the player's body and arm (hopefully; I don't know how it works with manual rendering)
			
			if (Main.MouseWorld.X > Owner.Center.X && Owner.direction != 1) Owner.ChangeDir(1);
			else if (Main.MouseWorld.X < Owner.Center.X && Owner.direction != -1) Owner.ChangeDir(-1);
			
			if (NeedNetUpdate) // Obligatory NeedNetUpdate setter
			{
				NeedNetUpdate = false;
				Projectile.netUpdate = true;
			}
		}
		else
		{
			if (Projectile.ai[0] == 0f)
			{ // Addresses a visual issue
				Guardian.GuardianItemCharge = 0;
			}
		}
		
		Vector2 sheathPos = Owner.MountedCenter.Floor(); // Sheaths are technically optional, so set a default value in case
		if (SheathTexture != null) // If the sheath texture is present and enabled, set the shea
		{
			Rectangle sheathFrame = SheathTexture.Frame(guardianItem.FencingBladeFrames, 3, AnimFrame % guardianItem.FencingBladeFrames, UseFrame % 3);
			sheathPos = (Owner.MountedCenter + new Vector2(6f * Owner.direction, 4f) + sheathFrame.Size().Scale(-Owner.direction) * 0.5f + guardianItem.SheathOffset.Scale(Owner.direction) + Vector2.UnitY * Owner.gfxOffY).Floor();
		}
		
		Projectile.timeLeft = 5; // Why do we set the timeLeft to 5 when the default is 600?
		
		if (Guardian.GuardianShowDebugVisuals) Dust.NewDustPerfect(sheathPos, DustID.Torch).noGravity = true;

		if (Projectile.ai[0] > 1) // Charged attack and deflect
		{
			if (Projectile.ai[0] >= 62f) // Deflecting state
			{
				Guardian.GuardianParry = true; // Activate parry
				Guardian.GuardianParryBuffer = true;
				
				Visible = true; // If not already visible (ie. sheath is disabled), make the blade visible 
				
				Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, MathHelper.PiOver2 * Owner.direction);
				Projectile.Center = Owner.MountedCenter + Vector2.UnitX * 10f * Owner.direction; // Blade is held outward and slightly downward
				Projectile.rotation = MathHelper.ToRadians(160f * Owner.direction);

				// Keep ai[0] at 62 while ai[1] is greater than 0
				Projectile.ai[0] = 62f; 
				Projectile.ai[1]--;
				
				if (Owner.immune) // If the player triggers a parry by going invincible (code borrowed from GuardianQuarterstaffAnchor.cs)
				{
					if (Owner.eocDash > 0 && Owner.eocHit != -1) // The player can trigger parries by ramming enemies 
						Guardian.DoParryItemParry(Main.npc[Owner.eocHit]);
					else // Refund the Guard cost (mostlys) if the player becomes immune for any other reason 
					{
						Projectile.ai[0] = 0;
						Projectile.ai[1] = 0;
						Guardian.GuardianGuardRecharging += Projectile.ai[1] / (guardianItem.ParryDuration * guardianItem.Item.GetGlobalItem<GuardianPrefixItem>().GetBlockDuration() * Guardian.GuardianParryDuration);
						Rectangle rect = Owner.Hitbox;
						rect.Y -= 64;
						CombatText.NewText(Guardian.Player.Hitbox, Color.LightGray, Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Interrupted"), false, true);
					}
				}
				else if (Projectile.ai[1] <= 0) // If the parry times out naturally (deflect failed), trigger the standard slash attack 
				{
					Projectile.ai[1] = 0;
					Projectile.ai[0] = -41f;
				}
			}
			else if (Projectile.ai[0].Between(21f, 61f)) // Reinforced attack state
			{
				if (Projectile.ai[0] == 61f)
				{
					Visible = true; //
					Projectile.ai[2] = Vector2.Normalize(Main.MouseWorld - Owner.MountedCenter).ToRotation() - MathHelper.PiOver2;
					Projectile.extraUpdates = 1;
					DamageReset = 0;
					SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, Projectile.Center);

					CombatText.NewText(Owner.getRect(), Color.White, MathHelper.ToDegrees(Projectile.ai[2]).ToString());
				}

				float rotation = Projectile.ai[2] - Owner.direction * MathHelper.Pi / 5f * MathF.Sin((guardianItem.SwingsPerAttack + 1) * 0.1571f * (Projectile.ai[0] - 21));
				Projectile.rotation = rotation + MathHelper.Pi;
				Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation - MathHelper.PiOver2 * Owner.direction);
				Projectile.Center = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, rotation - MathHelper.PiOver2 * Owner.direction) + Vector2.UnitY.RotatedBy(Projectile.rotation) * BladeTexture.Height * 0.25f;
				
				float swingInterval = 40f / (guardianItem.SwingsPerAttack + 1);
				if ((int)((61 - Projectile.ai[0]) / swingInterval) > DamageReset)
				{
					CreateSlashProj(Projectile.ai[2].ToRotationVector2(), true);
					SoundEngine.PlaySound(SoundID.Item71 with {MaxInstances = 10, PitchVariance = 0.4f}, Projectile.Center);
					guardianItem.OnAttack(Owner, Guardian, Projectile, true, DamageReset == 0);
					DamageReset++;
				}
				
				Projectile.ai[0] -= 20f / FencingBladeItem.useTime * guardianItem.SwingSpeed * Owner.GetTotalAttackSpeed(DamageClass.Melee);

				if (Guardian.UseSlam(1, true, true) && DashInput) // Perform dash attack on right click while reinforced attacking
				{
					Projectile.ai[0] = 20f;
					Projectile.ai[2] = Vector2.Normalize(Main.MouseWorld - Owner.MountedCenter).ToRotation() - MathHelper.PiOver2;
					Guardian.UseSlam();

					if (IsLocalOwner)
					{
						ModPlayer.ForcedVelocityTimer = 20;
						ModPlayer.ForcedVelocityVector = (Main.MouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX) * 20f;
					}

					DamageReset = 1;
					
					SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, Projectile.Center);
					NeedNetUpdate = true;
				}
				
				if (Projectile.ai[1] is > -3.14f and < 0f)
					Owner.ChangeDir(1);
				else
					Owner.ChangeDir(-1);
				
				if (Projectile.ai[0] <= 21f)
				{
					Projectile.extraUpdates = 0;
					Projectile.ai[0] = ChargeInput ? 1f : 0;
					Guardian.GuardianItemCharge = ChargeInput ? 1f : 0;
					Ding = null;
					Visible = false;
				}
			}
			else if (Projectile.ai[0] <= 20f)
			{
				if (Projectile.ai[0] == 20f)
				{
					if (!IsLocalOwner)
					{
						ModPlayer.ForcedVelocityTimer = 20;
						ModPlayer.ForcedVelocityVector = (Main.MouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX) * 20f;
					}

					Visible = true;
					Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.ai[2]);
					Projectile.Center = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.ai[2]);
					Projectile.rotation = Projectile.ai[2];
				}
				
				if ((int)((20 - Projectile.ai[0]) / 4f) > DamageReset)
				{
					Projectile.ResetLocalNPCHitImmunity();
					DamageReset++;
				}

				Projectile.ai[0]++;
				if (Projectile.ai[0] <= 1f)
				{
					Projectile.ai[0] = ChargeInput ? 1f : 0;
					Guardian.GuardianItemCharge = ChargeInput ? 1f : 0;
					Projectile.friendly = false;
					Ding = null;
					Visible = false;
					DamageReset = 0;
				}
			}
		}
		else if (Projectile.ai[0] < -1f)
		{
			if (Projectile.ai[0].Between(-41f, -1f))
			{
				if (Projectile.ai[0] == -41f)
				{
					Projectile.friendly = true;
					Visible = true;

					Projectile.ai[2] = Vector2.Normalize(Main.MouseWorld - Owner.MountedCenter).ToRotation() - MathHelper.PiOver2;
					SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, Projectile.Center);
					SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);

					CombatText.NewText(Owner.getRect(), Color.White, MathHelper.ToDegrees(Projectile.ai[2]).ToString());
					
					CreateSlashProj(Projectile.ai[2].ToRotationVector2());
					
				}


				Projectile.ai[0] += 20f / FencingBladeItem.useTime * guardianItem.SwingSpeed * Owner.GetTotalAttackSpeed(DamageClass.Melee);

				
				float rotation = Projectile.ai[2] - Owner.direction * 2.5f * MathF.Sin(0.1571f * (Projectile.ai[0] + 1));
				Projectile.rotation = rotation + MathHelper.Pi;
				Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation - MathHelper.PiOver2 * Owner.direction);
				Projectile.Center = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, rotation - MathHelper.PiOver2 * Owner.direction) + Vector2.UnitY.RotatedBy(Projectile.rotation) * BladeTexture.Height * 0.25f;

				
				
				if (Projectile.ai[0] >= -1f)
				{
					Projectile.ai[0] = ChargeInput ? 1f : 0;
					Guardian.GuardianItemCharge = ChargeInput ? 1f : 0;
					Projectile.friendly = false;
					Ding = null;
					Visible = false;
				}
			}
			
			if (Projectile.ai[1] is > -3.14f and < 0f)
				Owner.ChangeDir(1);
			else
				Owner.ChangeDir(-1);
		}
		else if (Projectile.ai[0] == 1)
		{
			if (guardianItem.DrawSheath && SheathTexture != null)
			{
				float rotation = Owner.MountedCenter.DirectionTo(sheathPos).ToRotation() - MathHelper.PiOver2 * Owner.direction;
				if (Owner.direction == -1) rotation += MathHelper.Pi;
				Projectile.Center = Owner.MountedCenter;
				Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, rotation);
				UseFrame = 1;
			}
			else
			{
				Visible = true;
				
				Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, MathHelper.Pi - Guardian.GuardianItemCharge * 0.006f * Owner.direction); // set arm position (90 degree offset since arm starts lowered)
				Vector2 armPosition = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.ThreeQuarters, MathHelper.Pi - Guardian.GuardianItemCharge * 0.006f * Owner.direction) - (Owner.Center - Owner.Center.Floor());
				Projectile.Center = armPosition;
				Projectile.rotation = Projectile.Center.DirectionTo((Owner.MountedCenter + Vector2.UnitY * (4f + Owner.gfxOffY)).Floor()).ToRotation();
			}
			
			
			if (Owner.eyeHelper.CurrentEyeFrame == PlayerEyeHelper.EyeFrame.EyeOpen)
			{
				Owner.eyeHelper.CurrentEyeFrame = PlayerEyeHelper.EyeFrame.EyeHalfClosed;
				Owner.eyeHelper.Update(Owner);
			}

			Guardian.GuardianItemCharge += 40f / FencingBladeItem.useTime * (Owner.GetTotalAttackSpeed(DamageClass.Melee) * 2f - 1f) * guardianItem.ChargeSpeedMultiplier;
			if (Guardian.GuardianItemCharge > 180f) // Fully-charged attack
			{
				if (Ding is false && IsLocalOwner) // Ding is a bool?, so pattern matching is needed to check ternary flag
				{
					if (ModContent.GetInstance<OrchidClientConfig>().GuardianAltChargeSounds) SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Owner.Center);
					else SoundEngine.PlaySound(SoundID.MaxMana, Owner.Center); // we should probably add a macro for this too
					Ding = true;
				}
				Guardian.GuardianItemCharge = 180f; // Keep charge at 180 when full
			}
			else if (Guardian.GuardianItemCharge >= 60 && Ding is null) // Semi-charged attack
			{
				Ding = false;
				CombatText.NewText(Owner.Hitbox, new Color(175, 255, 175), Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Charged"), false);
				SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact, Owner.Center);
			}

			if (!ChargeInput)
			{
				if (Guardian.GuardianItemCharge >= 180f)
				{
					Projectile.ai[0] = 61f;
				}
				else if (Guardian.GuardianItemCharge.Between(60f, 180f))
				{
					// when ai[0] is 63, the fencing blade is parrying/deflecting
					Projectile.ai[0] = 63f;
					SoundEngine.PlaySound(SoundID.Item52, Projectile.Center);
					Projectile.ai[1] = guardianItem.ParryDuration;
					Owner.immuneTime = 0;
					Owner.immune = false;
					Guardian.modPlayer.PlayerImmunity = 0;
					Guardian.GuardianParry = true;
					Guardian.GuardianParryBuffer = true;
				}
				else
				{
					Projectile.ai[0] = 0f;
				}

				if (Guardian.GuardianItemCharge > 60f)
					UseFrame = 2;
				
				Guardian.GuardianItemCharge = 0;
			}
		}
		else
		{
			if (Owner.eyeHelper.CurrentEyeFrame == PlayerEyeHelper.EyeFrame.EyeHalfClosed)
			{
				Owner.eyeHelper.CurrentEyeFrame = PlayerEyeHelper.EyeFrame.EyeOpen;
				Owner.eyeHelper.Update(Owner);
			}
			
			Projectile.Center = Owner.Center;
			UseFrame = 0;
			Ding = null;
			Visible = false;
		}
	}

	public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
	{
		if (BladeTexture == null) return false;
		if (SelectedItem < 0 || SelectedItem > 58) return false;
		if (FencingBladeItem.ModItem is not OrchidModGuardianFencingBlade guardianItem) return false;
		
		Color color = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f), Color.White);
		if (guardianItem.PreDrawFencingBlade(spriteBatch, Projectile, Owner, ref color))
		{
			SpriteEffects effects = (Owner.direction == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			if (guardianItem.DrawSheath && SheathTexture != null)
			{
				Rectangle sheathFrame = SheathTexture.Frame(guardianItem.FencingBladeFrames, 3, AnimFrame % guardianItem.FencingBladeFrames, UseFrame % 3);
				
				Vector2 drawPos = (Owner.MountedCenter + new Vector2(6f * Owner.direction, 4f * Owner.gravDir) + SheathTexture.Size().Scale(-Owner.direction) * 0.5f + guardianItem.SheathOffset.Scale(Owner.direction, Owner.gravDir) + Vector2.UnitY * Owner.gfxOffY).Floor();
			
				Color sheathLighting = Lighting.GetColor((int)(drawPos.X / 16f), (int)(drawPos.Y / 16f), Color.White);
				
				spriteBatch.Draw(SheathTexture, drawPos - Main.screenPosition, sheathFrame, sheathLighting, 0f, SheathTexture.Size() * 0.5f, Projectile.scale, effects, 0f);

				Texture2D sheathGlow = guardianItem.GetGlowmaskTexture(Owner, Projectile, true, out Rectangle? drawRectGlow, UseFrame, AnimFrame);
				if (sheathGlow != null)
				{
					Color glowColor = guardianItem.GetFencingBladeGlowmaskColor(Owner, Guardian, Projectile, true, lightColor); 
					spriteBatch.Draw(sheathGlow, drawPos - Main.screenPosition, drawRectGlow, glowColor, 0f, sheathGlow.Size() * 0.5f, Projectile.scale, effects, 0f);
				}
					
			}

			if (Visible)
			{
				Rectangle bladeFrame = BladeTexture.Frame(1, guardianItem.FencingBladeFrames, 0, AnimFrame % guardianItem.FencingBladeFrames);
				
				Vector2 drawPos = (Projectile.Center + Vector2.UnitY * Owner.gfxOffY).Floor();
				
				spriteBatch.Draw(BladeTexture, drawPos - Main.screenPosition, bladeFrame, color, Projectile.rotation, BladeTexture.Size() * 0.5f, Projectile.scale, effects, 0f);

				Texture2D bladeGlow = guardianItem.GetGlowmaskTexture(Owner, Projectile, false, out Rectangle? drawRectGlow, UseFrame, AnimFrame);
				if (bladeGlow != null)
				{
					Color glowColor = guardianItem.GetFencingBladeGlowmaskColor(Owner, Guardian, Projectile, true, lightColor); 
					spriteBatch.Draw(bladeGlow, drawPos - Main.screenPosition, drawRectGlow, glowColor, Projectile.rotation, bladeGlow.Size() * 0.5f, Projectile.scale, effects, 0f);
				}
			}
		}
		guardianItem.PostDrawFencingBlade(spriteBatch, Projectile, Owner, lightColor);
		
		return false;
	}
	
	public Projectile CreateSlashProj(Vector2 velocity, bool reinforced = false)
	{
		Projectile newProj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, velocity * FencingBladeItem.shootSpeed * (!reinforced ? 0.25f : 1f), ModContent.ProjectileType<FencingBladeSlashProjectile>(), 1, 1f, Owner.whoAmI, Main.rand.NextFloat(-0.05f, 0.05f));
		if (newProj.ModProjectile is FencingBladeSlashProjectile slashProj) // Code modified from GuardianKatarAnchor.cs 
		{
			slashProj.FencingBladeItem = FencingBladeItem.ModItem as OrchidModGuardianFencingBlade;
			slashProj.Strong = reinforced;
			newProj.damage = Guardian.GetGuardianDamage(FencingBladeItem.damage * (reinforced ? 0.4f : 1f));
			newProj.CritChance = (int)(Owner.GetCritChance<GuardianDamageClass>() + Owner.GetCritChance<GenericDamageClass>() + FencingBladeItem.crit);
			newProj.knockBack = FencingBladeItem.knockBack;

			newProj.netUpdate = true;
		}
		else
		{
			newProj.Kill();
			return null;
		}

		return newProj;
	}
}