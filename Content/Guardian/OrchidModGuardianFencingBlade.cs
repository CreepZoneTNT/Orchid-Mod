using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common;
using OrchidMod.Common.Global.Items;
using OrchidMod.Content.General.Prefixes;
using OrchidMod.Content.Guardian.Projectiles.Gauntlets;
using OrchidMod.Utilities;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian;

public abstract class OrchidModGuardianFencingBlade : OrchidModGuardianParryItem
{
	public virtual string BladeTexture => Texture + "_Blade";
	public virtual string BladeTextureGlow => Texture + "_Blade_Glow";
	/// <summary>The texture for the sheath. Must have 3 frames stacked vertically: one for when idle, one for when charging, and for one when attacking.</summary>
	/// <remarks>Although the sheath texture needs 3 vertical textures, you could add extra animation frames horizontally. Remember to override the <c>drawRectangle</c> property of <see cref="GetBladeTexture"/> to get the </remarks>
	public virtual string SheathTexture => Texture + "_Sheath";
	public virtual string SheathTextureGlow => Texture + "_Sheath_Glow";
	
	public SoundStyle SwingSound = SoundID.Item71 with {MaxInstances = 10, PitchVariance = 0.4f};
	/// <summary>Multiplies charge speed while holding left click.</summary>
	public float ChargeRate = 1f;
	public int ParryDuration = 20;
	/// <summary>Multiplier for the damage dealt by a Reinforced attack that consumed a Guard. Defaults to 0.8 (80%).</summary>
	public float SemiReinforcedDamage = 0.8f;
	public float DashDamage = 1f;
	public float DashKnockback = 1f;
	/// <summary>The percentage of the blade texture's height to offset its position when held.</summary>
	public float HoldOffset = 0.25f;
	/// <summary>The offset for the sheath texture when drawn, if <see cref="DrawSheath"/> is enabled.</summary>
	public Vector2 SheathOffset = Vector2.Zero;
	/// <summary> Duration (in frames) of the reinforced dash. Defaults to 10. </summary>
	public int DashDuration = 10;
	/// <summary> Velocity of the reinforced dash. Defaults to 20f. </summary>
	public float DashSpeed = 20f;
	/// <summary> Multiplies the velocity of the player after the reinforced dash (retains more momentum with higher values). Defaults to 0.33f (for early game weapons). </summary>
	public float DashMomentum = 0.33f;
	public virtual FencingBladeAttackProfile ChargedProfile(Projectile anchor) => FencingBladeAttackID.SingleSwing;
	public virtual FencingBladeAttackProfile ReinforcedProfile(Projectile anchor) => FencingBladeAttackID.MultiSwing;
	
	/// <summary>The amount of animation frames this weapon has.</summary>
	public int FencingBladeFrames = 1;
	/// <summary>Whether the sheath should be drawn. If false, the sword will always be drawn.</summary>
	public bool DrawSheath = true;
	
	public virtual void OnHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit) { } // Called when hitting a target during an attack
	public virtual void OnHitFirst(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit) { } // Called when hitting the first target for the first time during an attack
	public virtual void OnDashHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile) { } // Called when hitting a target during a dash
	public virtual void FencingBladeModifyHitNPC(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, ref NPC.HitModifiers modifiers, bool firstHit) { } // anchor's modifyhitNPC
	public virtual bool OnSlash(Player player, OrchidGuardian guardian, Projectile projectile, bool reinforced, ref int damage) => true; // Called on the first frame of an attack
	public virtual void OnParryFencingBlade(Player player, OrchidGuardian guardian, Entity aggressor, Projectile anchor) { } // Called on parrying anything
	public virtual bool PreDash(Player player, OrchidGuardian guardian, Projectile anchor) => guardian.UseSlam();
	public virtual bool PreDeflect(Player player, OrchidGuardian guardian, Entity aggressor, Projectile anchor) => guardian.UseGuard();
	public virtual void ExtraAIFencingBlade(Player player, OrchidGuardian guardian, Projectile anchor) {}
	public virtual void PostDrawFencingBlade(SpriteBatch spriteBatch, Projectile projectile, Player player, Color lightColor) { }
	public virtual bool PreDrawFencingBlade(SpriteBatch spriteBatch, Projectile projectile, Player player, ref Color lightColor) => true;
	
	public virtual Color GetColor(Player player, OrchidGuardian guardian, Projectile anchor) => Color.White;
	public virtual Color GetFencingBladeGlowmaskColor(Player player, OrchidGuardian guardian, Projectile projectile, bool sheath, Color lightColor) => Color.White;
	public virtual bool ProjectileAI(Player player, Projectile projectile, bool charged) => true;
	
	public virtual void SafeHoldItem(Player player) { }
	public virtual void SafeModifyTooltips(List<TooltipLine> tooltips) { }

	public sealed override void OnParry(Player player, OrchidGuardian guardian, Entity aggressor, Projectile anchor)
	{
		if (anchor.ModProjectile is GuardianFencingBladeAnchor modAnchor)
		{
			anchor.ai[0] = 41f;
			anchor.ai[2] = MathHelper.WrapAngle(Vector2.Normalize(Main.MouseWorld - player.MountedCenter).ToRotation() - MathHelper.PiOver2);
			guardian.OnAttack(AttackID.FencingBladeCounter, this);
			modAnchor.Ding = true;
			modAnchor.NeedNetUpdate = true;
			
		}
		OnParryFencingBlade(player, guardian, aggressor, anchor);
		
		// // Code borrowed from ThoriumGraniteGauntlet.cs
		// if (PreDeflect(player, guardian, aggressor, anchor) && anchor.ModProjectile is GuardianFencingBladeAnchor blade && blade.Ding is true && aggressor is not null)
		// {
		// 	bool deflect = false;
		// 	bool instantExplode = true;
		// 	Vector2 strikeVelocity = Vector2.UnitY.RotatedBy((Main.MouseWorld - player.MountedCenter).ToRotation() - MathHelper.PiOver2) * 8;
		// 	Vector2 strikeEndPosition = anchor.Center + strikeVelocity * 10;
		// 	int slashDamage = guardian.GetGuardianDamage(Item.damage * ReinforcedProfile(anchor).Damage * ReinforcedProfile(anchor).Quantity);
		// 	int highestDeflectedDamage = 0;
		// 	foreach (Projectile deflectProj in Main.ActiveProjectiles)
		// 	{
		// 		if (deflectProj.hostile && deflectProj.damage > 0 && Collision.CheckAABBvLineCollision(deflectProj.position + deflectProj.velocity - new Vector2(16), new Vector2(deflectProj.width + 32, deflectProj.height + 32), anchor.Center, strikeEndPosition))
		// 		{
		// 			if (!deflect)
		// 			{
		// 				deflect = true;
		// 				guardian.OnBlockProjectileFirst(anchor, deflectProj, 0, true);
		// 			}
		// 			else
		// 			{
		// 				instantExplode = false;
		// 				guardian.OnBlockProjectile(anchor, deflectProj, true);
		// 				if (deflectProj.damage > highestDeflectedDamage) highestDeflectedDamage = deflectProj.damage;
		// 				deflectProj.Kill();
		// 			}
		// 		}
		// 	}
		// 	foreach (NPC deflectEnemy in Main.ActiveNPCs)
		// 	{
		// 		if (!deflectEnemy.friendly && Collision.CheckAABBvLineCollision(deflectEnemy.position + deflectEnemy.velocity - new Vector2(16), new Vector2(deflectEnemy.width + 32, deflectEnemy.height + 32), anchor.Center, strikeEndPosition))
		// 		{
		// 			if (!deflect)
		// 			{
		// 				deflect = true;
		// 				guardian.OnBlockNPCFirst(anchor, deflectEnemy, 0, true);
		// 			}
		// 			else
		// 			{
		// 				guardian.OnBlockNPC(anchor, deflectEnemy, true);
		// 				if (deflectEnemy.damage > highestDeflectedDamage) highestDeflectedDamage = deflectEnemy.damage;
		// 				if (!deflectEnemy.dontTakeDamage)
		// 				{
		// 					NPC.HitInfo info = deflectEnemy.CalculateHitInfo(slashDamage, strikeVelocity.X > 1 ? 1 : -1, false, 1f, ModContent.GetInstance<GuardianDamageClass>());
		// 					if (info.Damage >= deflectEnemy.life) instantExplode = false; 
		// 					deflectEnemy.StrikeNPC(info);
		// 				}
		// 			}
		// 		}
		// 	}
		// 	if (deflect)
		// 	{
		// 		Projectile counterProj = Projectile.NewProjectileDirect(Item.GetSource_FromThis(), player.MountedCenter + strikeVelocity * 6f, Vector2.Zero, ModContent.ProjectileType<ThoriumGraniteGauntletProjectile>(), Math.Clamp(highestDeflectedDamage, slashDamage, 1000), Item.knockBack, player.whoAmI);
		// 		counterProj.CritChance = (int)(player.GetCritChance<GuardianDamageClass>() + player.GetCritChance<GenericDamageClass>() + Item.crit);
		// 		counterProj.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		// 		if (!instantExplode)
		// 		{
		// 			counterProj.damage = (int)(counterProj.damage * 1.5f);
		// 			SoundEngine.PlaySound(SoundID.Item37.WithPitchOffset(0.4f), player.Center);
		// 		}
		// 		else
		// 		{
		// 			counterProj.ai[0] = 1;
		// 			counterProj.timeLeft -= 4;
		// 			SoundEngine.PlaySound(SoundID.Item37.WithPitchOffset(0.6f), player.Center);
		// 		}
		// 	}
		// }
		
	}

	public override int AnchorType => ModContent.ProjectileType<GuardianFencingBladeAnchor>();
	
	public sealed override void SetDefaults()
	{
		Item.DamageType = ModContent.GetInstance<GuardianDamageClass>();
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.UseSound = SoundID.Item1;
		Item.autoReuse = false;
		Item.maxStack = 1;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTime = 30;
		Item.knockBack = 10f;
		Item.shootSpeed = 10f;
		
		OrchidGlobalItemPerEntity orchidItem = Item.GetGlobalItem<OrchidGlobalItemPerEntity>();
		orchidItem.guardianWeapon = true;

		SafeSetDefaults();

		Item.useAnimation = Item.useTime;
	}

	public override bool AltFunctionUse(Player player) => true;

	public override bool WeaponPrefix() => true;

	public override bool CanUseItem(Player player)
	{
		if (player.whoAmI == Main.myPlayer && !player.cursed)
		{
			if (player.ownedProjectileCounts[AnchorType] > 0)
			{
				var guardian = player.GetModPlayer<OrchidGuardian>();
				var proj = Main.projectile.FirstOrDefault(i => i.active && i.owner == player.whoAmI && i.type == AnchorType);
				if (proj != null && proj.ModProjectile is GuardianFencingBladeAnchor anchor)
				{
					bool shouldCharge = OrchidMod.OrchidClientConfig.GuardianSwapGauntletInputs ? Main.mouseRight : Main.mouseLeft;

					if (shouldCharge && guardian.GuardianItemCharge == 0f && proj.ai[0] == 0f)
					{
						proj.ai[0] = 1f;
						proj.ai[2] = 0f;
						anchor.NeedNetUpdate = true;
						guardian.GuardianItemCharge++;
						SoundEngine.PlaySound(SoundID.Item64, player.Center);
					}
				}
			}
		}
		return false;
	}

	public sealed override void HoldItem(Player player)
	{
		OrchidGuardian guardian = player.Guardian();
		guardian.GuardianDisplayUI = 300;
		
		if (player.ownedProjectileCounts[AnchorType] != 1)
		{
			foreach (Projectile projectile in Main.projectile)
			{
				if (projectile.active && projectile.owner == player.whoAmI && projectile.type == AnchorType)
				{
					projectile.Kill();
				}
			}

			var index = Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center.X, player.Center.Y, 0f, 0f, AnchorType, 0, 0f, player.whoAmI);

			var proj = Main.projectile[index];
			if (proj.ModProjectile is not GuardianFencingBladeAnchor fencingBlade)
				proj.Kill();

			else
				fencingBlade.OnChangeSelectedItem(player);
		}
		else
		{
			var proj = Main.projectile.FirstOrDefault(i => i.active && i.owner == player.whoAmI && i.type == AnchorType);
			if (proj != null && proj.ModProjectile is GuardianFencingBladeAnchor fencingBlade && fencingBlade.SelectedItem != player.selectedItem)
				fencingBlade.OnChangeSelectedItem(player);
		}
		SafeHoldItem(player);
	}
	
	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		var guardian = Main.LocalPlayer.GetModPlayer<OrchidGuardian>();
		TooltipLine tt = tooltips.FirstOrDefault(x => x.Name == "Damage" && x.Mod == "Terraria");
		if (tt != null)
		{
			string[] splitText = tt.Text.Split(' ');
			string damageValue = splitText.First();
			tt.Text = damageValue + " " + Language.GetTextValue(ModContent.GetInstance<OrchidMod>().GetLocalizationKey("DamageClasses.GuardianDamageClass.DisplayName"));
		}

		int index = tooltips.FindIndex(ttip => ttip.Mod.Equals("Terraria") && ttip.Name.Equals("Knockback"));
		tooltips.Insert(index + 1, new TooltipLine(Mod, "ParryDuration", Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.ParryDuration", OrchidUtils.FramesToSeconds((int)(ParryDuration * Item.GetGlobalItem<GuardianPrefixItem>().GetBlockDuration() * guardian.GuardianParryDuration)))));


		string click = OrchidUtils.GetClickInfoTooltip(OrchidMod.OrchidClientConfig.GuardianSwapGauntletInputs);
		tooltips.Insert(index + 2, new TooltipLine(Mod, "ClickInfo", Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.ChargeToParry"))
		{
			OverrideColor = new Color(175, 255, 175)
		});
		tooltips.Insert(index + 3, new TooltipLine(Mod, "Dash", Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.DashDuringSwing", click))
		{
			OverrideColor = new Color(175, 255, 175)
		});

		SafeModifyTooltips(tooltips);
	}
	
	public virtual Texture2D GetGlowmaskTexture(Player player, Projectile anchor, bool sheath, out Rectangle? drawRectangle, int frame, int frameAnim = 0)
	{
		drawRectangle = null;
		if (ModContent.RequestIfExists(sheath ? SheathTextureGlow : BladeTextureGlow, out Asset<Texture2D> assetglow))
		{
			Texture2D texture = assetglow.Value;
			if (FencingBladeFrames > 1) 
				if (sheath) drawRectangle = texture.Frame(FencingBladeFrames, 3, frameAnim % FencingBladeFrames, frame % 3);
				else drawRectangle = texture.Frame(1, FencingBladeFrames, 0, frameAnim % FencingBladeFrames);
			else drawRectangle = texture.Frame(1, 3, 0, frame % 3);
			return texture;
		}
		return null;
	}
	
	public int GetAnchor(Player player)
	{
		if (player.ownedProjectileCounts[AnchorType] != 1)
			return -1;
		foreach (Projectile proj in Main.ActiveProjectiles)
			if (proj.owner == player.whoAmI && proj.type == AnchorType)
				return proj.whoAmI;
		return -1;
	}
}

public struct FencingBladeAttackProfile()
{
	/// <inheritdoc cref="GuardianFencingBladeAnchor.DoSlashStyle"/>
	public int AttackStyle = 0;

	/// <summary>The amount of slash projectiles created during the attack, or the amount of stab projectiles created by the focus. Unused if <c>SwingStyle</c> is 0 or 2, when <c>UseSwingFocusProj</c> is disabled.</summary>
	/// <remarks>Also affects the attack animation: higher values make the blade swing faster. Use alongside <see cref="SwingSpeed"/> to balance the rate of fire with the attack duration.</remarks>
	/// <seealso cref="GuardianFencingBladeAnchor.DoSlashStyle"/>
	public int Quantity = 1;

	/// <summary>Multiplies the amount of damage inflicted per projectile.</summary>
	public float Damage = 1f;
	/// <summary>Multiplies the amount of damage inflicted per projectile.</summary>
	public float DamageDecay = 0.05f;

	/// <summary>Multiplies the animation speed of the attack.</summary>
	public float AnimationSpeed = 1f;

	/// <summary>Multiplies the velocity of the attack's projectile(s).</summary>
	public float Velocity = 1f;

	/// <summary>something something random angle offset</summary>
	/// <remarks>If <c>UsesFocusProjectile</c> is true, this controls the random offset from the ordinals that the focus spawns each stab projectile at; otherwise this controls the random rate each slash curves in flight.</remarks>
	public float BendAmount = 0f;

	/// <summary>The generic "control angle" for the attack's animation. Function depends on <c>AttackStyle</c>, but generally determines how wide a swing is when performed.</summary>
	public float ControlAngle = MathHelper.PiOver2;
	/// <summary>Multiplies the base scale of the attack's projectile(s).</summary>
	public float Scale = 1f;
	/// <summary>Multiplier for the change of scale of the attack's projectile(s).</summary>
	public float ScaleChange = 1f;
	/// <summary>
	/// If true, the attack will spawn a "<see cref="FencingBladeFocusProjectile">Focus</see>" projectile that conjures diagonal stab projectiles on an interval, instead of individual slashes/stabs.
	/// Only works when <c>AttackStyle</c> is set to 0 or 2, as <see cref="Quantity"/> is used to determine the amount of stabs conjured.<br/>
	/// Focus stabs cannot bend, so be sure to read <see cref="BendAmount"/> and <see cref="FocusRotates"/>. 
	/// </summary>
	/// <remarks>Works similarly to the fully-powered Blade from <i>Cave Story</i>.</remarks>
	/// <seealso cref="Stab"/>
	public bool UsesFocusProjectile = false;

	/// <summary>If true, the slash projectiles will have their <c>Stab</c> property set, changing their appearance from an arc shape to a linear slice. Mostly visual.</summary>
	public bool Stab = false;

	/// <summary>If true while <c>UsesFocusProjectile</c> is also true, the stab projectiles spawned by the focus will respect the focus projectile's direction instead of being locked to the ordinal directions.</summary>
	public bool FocusRotates = false;
}

public static class FencingBladeAttackID
{
	public static readonly FencingBladeAttackProfile SingleSwing = new();

	public static readonly FencingBladeAttackProfile MultiSwing = new()
	{
		AttackStyle = 1,
		Quantity = 6,
		Damage = 0.5f,
		AnimationSpeed = 0.8f,
		BendAmount = 0.01f,
		ControlAngle = MathHelper.Pi / 5f,
		ScaleChange = 1.001f
	};

	public static readonly FencingBladeAttackProfile SingleThrust = new()
	{
		AttackStyle = 2,
		Stab = true,
		DamageDecay = 0.02f,
		AnimationSpeed = 0.8f
	};

	/*public static FencingBladeAttackProfile CustomSwing(float damage = 1f, float speed = 1f, float velocity = 1f, float bend = 0f, float angle = MathHelper.PiOver2, float scale = 1f, float scaleMult = 1f) => SingleSwing with
	{
		Damage = damage,
		AnimationSpeed = speed,
		Velocity = velocity,
		BendAmount = bend,
		ControlAngle = angle,
		Scale = scale,
		ScaleChange = scaleMult
	};*/
}