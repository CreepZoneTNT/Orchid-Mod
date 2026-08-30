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
using Terraria.DataStructures;
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
	public List<int> HitNPCs;
	
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
		HitNPCs = [];
	}
	
	public void OnChangeSelectedItem(Player Owner)
	{
		SelectedItem = Owner.selectedItem;
		Projectile.ai[0] = 0f;
		Projectile.ai[1] = 0f;
		Projectile.ai[2] = 0f;
		Projectile.localAI[1] = 0;
		Projectile.netUpdate = true;
		FencingBladeDashTimer = 0;
		FencingBladeDashAngle = 0;
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


			if (FencingBladeDashTimer > 0)
			{
				if (FencingBladeDashTimer > 1)
				{
					if (Main.rand.NextBool())
					{
						Dust dust = Dust.NewDustDirect(Owner.position, Owner.width, Owner.height, DustID.Smoke);
						dust.noGravity = true;
					}
					
					if (IsLocalOwner)
					{
						foreach (NPC npc in Main.npc)
						{
							if (IsValidTarget(npc) && !HitNPCs.Contains(npc.whoAmI) && npc.Hitbox.Intersects(Owner.Hitbox))
							{
								HitNPCs.Add(npc.whoAmI);
								guardianItem.OnDashHit(Owner, Guardian, npc, Projectile);
								int damage = Guardian.GetGuardianDamage(guardianItem.Item.damage * guardianItem.DashDamage);
								Owner.ApplyDamageToNPC(npc, damage, guardianItem.DashKnockback, Owner.direction, Main.rand.Next(100) < Projectile.CritChance, ModContent.GetInstance<GuardianDamageClass>());
								Owner.AddImmuneTime(-1, 5);
							}
						}
					}
				}
				else
				{
					for (int i = 0; i < 5; i++)
					{
						Dust dust = Dust.NewDustDirect(Owner.Center, 0, 0, DustID.Smoke);
						dust.scale *= Main.rand.NextFloat(1f, 1.5f);
						dust.velocity *= Main.rand.NextFloat(0.5f, 0.75f);
					}

					for (int i = 0; i < 3; i++)
					{
						Gore gore = Gore.NewGoreDirect(Owner.GetSource_FromAI(), Owner.Center + new Vector2(Main.rand.NextFloat(-24f, 0f), Main.rand.NextFloat(-24f, 0f)), Vector2.UnitY.RotatedByRandom(MathHelper.Pi), 61 + Main.rand.Next(3));
						gore.rotation = Main.rand.NextFloat(MathHelper.Pi);
						gore.scale *= Main.rand.NextFloat(0.4f, 0.66f);
						gore.velocity *= Main.rand.NextFloat(0.5f, 0.75f);
					}
				}
				
				float rotation = Projectile.ai[2];
				Projectile.rotation = rotation + MathHelper.Pi;
				Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
				Projectile.Center = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, rotation) + Vector2.UnitY.RotatedBy(rotation) * BladeTexture.Height * GuardianItem.HoldOffset;
				
				FencingBladeDashTimer--;
			}
			else if (Projectile.ai[0] < -1f) // Normal attack
			{
				FencingBladeAttackProfile profile = GuardianItem.ChargedProfile(Projectile);
				if (Projectile.ai[0] == -41f)
				{
					Projectile.friendly = true;
					Projectile.extraUpdates = 1;
					Visible = true;

					SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, Projectile.Center);
						
					// CombatText.NewText(Owner.getRect(), Color.White, MathHelper.ToDegrees(Projectile.ai[2]).ToString());
				}
				
				if (Projectile.ai[2] is > -3.14f and < 0f)
					Owner.ChangeDir(1);
				else
					Owner.ChangeDir(-1);
					
				DoSlashStyle(profile.AttackStyle, -(Projectile.ai[0] + 1), GuardianItem.SwingSound, profile.UsesFocusProjectile);
				
				Projectile.ai[0] += 20f / FencingBladeItem.useTime * profile.AnimationSpeed * Owner.GetTotalAttackSpeed(DamageClass.Melee);
				
				AttackTimer++;
				SwingTimer++;
				
				if (Projectile.ai[0] >= -1f)
				{
					Projectile.ai[0] = 1f;
					Projectile.extraUpdates = 0;
					Guardian.GuardianItemCharge = 0;
					AttackTimer = 0;
					SwingTimer = 0;
					Ding = null;
					Visible = false;
					Projectile.netUpdate = true;
				}
			}
			else if (Projectile.ai[0] > 1) // Charged attack and deflect
			{
				if (Projectile.ai[0] > 41f) // Deflecting state
				{
					Guardian.GuardianParry = true; // Activate parry
					Guardian.GuardianParryBuffer = true;
					
					Visible = true; // If not already visible (ie. sheath is disabled), make the blade visible 
					
					// Keep ai[0] at 42 while ai[1] is greater than 0
					Projectile.ai[0] = 42f; 
					Projectile.ai[1]--;
					
					if (Projectile.ai[2] is > -3.14f and < 0f)
						Owner.ChangeDir(1);
					else
						Owner.ChangeDir(-1);
						
					Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, MathHelper.PiOver2 * -Owner.direction);
					Projectile.Center = Owner.MountedCenter + Vector2.UnitX * 10f * Owner.direction; // Blade is held outward and slightly downward
					Projectile.rotation = MathHelper.ToRadians(160f * Owner.direction);

					UpdateCache(0);
					
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

						foreach (Projectile enemyProj in Main.ActiveProjectiles)
						{
							if (!enemyProj.friendly && enemyProj.damage > 0 && enemyProj.Hitbox.Intersects(Projectile.Hitbox))
							{
								enemyProj.Kill();
							}
						}
						foreach (NPC enemyNPC in Main.ActiveNPCs)
						{
							if (!enemyNPC.friendly && enemyNPC.damage > 0 && enemyNPC.Hitbox.Intersects(Projectile.Hitbox))
							{
								enemyNPC.velocity += Vector2.UnitY.RotatedBy(Projectile.ai[2]) * 20f * enemyNPC.knockBackResist;
								enemyNPC.netUpdate = true;
							}
						}
						Projectile.netUpdate = true;
					}
				}
				else if (Projectile.ai[0] is > 2f and <= 41f) // Reinforced attack state
				{
					FencingBladeAttackProfile profile = GuardianItem.ReinforcedProfile(Projectile);
					if (Projectile.ai[0] == 41f)
					{
						Visible = true; //
						Projectile.extraUpdates = 1;
						DamageReset = 0;
						
						CombatText.NewText(Owner.Hitbox, new Color(175, 255, 175), Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Reinforced"), false);
						
						SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, Projectile.Center);

						// CombatText.NewText(Owner.getRect(), Color.White, MathHelper.ToDegrees(Projectile.ai[2]).ToString());
					}
					
					if (Projectile.ai[2] is > -3.14f and < 0f)
						Owner.ChangeDir(1);
					else
						Owner.ChangeDir(-1);
						
					DoSlashStyle(profile.AttackStyle, Projectile.ai[0] - 1f, GuardianItem.SwingSound, true, profile.UsesFocusProjectile);
					
					Projectile.ai[0] -= 20f / FencingBladeItem.useTime * profile.AnimationSpeed * Owner.GetTotalAttackSpeed(DamageClass.Melee);

					if (IsLocalOwner && Main.mouseRight && Main.mouseRightRelease && GuardianItem.PreDash(Owner, Guardian, Projectile)) // Perform dash attack on right click while reinforced attacking
					{
						Projectile.ai[0] = 1.5f;
						FencingBladeDashAngle = Vector2.Normalize(Main.MouseWorld - Owner.MountedCenter).ToRotation() - MathHelper.PiOver2;
						FencingBladeDashTimer = GuardianItem.DashDuration;
						HitNPCs.Clear();
						DamageReset = 0;
						Visible = true;
						
						Projectile.netUpdate = true;
						// NeedNetUpdate = true;
					}
					
					
					if (Projectile.ai[0] <= 2f)
					{
						if (Guardian.GuardianItemCharge > 0) Projectile.ai[0] = 1f;
						else Projectile.ai[0] = 0f;
						Projectile.extraUpdates = 0;
						AttackTimer = 0;
						SwingTimer = 0;
						Ding = null;
						Visible = false;
						Projectile.netUpdate = true;
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
				
				float rotation = Owner.MountedCenter.DirectionTo(sheathPos).ToRotation() - 3f * MathHelper.PiOver4 * Owner.direction;
				
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
						Guardian.GuardCostUI = 1;
					}
					
					bool swap = OrchidMod.OrchidClientConfig.GuardianSwapGauntletInputs;
					bool chargeInput = swap ? Main.mouseRight : Main.mouseLeft; // we should probably add a macro for this
					bool altInput = swap ? Main.mouseLeft : Main.mouseRight; // we should probably add a macro for this
					
					if (Guardian.GuardianItemCharge >= 180f && altInput && Guardian.UseSlam(1, true))
					{
						Guardian.UseSlam();
						Projectile.ai[0] = 41f;
						Projectile.ai[2] = MathHelper.WrapAngle(Vector2.Normalize(Main.MouseWorld - Owner.MountedCenter).ToRotation() - MathHelper.PiOver2);
						Guardian.OnAttack(AttackID.FencingBladeReinforcedSlash, GuardianItem);
						Guardian.GuardianItemCharge = 61f;
						Ding = false;
						Owner.TryInterruptingItemUsage();
						NeedNetUpdate = true;
					}
					else if (!chargeInput)
					{
						if (Guardian.GuardianItemCharge >= 60f)
						{// when ai[0] is 43, the fencing blade is parrying/deflecting
							if (Guardian.GuardianItemCharge >= 180f || Guardian.UseGuard(1, true))
							{
								if (Guardian.GuardianItemCharge < 180f)
									Guardian.UseGuard();
									
								Projectile.ai[0] = 43f;
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
								Ding = null;
								Projectile.ai[0] = -41f;
							}
						}
						else
						{
							Projectile.ai[0] = 0;
							Projectile.ai[2] = 0;
						}
						
						Projectile.ai[2] = MathHelper.WrapAngle(Vector2.Normalize(Main.MouseWorld - Owner.MountedCenter).ToRotation() - MathHelper.PiOver2);

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
				FirstHit = false;
				Owner.eyeHelper.SwitchToState(PlayerEyeHelper.EyeState.InStorm, true);
				
				Projectile.Center = Owner.Center;
				AttackTimer = 0;
				SwingTimer = 0;
				UseFrame = 0;
			}
		}
		
		
		GuardianItem.ExtraAIFencingBlade(Owner, Guardian, Projectile);
	}

	public override bool? CanCutTiles() => Projectile.ai[0] is > 1 and < 42 or < 0;

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
				
				if (Projectile.ai[0] is > 2f and < 41f or > -41f and < -1f)
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
	
	public void UpdateCache(int limit = 10, bool recordNew = true, bool purgeOld = true)
	{
		if (recordNew)
		{
			OldPosition.Add(Projectile.Center);
			OldRotation.Add(Projectile.rotation);
		}
				
		if (purgeOld && OldPosition.Count > limit)
		{
			OldPosition.RemoveAt(0);
			OldRotation.RemoveAt(0);
		}
	}
	
	public void ClearCache()
	{
		OldPosition.Clear();
		OldRotation.Clear();
	}
	
	/// <summary>
	/// Executes the given attack style. <c>ai</c> should begin at 40 and decrement to 0 for the animation to play in correct order.<br/>
	/// For style IDs 0 and 2, <c>useFocusProj</c> will create an invisible projectile that spawns slashes onto it, instead of the default singular wave.
	/// </summary>
	/// <seealso cref="OrchidModGua">dd</seealso>
	/// <remarks>Attack style IDs<br/>
	/// 0: Single swing (default basic)<br/>
	/// 1: Rapid multi-swing (default reinforced)<br/>
	/// 2: Single thrust<br/>
	/// 3: Rapid multi-thrust<br/>
	/// </remarks>
	public void DoSlashStyle(int style, float ai, SoundStyle sound, bool reinforced = false, bool useFocusProj = false)
	{
		Vector2 sheathPos = Owner.MountedCenter.Floor(); // Sheaths are technically optional, so set a default value in case
		float toSheath = MathHelper.PiOver4 * Owner.direction;
		if (SheathTexture != null) // If the sheath texture is present and enabled, set the shea
		{
			Rectangle sheathFrame = SheathTexture.Frame(GuardianItem.FencingBladeFrames, 3, AnimFrame % GuardianItem.FencingBladeFrames, UseFrame % 3);
			sheathPos = (Owner.MountedCenter + new Vector2(6f * Owner.direction, 4f) + sheathFrame.Size().Scale(-Owner.direction) * 0.5f + GuardianItem.SheathOffset.Scale(Owner.direction) + Vector2.UnitY * Owner.gfxOffY).Floor();
			toSheath = Owner.MountedCenter.DirectionTo(sheathPos).ToRotation() - 3f * MathHelper.PiOver4;
		}
		float rotation = 0;
		
		FencingBladeAttackProfile settings = reinforced ? GuardianItem.ReinforcedProfile(Projectile) : GuardianItem.ChargedProfile(Projectile);
		
		AttackTimer++;
		SwingTimer++;
		
		int damage = Guardian.GetGuardianDamage(FencingBladeItem.damage * settings.Damage * (reinforced && Ding is not true ? GuardianItem.SemiReinforcedDamage : Ding is null ? 0.8f : 1f));
		
		switch (style)
		{
			case 0: // Normal attack: big upward slash
				if (ai >= 35f)
				{
					rotation = 1.5f * settings.ControlAngle * MathF.Sin(MathHelper.Pi/10 * ai) + settings.ControlAngle / 2f;
					UpdateCache();
					if (rotation >= 0 && DamageReset == 0)
					{
						if (GuardianItem.OnSlash(Owner, Guardian, Projectile, reinforced, ref damage))
						{
							Vector2 velocity = Vector2.UnitY.RotatedBy(Projectile.ai[2]) * FencingBladeItem.shootSpeed * settings.Velocity;
							if (useFocusProj) CreateFocusProj(velocity * 0.5f, damage, reinforced);
							else CreateSlashProj(velocity, damage, reinforced);
						}
							
						SoundEngine.PlaySound(sound, Projectile.Center);
						DamageReset++;
						Projectile.netUpdate = true;
						SwingTimer = 0;
					}
				}
				else if (ai >= 20f)
				{
					rotation = 0.5f * settings.ControlAngle * (MathF.Sin(MathHelper.Pi/15 * (ai - 12.5f)) - 1);
					UpdateCache();
				}
				else
				{
					rotation = ((toSheath - Projectile.ai[2]) + (Owner.direction == -1 ? MathHelper.Pi : 0)) * MathF.Sin(MathHelper.Pi/40 * (ai + 20f));
					UpdateCache(0, false);
				}
				
				rotation = Projectile.ai[2] + Owner.direction * rotation;
				
				Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
				Projectile.Center = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, rotation) + Vector2.UnitY.RotatedBy(Projectile.rotation - MathHelper.Pi) * BladeTexture.Height * GuardianItem.HoldOffset;
				
				Projectile.rotation = rotation + MathHelper.Pi;
				if (ai < 20) Projectile.rotation = (toSheath - Projectile.ai[2]);
				
				break;
			case 1: // Reinforced attack:
				float sheathAngle = Owner.MountedCenter.DirectionTo(sheathPos).ToRotation();
				if (Projectile.direction == -1) 
					sheathAngle += MathHelper.Pi;
				int swings = settings.Quantity + 1;
				float interval = 40f / swings;
				if (ai >= 40 - interval / 2f)
				{
					rotation = MathHelper.SmoothStep(Projectile.ai[2], sheathAngle, (ai - (40 - interval / 2f)) / (interval / 2));
					UpdateCache();
				}
				else if (ai <= interval)
				{
					rotation = MathHelper.SmoothStep(sheathAngle, Projectile.ai[2], ai / interval);					
					// rotation = ((toSheath - Projectile.ai[2]) + (Owner.direction == -1 ? MathHelper.Pi : 0)) * MathF.Sin(ai * MathHelper.Pi/interval);
					UpdateCache();
				}
				else
				{
					rotation = Projectile.ai[2] + Owner.direction * settings.ControlAngle * MathF.Sin(swings * ai * MathHelper.Pi/20);
					UpdateCache(0);
				}
					
		
				Projectile.rotation = rotation + MathHelper.Pi;
				Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
				Projectile.Center = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, rotation) + Vector2.UnitY.RotatedBy(rotation) * BladeTexture.Height * GuardianItem.HoldOffset;
				
				if ((int)((40 - ai) / interval) > DamageReset)
				{
					if (GuardianItem.OnSlash(Owner, Guardian, Projectile, reinforced, ref damage))
						CreateSlashProj(Vector2.UnitY.RotatedBy(Projectile.ai[2]) * FencingBladeItem.shootSpeed * settings.Velocity, damage, reinforced);
							
					SoundEngine.PlaySound(sound, Projectile.Center);
					Projectile.netUpdate = true;
					DamageReset++;
					SwingTimer = 0;
				}
				break;
			case 2:
				if (ai < 20f)
				{
					rotation = ((toSheath - Projectile.ai[2]) + (Owner.direction == -1 ? MathHelper.Pi : 0)) * MathF.Sin(MathHelper.Pi/40 * (ai + 20f));
					UpdateCache(0, false);
				}
				else
				{
					rotation = 0;
					if (ai <= 30 && DamageReset == 0)
					{
						if (GuardianItem.OnSlash(Owner, Guardian, Projectile, reinforced, ref damage))
						{
							Vector2 velocity = Vector2.UnitY.RotatedBy(Projectile.ai[2]) * FencingBladeItem.shootSpeed * settings.Velocity;
							if (useFocusProj) CreateFocusProj(velocity * 0.5f, damage, reinforced);
							else CreateSlashProj(velocity, damage, reinforced);
						}
							
						SoundEngine.PlaySound(sound, Projectile.Center);
						DamageReset++;
						Projectile.netUpdate = true;
						SwingTimer = 0;
					}
				}

				rotation = Projectile.ai[2] + Owner.direction * rotation;
				
				Projectile.rotation = rotation + MathHelper.Pi;
				Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
				float distance = BladeTexture.Height * GuardianItem.HoldOffset;
					distance *= ai >= 20f ? -(0.5f + 1.5f * MathF.Sin(MathHelper.Pi * ai / 20)) : 0.5f;
				Projectile.Center = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, rotation) + Vector2.UnitY.RotatedBy(rotation) * distance;
				break;
			
		}
	}
	
	public void CreateSlashProj(Vector2 velocity, int damage, bool reinforced = false)
	{
		Guardian.OnAttack(reinforced ? AttackID.FencingBladeReinforcedSlash : AttackID.FencingBladeSlash, GuardianItem);
		
		Projectile newProj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, velocity, ModContent.ProjectileType<FencingBladeSlashProjectile>(), 1, 1f, Owner.whoAmI);
		FencingBladeAttackProfile settings = reinforced ? GuardianItem.ReinforcedProfile(Projectile) : GuardianItem.ChargedProfile(Projectile);
		
		if (newProj.ModProjectile is FencingBladeSlashProjectile slashProj) // Code modified from GuardianKatarAnchor.cs 
		{
			slashProj.FencingBladeItem = GuardianItem;
			slashProj.Strong = Ding is true;
			slashProj.Scale = settings.Scale;
			slashProj.ScaleMult = settings.ScaleChange;
			slashProj.Stab = settings.Stab;
			slashProj.DamageDecay = settings.DamageDecay;
			newProj.ai[0] = Main.rand.NextFloat(-settings.BendAmount, settings.BendAmount);
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
		FencingBladeAttackProfile settings = reinforced ? GuardianItem.ReinforcedProfile(Projectile) : GuardianItem.ChargedProfile(Projectile);

		if (newProj.ModProjectile is FencingBladeFocusProjectile focusProj) // Code modified from GuardianKatarAnchor.cs 
		{
			focusProj.FencingBladeItem = GuardianItem;
			focusProj.Strong = Ding is true;
			focusProj.FencingBladeProfile = settings;
			newProj.ai[0] = settings.Quantity;
			newProj.ai[1] = settings.BendAmount;
			newProj.ai[2] = settings.Scale;
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
	public int AttackDuration(bool reinforced)
	{
		FencingBladeAttackProfile profile = reinforced ? GuardianItem.ReinforcedProfile(Projectile) : GuardianItem.ChargedProfile(Projectile);
		return (int)(2 * FencingBladeItem.useTime / profile.AnimationSpeed * Owner.GetTotalAttackSpeed(DamageClass.Melee));
	} 
	/// <summary>Returns a rough estimate of the amount of time between swing attacks in (fractional) ticks, based on attack speed multipliers.</summary>
	/// <remarks>Note that since the amount of swings per attack does not always divide evenly into the total attack duration, the duration may come out as a decimal.
	/// If using this method in <see cref="OrchidModGuardianFencingBlade.ExtraAIFencingBlade">ExtraAIFencingBlade</see> or any other Fencing Blade hooks to perform special effects on a timer, consider rounding down or up as needed.</remarks>
	public float AttackInterval(bool reinforced)
	{
		FencingBladeAttackProfile profile = reinforced ? GuardianItem.ReinforcedProfile(Projectile) : GuardianItem.ChargedProfile(Projectile);
		return AttackDuration(reinforced) / (float)(profile.Quantity + 1);
	}
}