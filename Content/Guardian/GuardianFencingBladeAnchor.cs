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
	/// <remarks>Unlike most other <c>Ding</c> flags, Fencing Blades' <c>Ding</c> uses a nullable boolean as a "ternary boolean": either no ding (null), semi-ding (false), or ding (true).</remarks>
	public bool? Ding;
	/// <summary>The total amount of frames an attack has lasted for so far.</summary>
	public int AttackTimer;
	/// <summary>The total amount of frames that have passed since the last swing. Reset to 0 at the start of a swing.</summary>
	public int SwingTimer;
	
	public float FencingBladeDashAngle = 0f;
	public int FencingBladeDashTimer = 0;
	
	public new Player Owner;
	public int SelectedItem { get; set; } = -1;
	public Item FencingBladeItem => Owner.inventory[SelectedItem];
	public OrchidModGuardianFencingBlade GuardianItem;
	
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
		
		OldPosition = [];
		OldRotation = [];
	}
	
	public void OnChangeSelectedItem(Player Owner)
	{
		SelectedItem = Owner.selectedItem;
		Projectile.ai[0] = 0f;
		Projectile.ai[1] = 0f;
		Projectile.ai[2] = 0f;
		Projectile.localAI[1] = 0;
		Projectile.netUpdate = true;
		Owner.Guardian().GuardianItemCharge = 0;

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
		
		Owner ??= base.Owner;
		
		if (SelectedItem < 0 || FencingBladeItem == null || FencingBladeItem.ModItem is not OrchidModGuardianFencingBlade guardianItem || Owner.HeldItem.ModItem is not OrchidModGuardianFencingBlade || !Owner.active || Owner.dead)
		{ // Kill the projectile if something goes wrong (selected item is invalid, held item is not a Fencing Blade, Owner is dead)
			Projectile.Kill();
			return;
		}
		else
		{
			GuardianItem = guardianItem;
			
			if (IsLocalOwner)
			{ // Player rotation & Item netupdate
				Owner.heldProj = Projectile.whoAmI; // Set heldProj so the anchor is sandwiched between the player's body and arm (hopefully; I don't know how it works with manual rendering)
				
				if (Main.MouseWorld.X > Owner.Center.X && Owner.direction != 1) Owner.ChangeDir(1);
				else if (Main.MouseWorld.X < Owner.Center.X && Owner.direction != -1) Owner.ChangeDir(-1);
				
				Projectile.velocity = (Main.MouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX);
				
				if (NeedNetUpdate) // Obligatory NeedNetUpdate setter
				{
					NeedNetUpdate = false;
					Projectile.netUpdate = true;
				}
			}
			else
			{
				Projectile.velocity = Vector2.UnitY.RotatedBy(Projectile.ai[2]);
				
				if (Projectile.ai[0] == 0f)
				{ // Addresses a visual issue
					Guardian.GuardianItemCharge = 0;
				}
			}
			Projectile.velocity *= float.Epsilon;
			
			Vector2 sheathPos = Owner.MountedCenter.Floor(); // Sheaths are technically optional, so set a default value in case
			if (SheathTexture != null) // If the sheath texture is present and enabled, set the shea
			{
				Rectangle sheathFrame = SheathTexture.Frame(GuardianItem.FencingBladeFrames, 3, AnimFrame % guardianItem.FencingBladeFrames, UseFrame % 3);
				sheathPos = (Owner.MountedCenter + new Vector2(6f * Owner.direction, 4f) + sheathFrame.Size().Scale(-Owner.direction) * 0.5f + GuardianItem.SheathOffset.Scale(Owner.direction) + Vector2.UnitY * Owner.gfxOffY).Floor();
			}
			
			Projectile.timeLeft = 5; // Why do we set the timeLeft to 5 when the default is 600?
			
			if (Guardian.GuardianDebugVisuals == 1) Dust.NewDustPerfect(sheathPos, DustID.Torch).noGravity = true;

			if (Projectile.ai[0] < -1f) // Normal attack
			{
				if (Projectile.ai[0] == -41f)
				{
					Projectile.friendly = true;
					Visible = true;

					// Projectile.ai[2] = Vector2.Normalize(Main.MouseWorld - Owner.MountedCenter).ToRotation() - MathHelper.PiOver2;
					// SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, Projectile.Center);
					SoundEngine.PlaySound(GuardianItem.SwingSound, Projectile.Center);
						
					// CombatText.NewText(Owner.getRect(), Color.White, MathHelper.ToDegrees(Projectile.ai[2]).ToString());
					
					int damage = Guardian.GetGuardianDamage(FencingBladeItem.damage * GuardianItem.SwingDamage);
					if (GuardianItem.OnSlash(Owner, Guardian, Projectile, false, ref damage))
						CreateSlashProj(Vector2.UnitY.RotatedBy(Projectile.ai[2]) * FencingBladeItem.shootSpeed * GuardianItem.SwingVelocity, damage);
				}
				
				if (Projectile.ai[2] is > -3.14f and < 0f)
					Owner.ChangeDir(1);
				else
					Owner.ChangeDir(-1);
					
				DoSlashStyle(GuardianItem.SwingStyle, -(Projectile.ai[0] + 1), GuardianItem.SwingSound);
				
				OldPosition.Add(Projectile.Center);
				OldRotation.Add(Projectile.rotation);
				
				if (OldPosition.Count > 10)
				{
					OldPosition.RemoveAt(0);
					OldRotation.RemoveAt(0);
				}
				
				Projectile.ai[0] += 20f / FencingBladeItem.useTime * GuardianItem.SwingSpeed * Owner.GetTotalAttackSpeed(DamageClass.Melee);
				
				AttackTimer++;
				SwingTimer++;
				
				if (Projectile.ai[0] >= -1f)
				{
					Projectile.ai[0] = 1f;
					Guardian.GuardianItemCharge = 0;
					Projectile.netUpdate = true;
					AttackTimer = 0;
					SwingTimer = 0;
					Ding = null;
					Visible = false;
				}
			}
			else if (Projectile.ai[0] > 1) // Charged attack and deflect
			{
				if (Projectile.ai[0] >= 62f) // Deflecting state
				{
					Guardian.GuardianParry = true; // Activate parry
					Guardian.GuardianParryBuffer = true;
					
					Visible = true; // If not already visible (ie. sheath is disabled), make the blade visible 
					
					// Keep ai[0] at 62 while ai[1] is greater than 0
					Projectile.ai[0] = 62f; 
					Projectile.ai[1]--;
					
					if (Projectile.ai[2] is > -3.14f and < 0f)
						Owner.ChangeDir(1);
					else
						Owner.ChangeDir(-1);
						
					Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, MathHelper.PiOver2 * -Owner.direction);
					Projectile.Center = Owner.MountedCenter + Vector2.UnitX * 10f * Owner.direction; // Blade is held outward and slightly downward
					Projectile.rotation = MathHelper.ToRadians(160f * Owner.direction);

					if (OldPosition.Count > 0)
					{
						OldPosition.RemoveAt(0);
						OldRotation.RemoveAt(0);
					}
					
					if (Owner.immune) // If the player triggers a parry by going invincible (code borrowed from GuardianQuarterstaffAnchor.cs)
					{
						if (Owner.eocDash > 0 && Owner.eocHit != -1) // The player can trigger parries by ramming enemies 
							Guardian.DoParryItemParry(Main.npc[Owner.eocHit]);
						else // Refund the Guard cost if the player becomes immune for any other reason 
						{
							// Projectile.ai[0] = 0;
							Projectile.ai[1] = 0;
							Guardian.GuardianGuardRecharging += Projectile.ai[1] / Guardian.GetParryDuration(FencingBladeItem, GuardianItem.ParryDuration);
							Rectangle rect = Owner.Hitbox;
							rect.Y -= 64;
							CombatText.NewText(Guardian.Player.Hitbox, Color.LightGray, Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Interrupted"), false, true);
						}
					}
					else if (Projectile.ai[1] <= 0) // If the parry times out naturally (deflect failed), trigger the standard slash attack 
					{
						Projectile.ai[1] = 0;
						Projectile.ai[0] = -41f;
						Projectile.netUpdate = true;
					}
				}
				else if (Projectile.ai[0].Between(21f, 61f)) // Reinforced attack state
				{
					if (Projectile.ai[0] == 61f)
					{
						Visible = true; //
						Projectile.extraUpdates = 1;
						DamageReset = 0;
						SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, Projectile.Center);

						// CombatText.NewText(Owner.getRect(), Color.White, MathHelper.ToDegrees(Projectile.ai[2]).ToString());
					}
					
					if (Projectile.ai[2] is > -3.14f and < 0f)
						Owner.ChangeDir(1);
					else
						Owner.ChangeDir(-1);
						
					DoSlashStyle(GuardianItem.ReinforcedSwingStyle, Projectile.ai[0] - 21f, GuardianItem.SwingSound, true);
					
					OldPosition.Add(Projectile.Center);
					OldRotation.Add(Projectile.rotation);
					
					if (OldPosition.Count > 10)
					{
						OldPosition.RemoveAt(0);
						OldRotation.RemoveAt(0);
					}
					
					Projectile.ai[0] -= 20f / FencingBladeItem.useTime * GuardianItem.ReinforcedSwingSpeed * Owner.GetTotalAttackSpeed(DamageClass.Melee);

					if (IsLocalOwner && Main.mouseRight && GuardianItem.PreDash(Owner, Guardian, Projectile)) // Perform dash attack on right click while reinforced attacking
					{
						Projectile.ai[0] = 20f;
						Projectile.ai[2] = Vector2.Normalize(Main.MouseWorld - Owner.MountedCenter).ToRotation() - MathHelper.PiOver2;

						DamageReset = 0;
						
						// SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, Projectile.Center);
						NeedNetUpdate = true;
					}
					
					
					if (Projectile.ai[0] <= 21f)
					{
						if (Guardian.GuardianItemCharge > 0) Projectile.ai[0] = 1f;
						else Projectile.ai[0] = 0f;
						Projectile.extraUpdates = 0;
						Projectile.netUpdate = true;
						AttackTimer = 0;
						SwingTimer = 0;
						Ding = null;
						Visible = false;
					}
				}
				else if (Projectile.ai[0] <= 20f)
				{
					float rotation = Projectile.ai[2];
					Projectile.rotation = rotation + MathHelper.Pi;
					Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
					Projectile.Center = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, rotation) + Vector2.UnitY.RotatedBy(rotation) * BladeTexture.Height * GuardianItem.HoldOffset;
					
					Owner.position = Collision.TileCollision(Owner.position, Vector2.UnitY.RotatedBy(rotation) * GuardianItem.DashSpeed, Owner.width, Owner.height, true, true, (int)Owner.gravDir);
					Owner.fullRotation = rotation;
					Owner.fullRotationOrigin = Owner.MountedCenter;
					
					if ((int)((20 - Projectile.ai[0]) / 4f) > DamageReset)
					{
						foreach (NPC npc in Main.ActiveNPCs)
						{
							if (IsValidTarget(npc) && OrchidUtils.CheckCircularvCircularCollision(Projectile.CircularizeHitbox(), npc.CircularizeHitbox()))
							{
								NPC.HitInfo hitInfo = npc.CalculateHitInfo(Guardian.GetGuardianDamage(FencingBladeItem.damage), (Projectile.velocity.X > 0).ToDirectionInt(), false, 2f, ModContent.GetInstance<GuardianDamageClass>());
								npc.StrikeNPC(hitInfo);
								GuardianItem.OnHit(Owner, Guardian, npc, Projectile, hitInfo);
							}
								
						}
						DamageReset++;
					}

					Projectile.ai[0]--;
					if (Projectile.ai[0] <= 1f)
					{
						Projectile.ai[0] = 0;
						Guardian.GuardianItemCharge = 0;
						Projectile.netUpdate = true;
						Projectile.friendly = false;
						Ding = null;
						Visible = false;
						DamageReset = 0;
					}
				}
			}
			else if (Projectile.ai[0] == 1)
			{
				if (Guardian.GuardianItemCharge < 180f)
				{
					Guardian.GuardianItemCharge += 30f / FencingBladeItem.useTime * Owner.GetTotalAttackSpeed(DamageClass.Melee) * GuardianItem.ChargeRate;
					if (Guardian.GuardianItemCharge > 180f) Guardian.GuardianItemCharge = 180f;
				}
				
				float rotation = Owner.MountedCenter.DirectionTo(sheathPos).ToRotation() - MathHelper.PiOver2 * Owner.direction;
				
				if (GuardianItem.DrawSheath && SheathTexture != null)
				{
					if (Owner.direction == -1) rotation += MathHelper.Pi;
					
					Projectile.Center = Owner.MountedCenter;
					Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, rotation);
					UseFrame = 1;
				}
				else
				{
					Visible = true;
					
					rotation = Guardian.GuardianItemCharge * 0.006f * -Owner.direction;
					Projectile.rotation = rotation + MathHelper.Pi - MathHelper.PiOver2 * Owner.direction;
					Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, rotation);
					Projectile.Center = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, rotation) - (Vector2.UnitY.RotatedBy(Projectile.rotation) * BladeTexture.Height * GuardianItem.HoldOffset).Floor();
				}
				
				if (OldPosition.Count > 0)
				{
					OldPosition.RemoveAt(0);
					OldRotation.RemoveAt(0);
				}
				
				if (IsLocalOwner)
				{
					if (Guardian.GuardianItemCharge >= 180f) // Fully-charged attack
					{
						if (Ding is not true)
						{
							Ding = true;
							// we should probably add a macro for this too
							if (OrchidMod.OrchidClientConfig.GuardianAltChargeSounds) SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Owner.Center);
							else SoundEngine.PlaySound(SoundID.MaxMana, Owner.Center); 
						}
					}
					else if (Guardian.GuardianItemCharge >= 60) // Semi-charged attack
					{
						if (Ding is not false)
						{
							Ding = false;
						
							CombatText.NewText(Owner.Hitbox, new Color(175, 255, 175), Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Charged"), false);
							SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact, Owner.Center);
						}
					}
					
					bool chargeInput = OrchidMod.OrchidClientConfig.GuardianSwapGauntletInputs ? Main.mouseRight : Main.mouseLeft; // we should probably add a macro for this
					
					if (!chargeInput)
					{
						if (Guardian.GuardianItemCharge >= 180f)
							Projectile.ai[0] = 61f;
						else if (Guardian.GuardianItemCharge.Between(60f, 180f))
						{
							// when ai[0] is 63, the fencing blade is parrying/deflecting
							Projectile.ai[0] = 63f;
							SoundEngine.PlaySound(SoundID.Item52, Projectile.Center);
							Projectile.ai[1] = Guardian.GetParryDuration(FencingBladeItem, GuardianItem.ParryDuration);
							
							OldPosition.Clear();
							OldRotation.Clear();
							
							Owner.immuneTime = 0;
							Owner.immune = false;
							ModPlayer.PlayerImmunity = 0;
							Guardian.GuardianParry = true;
							Guardian.GuardianParryBuffer = true;
						}
						else
						{
							Projectile.ai[0] = 0;
							Projectile.ai[2] = 0;
						}
						
						Projectile.ai[2] = Vector2.Normalize(Main.MouseWorld - Owner.MountedCenter).ToRotation() - MathHelper.PiOver2;

						if (Guardian.GuardianItemCharge > 60f)
							UseFrame = 2;
						
						SwingTimer = 0;
						DamageReset = 0;
						Guardian.GuardianItemCharge = 0;
						Projectile.netUpdate = true;
					}
				}
			}
			else
			{
				Ding = null;
				Visible = false;
				Owner.eyeHelper.SwitchToState(PlayerEyeHelper.EyeState.InStorm, true);
				
				Projectile.Center = Owner.Center;
				AttackTimer = 0;
				SwingTimer = 0;
				UseFrame = 0;
			}
		}
		
		GuardianItem.ExtraAIFencingBlade(Owner, Guardian, Projectile);
	}

	public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
	{
		if (BladeTexture == null) return false;
		if (SelectedItem < 0 || SelectedItem > 58) return false;
		if (FencingBladeItem.ModItem is not OrchidModGuardianFencingBlade GuardianItem) return false;
		
		Color color = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f), Color.White);
		if (GuardianItem.PreDrawFencingBlade(spriteBatch, Projectile, Owner, ref color))
		{
			SpriteEffects effects = (Owner.direction == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			if (GuardianItem.DrawSheath && SheathTexture != null)
			{
				Rectangle sheathFrame = SheathTexture.Frame(GuardianItem.FencingBladeFrames, 3, AnimFrame % GuardianItem.FencingBladeFrames, UseFrame % 3);
				
				Vector2 drawPos = (Owner.MountedCenter + new Vector2(6f * Owner.direction, 4f * Owner.gravDir) + SheathTexture.Size().Scale(-Owner.direction) * 0.5f + GuardianItem.SheathOffset.Scale(Owner.direction, Owner.gravDir) + Vector2.UnitY * Owner.gfxOffY).Floor();
			
				Color sheathLighting = Lighting.GetColor((int)(drawPos.X / 16f), (int)(drawPos.Y / 16f), Color.White);
				
				spriteBatch.Draw(SheathTexture, drawPos - Main.screenPosition, sheathFrame, sheathLighting, 0f, SheathTexture.Size() * 0.5f, Projectile.scale, effects, 0f);

				Texture2D sheathGlow = GuardianItem.GetGlowmaskTexture(Owner, Projectile, true, out Rectangle? drawRectGlow, UseFrame, AnimFrame);
				if (sheathGlow != null)
				{
					Color glowColor = GuardianItem.GetFencingBladeGlowmaskColor(Owner, Guardian, Projectile, true, lightColor); 
					spriteBatch.Draw(sheathGlow, drawPos - Main.screenPosition, drawRectGlow, glowColor, 0f, sheathGlow.Size() * 0.5f, Projectile.scale, effects, 0f);
				}
					
			}

			if (Visible)
			{
				Rectangle bladeFrame = BladeTexture.Frame(1, GuardianItem.FencingBladeFrames, 0, AnimFrame % GuardianItem.FencingBladeFrames);
				
				Vector2 drawPos = (Projectile.Center + Vector2.UnitY * Owner.gfxOffY).Floor();
				
				if (Projectile.ai[0].Between(21f, 61f) || Projectile.ai[0].Between(-41f, -1f))
				{ // attacking = draw trail
					spriteBatch.End(out SpriteBatchSnapshot spriteBatchSnapshot);
					spriteBatch.Begin(spriteBatchSnapshot with { BlendState = BlendState.Additive });

					for (int i = 0; i < OldPosition.Count; i++)
					{
						Vector2 drawPosTrail = (OldPosition[i] + Vector2.UnitY * Owner.gfxOffY).Floor();
						spriteBatch.Draw(BladeTexture, drawPosTrail - Main.screenPosition , bladeFrame, lightColor * 0.05f * (i + 1), OldRotation[i], BladeTexture.Size() * 0.5f, Projectile.scale, effects, 0f);
					}

					spriteBatch.End();
					spriteBatch.Begin(spriteBatchSnapshot);
				}
				
				spriteBatch.Draw(BladeTexture, drawPos - Main.screenPosition, bladeFrame, color, Projectile.rotation, BladeTexture.Size() * 0.5f, Projectile.scale, effects, 0f);

				Texture2D bladeGlow = GuardianItem.GetGlowmaskTexture(Owner, Projectile, false, out Rectangle? drawRectGlow, UseFrame, AnimFrame);
				if (bladeGlow != null)
				{
					Color glowColor = GuardianItem.GetFencingBladeGlowmaskColor(Owner, Guardian, Projectile, false, lightColor); 
					spriteBatch.Draw(bladeGlow, drawPos - Main.screenPosition, drawRectGlow, glowColor, Projectile.rotation, bladeGlow.Size() * 0.5f, Projectile.scale, effects, 0f);
				}
			}
		}
		GuardianItem.PostDrawFencingBlade(spriteBatch, Projectile, Owner, lightColor);
		
		return false;
	}
	
	/// <summary>
	/// Executes the given attack style. <c>ai</c> should begin at 40 and decrement to 0 for the animation to play in correct order.<br/>
	/// For style IDs 0 and 2, <c>useFocusProj</c> will create an invisible projectile that spawns slashes onto it, instead of the default singular wave.
	/// </summary>
	/// <remarks>Attack style IDs<br/>
	/// 0: Single swing (default basic)<br/>
	/// 1: Rapid multi-swing (default reinforced)<br/>
	/// 2: Single thrust<br/>
	/// 3: Rapid multi-thrust<br/>
	/// </remarks>
	public void DoSlashStyle(int style, float ai, SoundStyle sound, bool reinforced = false, bool useFocusProj = false)
	{
		Vector2 sheathPos = Owner.MountedCenter.Floor(); // Sheaths are technically optional, so set a default value in case
		if (SheathTexture != null) // If the sheath texture is present and enabled, set the shea
		{
			Rectangle sheathFrame = SheathTexture.Frame(GuardianItem.FencingBladeFrames, 3, AnimFrame % GuardianItem.FencingBladeFrames, UseFrame % 3);
			sheathPos = (Owner.MountedCenter + new Vector2(6f * Owner.direction, 4f) + sheathFrame.Size().Scale(-Owner.direction) * 0.5f + GuardianItem.SheathOffset.Scale(Owner.direction) + Vector2.UnitY * Owner.gfxOffY).Floor();
		}
		float rotation = 0;
		
		float damageMult = GuardianItem.SwingDamage;
		float velocityMult = GuardianItem.SwingVelocity;
		
		if (reinforced)
		{
			damageMult = GuardianItem.ReinforcedSwingDamage;
			velocityMult = GuardianItem.ReinforcedSwingVelocity;
		}
		
		AttackTimer++;
		SwingTimer++;
		
		switch (style)
		{
			case 0: // Normal attack: big upward slash
				rotation = Projectile.ai[2] - Owner.direction * MathHelper.Pi / 3f * MathF.Sin(0.3142f * ai);
				
				if (ai == 40f)
				{
					int damage = Guardian.GetGuardianDamage(FencingBladeItem.damage * damageMult);
					if (GuardianItem.OnSlash(Owner, Guardian, Projectile, reinforced, ref damage))
					{
						if (useFocusProj) CreateFocusProj(Vector2.UnitY.RotatedBy(Projectile.ai[2]) * FencingBladeItem.shootSpeed * velocityMult, damage, reinforced);
						else CreateSlashProj(Vector2.UnitY.RotatedBy(Projectile.ai[2]) * FencingBladeItem.shootSpeed * velocityMult, damage, reinforced);
					}
							
					SoundEngine.PlaySound(sound, Projectile.Center);
					Projectile.netUpdate = true;
					SwingTimer = 0;
				}
				break;
			case 1: // Reinforced attack:
				float sheathAngle = Owner.MountedCenter.DirectionTo(sheathPos).ToRotation();
				if (Projectile.direction == -1) 
					sheathAngle += MathHelper.TwoPi;
				int swings = (reinforced ? GuardianItem.ReinforcedSwingsPerAttack : GuardianItem.SwingsPerAttack) + 1;
				float interval = 40f / swings;
				if (ai >= 40 - interval / 2f)
				{
					rotation = MathHelper.SmoothStep(Projectile.ai[2], sheathAngle, (ai - (40 - interval / 2f)) / (interval / 2f));
				} 
				else if (ai <= interval)
				{
					rotation = MathHelper.SmoothStep(sheathAngle, Projectile.ai[2], ai / interval);
				}
				else
				{
					rotation = Projectile.ai[2] + Owner.direction * MathHelper.Pi / 5f * MathF.Sin(swings * 0.1571f * ai);
				}
				
				if ((int)((40 - ai) / interval) > DamageReset)
				{
					int damage = Guardian.GetGuardianDamage(FencingBladeItem.damage * damageMult);
					if (GuardianItem.OnSlash(Owner, Guardian, Projectile, reinforced, ref damage))
						CreateSlashProj(Vector2.UnitY.RotatedBy(Projectile.ai[2]) * FencingBladeItem.shootSpeed * velocityMult, damage, reinforced);
							
					SoundEngine.PlaySound(sound, Projectile.Center);
					Projectile.netUpdate = true;
					DamageReset++;
					SwingTimer = 0;
				}
				break;
		}
		
		Projectile.rotation = rotation + MathHelper.Pi;
		Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
		Projectile.Center = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, rotation) + Vector2.UnitY.RotatedBy(rotation) * BladeTexture.Height * GuardianItem.HoldOffset;
	}
	
	public void CreateSlashProj(Vector2 velocity, int damage, bool reinforced = false)
	{
		Guardian.OnAttack(reinforced ? AttackID.FencingBladeReinforcedSlash : AttackID.FencingBladeSlash, GuardianItem);
		
		Projectile newProj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, velocity, ModContent.ProjectileType<FencingBladeSlashProjectile>(), 1, 1f, Owner.whoAmI);
		if (newProj.ModProjectile is FencingBladeSlashProjectile slashProj) // Code modified from GuardianKatarAnchor.cs 
		{
			slashProj.FencingBladeItem = GuardianItem;
			slashProj.Strong = reinforced;
			newProj.ai[0] = reinforced ? Main.rand.NextFloat(-GuardianItem.ReinforcedSwingBend, GuardianItem.ReinforcedSwingBend) : Main.rand.NextFloat(-GuardianItem.SwingBend, GuardianItem.SwingBend);
			newProj.rotation = newProj.velocity.ToRotation();
			newProj.damage = damage;
			newProj.CritChance = (int)(Owner.GetCritChance<GuardianDamageClass>() + Owner.GetCritChance<GenericDamageClass>() + FencingBladeItem.crit);
			newProj.knockBack = FencingBladeItem.knockBack;

			newProj.netUpdate = true;
		}
		else
			newProj.Kill();
	}
	
	public void CreateFocusProj(Vector2 velocity, int damage, bool reinforced = false)
	{
		Guardian.OnAttack(reinforced ? AttackID.FencingBladeReinforcedSlash : AttackID.FencingBladeSlash, GuardianItem);
		
		Projectile newProj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, velocity, ModContent.ProjectileType<FencingBladeFocusProjectile>(), 1, 1f, Owner.whoAmI);
		if (newProj.ModProjectile is FencingBladeSlashProjectile slashProj) // Code modified from GuardianKatarAnchor.cs 
		{
			slashProj.FencingBladeItem = GuardianItem;
			slashProj.Strong = reinforced;
			newProj.ai[0] = reinforced ? GuardianItem.ReinforcedSwingsPerAttack : GuardianItem.SwingsPerAttack;
			newProj.rotation = newProj.velocity.ToRotation();
			newProj.damage = damage;
			newProj.CritChance = (int)(Owner.GetCritChance<GuardianDamageClass>() + Owner.GetCritChance<GenericDamageClass>() + FencingBladeItem.crit);
			newProj.knockBack = FencingBladeItem.knockBack;

			newProj.netUpdate = true;
		}
		else
			newProj.Kill();
	}
	
	/// <summary>Returns a rough estimate of the amount of time an attack will take in ticks, based on attack speed multipliers.</summary>
	public int AttackDuration(bool reinforced) => (int)(2 * FencingBladeItem.useTime / ((reinforced ? GuardianItem.ReinforcedSwingSpeed : GuardianItem.SwingSpeed) * Owner.GetTotalAttackSpeed(DamageClass.Melee)));
	/// <summary>Returns a rough estimate of the amount of time between swing attacks in (fractional) ticks, based on attack speed multipliers.</summary>
	/// <remarks>Note that since the amount of swings per attack does not always divide evenly into the total attack duration, the duration may come out as a decimal.
	/// If using this method in <see cref="OrchidModGuardianFencingBlade.ExtraAIFencingBlade">ExtraAIFencingBlade</see> or any other Fencing Blade hooks to perform special effects on a timer, consider rounding down or up as needed.</remarks>
	public float AttackInterval(bool reinforced) => AttackDuration(reinforced) / (float)((reinforced ? GuardianItem.SwingsPerAttack : GuardianItem.SwingsPerAttack) + 1);
}