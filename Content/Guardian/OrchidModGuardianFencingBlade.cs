using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common;
using OrchidMod.Common.Global.Items;
using OrchidMod.Content.General.Prefixes;
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
	/// <summary>The multiplier for the basic swing attack performed after a failed deflect.</summary>
	public float SwingSpeed = 1f;
	/// <summary>The multiplier for the reinforced swing attack performed after a successful deflect or at full charge.</summary>
	public float ReinforcedSwingSpeed = 1f;
	public int ParryDuration = 20;
	/// <summary>The amount of slash projectiles created during the reinforced attack.</summary>
	/// <remarks>Also affects the attack animation: higher values make the blade swing faster. Use alongside <see cref="ReinforcedSwingSpeed"/> to balance the rate of fire with the attack duration.</remarks>
	public int SwingsPerAttack = 6;
	public float SwingDamage = 1f;
	public float ReinforcedSwingDamage = 0.4f;
	/// <summary>The percentage of the blade texture's height to offset its position when held.</summary>
	public float HoldOffset = 0.25f;
	/// <summary>The offset for the sheath texture when drawn, if <see cref="DrawSheath"/> is enabled.</summary>
	public Vector2 SheathOffset = Vector2.Zero;
	public float DashVelocity = 20f;
	public float SwingVelocity = 0.5f;
	public float ReinforcedSwingVelocity = 1.5f;
	/// <summary>Controls the random variation (in radians) that the basic swing attack's slash projectile bends.</summary>
	/// <remarks>This value is passed into the slash projectile as <c>ai[0]</c>, as the lower and upper bounds of <c>NextFloat</c> when not reinforced.</remarks>
	public float SwingBend = 0f;
	/// <summary>Controls the random variation (in radians) that the reinforced attack's slash projectiles bend.</summary>
	/// <remarks>This value is passed into the slash projectile as <c>ai[0]</c>, as the lower and upper bounds of <c>NextFloat</c> when reinforced.</remarks>
	public float ReinforcedSwingBend = 0.025f;
	
	public int SwingStyle = 0;
	public int ReinforcedSwingStyle = 1;
	
	/// <summary>The amount of animation frames this weapon has.</summary>
	public int FencingBladeFrames = 1;
	/// <summary>Whether the sheath should be drawn. If false, the sword will always be drawn.</summary>
	public bool DrawSheath = true;
	
	public virtual void OnHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit) { } // Called when hitting a target during an attack
	public virtual void OnHitFirst(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit) { } // Called when hitting the first target for the first time during an attack
	public virtual void FencingBladeModifyHitNPC(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, ref NPC.HitModifiers modifiers, bool firstHit) { } // anchor's modifyhitNPC
	public virtual bool OnSwing(Player player, OrchidGuardian guardian, Projectile projectile, bool reinforced, ref int damage) => true; // Called on the first frame of an attack
	public virtual void OnStartAttack(Player player, OrchidGuardian guardian, Projectile projectile, bool charged, bool first) { } // Called on the first frame of an attack
	public virtual void OnParryFencingBlade(Player player, OrchidGuardian guardian, Entity aggressor, Projectile anchor) { } // Called on parrying anything
	public virtual bool PreDash(Player player, OrchidGuardian guardian, Projectile anchor) => guardian.UseSlam();
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
		anchor.ai[0] = 61f;
		anchor.ai[2] = Vector2.Normalize(Main.MouseWorld - player.MountedCenter).ToRotation() - MathHelper.PiOver2;
		guardian.OnAttack(AttackID.FencingBladeCounter, this);
		((GuardianFencingBladeAnchor)anchor.ModProjectile).NeedNetUpdate = true;
		OnParryFencingBlade(player, guardian, aggressor, anchor);
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
					bool shouldBlock = Main.mouseRight && Main.mouseRightRelease;
					bool shouldCharge = Main.mouseLeft;
						
					if (ModContent.GetInstance<OrchidClientConfig>().GuardianSwapGauntletInputs)
					{
						shouldBlock = Main.mouseLeft && Main.mouseLeftRelease;
						shouldCharge = Main.mouseRight;
					}

					if (shouldCharge && guardian.GuardianItemCharge == 0f && proj.ai[0] == 0f)
					{
						proj.ai[0] = 1f;
						proj.ai[2] = 0f;
						anchor.NeedNetUpdate = true;
						guardian.GuardianItemCharge++;
						SoundEngine.PlaySound(SoundID.Item7, player.Center);
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


		string click = ModContent.GetInstance<OrchidClientConfig>().GuardianSwapGauntletInputs ? Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.LeftClick") : Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.RightClick");
		tooltips.Insert(index + 2, new TooltipLine(Mod, "ClickInfo", Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Parry", click))
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
}