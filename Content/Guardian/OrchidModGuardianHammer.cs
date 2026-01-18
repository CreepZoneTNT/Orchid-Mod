using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common.Global.Items;
using OrchidMod.Content.General.Prefixes;
using OrchidMod.Utilities;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace OrchidMod.Content.Guardian
{
	public abstract class OrchidModGuardianHammer : OrchidModGuardianItem
	{
		/// <summary>
		/// Time in ticks before the anchor starts to return to the player.
		/// </summary>
		public int Range;
		
		/// <summary>
		/// The amount of slams gained from hitting with a fully charged throw.
		/// </summary>
		public int SlamStacks;
		
		/// <summary>
		/// The amount of guards gained from hitting with a fully charged throw.
		/// </summary>
		public int GuardStacks;
		
		/// <summary>
		/// The time span of blocking with the hammer before it returns in ticks.
		/// </summary>
		public int BlockDuration;
		
		/// <summary>
		/// If true, the anchor will penetrate through enemies indefinitely instead of returning on hit.
		/// <br/> Defaults to <see langword="false"/>.
		/// </summary>
		public bool Penetrate;
		
		/// <summary>
		/// If true, the anchor will collide with tiles.
		/// <br/> Defaults to <see langword="true"/>.
		/// </summary>
		public bool TileCollide;
		
		/// <summary>
		/// If true, the anchor will bounce off tiles and preserve its velocity instead of immediately returning on collision.
		/// <br/>Defaults to <see langword="false"/>.
		/// </summary>
		public bool TileBounce;
		
		/// <summary>
		/// Multiplier for the velocity of the hammer when returning to the player.
		/// </summary>
		public float ReturnSpeed;
		
		/// <summary>
		/// Multiplier for the speed of a melee swing.
		/// </summary>
		public float SwingSpeed;
		
		/// <summary>
		/// Multiplier for the amount of bonus charge gained from hitting with a melee swing.
		/// </summary>
		public float SwingChargeGain;
		
		/// <summary>
		/// Multiplier for the amount of damage inflicted by a melee swing.
		/// <br/> If the hammer charge is less than full, this multiplier is increased by 50%.
		/// </summary>
		public float SwingDamage;
		
		/// <summary>
		/// Multiplier for the amount of damage inflicted by a throw hit.
		/// <br>/ If <see cref="Penetrate">Penetrate</see> is false, this multiplier is decreased by 25% for each enemy hit other than the initial target.
		/// </summary>
		public float ThrowDamage;
		
		/// <summary>
		/// Multiplier for the amount of damage inflicted by a block hit.
		/// </summary>
		public float BlockDamage;
		
		public int HitCooldown;
		
		/// <summary>
		/// If true, the warhammer will never be swung.
		/// </summary>
		public bool CannotSwing;
		
		/// <summary>
		/// Offsets the drawing of the warhammer while being held (in pixels). Negative values draw it closer, while positive further.
		/// </summary>
		public float HoldOffset;
		
		/// <summary>
		/// Multiplier for the initial velocity of the hammer when blocking.
		/// </summary>
		public float BlockVelocityMult;
		
		/// <summary>
		/// If true, the anchor will load and use ItemName_Hammer.png as its texture.
		/// </summary>
		public bool hasSpecialHammerTexture = false;
		
		/// <summary>
		/// The file name of the anchor's texture file, if <see cref="hasSpecialHammerTexture">hasSpecialHammerTexture</see> is true.
		/// </summary>
		public virtual string HammerTexture => Texture + "_Hammer";
		
		/// <summary>
		/// Called upon pushing an enemy with a throw (can happen repeatedly).
		/// </summary>
		public virtual void OnBlockContact(Player player, OrchidGuardian guardian, NPC target, Projectile projectile) { } 
		
		/// <summary>
		/// Called upon blocking an enemy (1 time per throw per enemy).
		/// </summary>
		public virtual void OnBlockNPC(Player player, OrchidGuardian guardian, NPC target, Projectile projectile) { } 

		/// <summary>
		/// Called upon blocking the first enemy of a blocking throw.
		/// </summary>
		public virtual void OnBlockFirstNPC(Player player, OrchidGuardian guardian, NPC target, Projectile projectile) { } 

		/// <summary>
		/// Called upon blocking a proejctile. Return false to prevent the projectile from being destroyed. Returns true by default.
		/// </summary
		public virtual bool OnBlockProjectile(Player player, OrchidGuardian guardian, Projectile projectileHammer, Projectile projectileBlocked) { return true; } 

		/// <summary>
		/// Called upon blocking the first projectile of a blocking throw.
		/// </summary>
		public virtual void OnBlockFirstProjectile(Player player, OrchidGuardian guardian, Projectile projectileHammer, Projectile projectileBlocked) { } 

		/// <summary>
		/// Called upon landing any melee swing hit.
		/// </summary>
		public virtual void OnMeleeHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, float knockback, bool crit, bool FullyCharged) { } 

		/// <summary>
		/// Called upon landing the first hit of a melee swing.
		/// </summary>
		public virtual void OnMeleeHitFirst(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, float knockback, bool crit, bool FullyCharged) { } 

		/// <summary>
		/// Called upon landing any throw hit.
		/// </summary>
		public virtual void OnThrowHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, float knockback, bool crit, bool Weak) { } 

		/// <summary>
		/// Called upon landing the first hit of a throw.
		/// </summary>
		public virtual void OnThrowHitFirst(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, float knockback, bool crit, bool Weak) { } 

		/// <summary>
		/// Called upon landing any block hit.
		/// </summary>
		public virtual void OnBlockHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, float knockback, bool crit) { } 

		/// <summary>
		/// Called upon landing the first hit of a block.
		/// </summary>
		public virtual void OnBlockHitFirst(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, float knockback, bool crit) { } 

		/// <summary>
		/// Called upon colliding with a tile.
		/// </summary>
		/// <param name="oldVelocity">The velocity of the anchor upon collision.</param>
		public virtual void OnThrowTileCollide(Player player, OrchidGuardian guardian, Projectile projectile, Vector2 oldVelocity) { }

		/// <summary>
		/// Called on the first frame of a throw.
		/// </summary>
		/// <param name="FullyCharged">Whether or not the guardian's hammer charge is full.</param>
		public virtual void OnSwing(Player player, OrchidGuardian guardian, Projectile projectile, bool FullyCharged) { } 

		/// <summary>
		/// Called on the first frame of a swing.
		/// </summary>
		public virtual void OnThrow(Player player, OrchidGuardian guardian, Projectile projectile, bool Weak) { } 

		/// <summary>
		/// Called on the first frame of a block.
		/// </summary>
		public virtual void OnBlockThrow(Player player, OrchidGuardian guardian, Projectile projectile) { } 

		/// <summary>
		/// Allows you to determine how this projectile behaves. This will be called at the end of the anchor's <c>Projectile.AI()</c>. 
		/// </summary>
		public virtual void ExtraAI(Player player, OrchidGuardian guardian, Projectile projectile) { } 
		
		/// <summary>
		/// Called before default throw AI. Return false to prevent the default AI from running.
		/// </summary>
		/// <remarks>
		/// Remember to set <c>Projectile.friendly</c> and <c>OrchidModGuardianProjectile.ResetHitStatus()</c> if overriding default behavior.
		/// </remarks>
		public virtual bool ThrowAI(Player player, OrchidGuardian guardian, Projectile projectile, bool Weak) => true;
		
		/// <summary>
		/// Called before drawing the hammer. Return false to prevent the default draw code from running.
		/// </summary>
		/// <remarks>
		/// The default draw code will use hammerTexture and drawRectangle (which defaults to null)
		/// </remarks>
		public virtual bool PreDrawHammer(Player player, OrchidGuardian guardian, Projectile projectile, SpriteBatch spriteBatch, ref Color lightColor, ref Texture2D hammerTexture, ref Rectangle drawRectangle) => true;
		
		/// <summary>
		/// Called after drawing the hammer.
		/// </summary>
		public virtual void PostDrawHammer(Player player, OrchidGuardian guardian, Projectile projectile, SpriteBatch spriteBatch, Color lightColor, Texture2D hammerTexture, Rectangle drawRectangle) { }
		public virtual Color GetHammerGlowmaskColor(Player player, OrchidGuardian guardian, Projectile projectile, Color lightColor) => Color.White;

		public sealed override void SetDefaults()
		{
			Item.DamageType = GetInstance<GuardianDamageClass>();
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.UseSound = SoundID.Item1;
			Item.autoReuse = false;
			Item.maxStack = 1;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 30;
			Item.knockBack = 10f;
			Item.shootSpeed = 10f;
			Range = 0;
			HitCooldown = 30;
			Penetrate = false;
			TileBounce = false;
			TileCollide = true;
			SlamStacks = 0;
			ThrowDamage = 1f;
			ReturnSpeed = 1f;
			SwingSpeed = 1f;
			SwingDamage = 0.5f;
			SwingChargeGain = 1f;
			BlockDamage = 0.33f;
			BlockDuration = 180;
			BlockVelocityMult = 1f;

			OrchidGlobalItemPerEntity orchidItem = Item.GetGlobalItem<OrchidGlobalItemPerEntity>();
			orchidItem.guardianWeapon = true;

			SafeSetDefaults();

			Item.useAnimation = Item.useTime;
		}

		public override bool AltFunctionUse(Player player)
		{
			return true;
		}

		public override bool WeaponPrefix() => true;

		public sealed override void HoldItem(Player player)
		{
			var guardian = player.GetModPlayer<OrchidGuardian>();
			guardian.GuardianDisplayUI = 300;
		}

		public override bool? UseItem(Player player)
		{
			var guardian = player.GetModPlayer<OrchidGuardian>();
			int projType = ProjectileType<GuardianHammerAnchor>();
			int damage = guardian.GetGuardianDamage(Item.damage);
			Projectile projectile = Projectile.NewProjectileDirect(Item.GetSource_FromThis(), player.Center, Vector2.Zero, projType, damage, Item.knockBack, player.whoAmI);
			projectile.CritChance = (int)(player.GetCritChance<GuardianDamageClass>() + player.GetCritChance<GenericDamageClass>() + Item.crit);

			if (Main.mouseRight && Main.mouseRightRelease && projectile.ModProjectile is GuardianHammerAnchor anchor && guardian.UseGuard(1, true))
			{
				guardian.UseGuard(1);
				projectile.velocity = Vector2.Normalize(Main.MouseWorld - player.Center) * (10f + (Item.shootSpeed - 10f) * 0.35f * BlockVelocityMult);
				projectile.friendly = true;
				projectile.knockBack = 0f;
				projectile.tileCollide = true;

				anchor.BlockDuration = (int)(BlockDuration * Item.GetGlobalItem<GuardianPrefixItem>().GetBlockDuration() * guardian.GuardianBlockDuration + 10);
				anchor.NeedNetUpdate = true;
			}

			guardian.GuardianItemCharge = 0f;
			return true;
		}
		
		public override bool CanUseItem(Player player)
		{
			int projType = ProjectileType<GuardianHammerAnchor>();

			if (Main.mouseRight && Main.mouseRightRelease)
			{
				var proj = Main.projectile.FirstOrDefault(i => i.active && i.owner == player.whoAmI && i.type == projType && i.ModProjectile is GuardianHammerAnchor warhammer && warhammer.BlockDuration > 0);
				if (proj != null && proj.ModProjectile is GuardianHammerAnchor warhammer)
				{ // recalls existing blocking warhammers when right clicking
					warhammer.BlockDuration = -30; // -30 instead of -1 so they return faster
					proj.netUpdate = true;
				}
			}

			if (player.ownedProjectileCounts[projType] > 0 || (!(Main.mouseRight && Main.mouseRightRelease && player.GetModPlayer<OrchidGuardian>().UseGuard(1, true)) && !Main.mouseLeft)) return false;
			return base.CanUseItem(player);
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

			tooltips.Insert(index + 1, new TooltipLine(Mod, "BlockDuration", Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.BlockDuration", OrchidUtils.FramesToSeconds((int)(BlockDuration * Item.GetGlobalItem<GuardianPrefixItem>().GetBlockDuration() * guardian.GuardianBlockDuration)))));

			string click = Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.RightClick");
			tooltips.Insert(index + 2, new TooltipLine(Mod, "ClickInfo", Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Block", click))
			{
				OverrideColor = new Color(175, 255, 175)
			});

			tooltips.Insert(index + 3, new TooltipLine(Mod, "Swing", Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.ChargeToThrow"))
			{
				OverrideColor = new Color(175, 255, 175)
			});

			if (GuardStacks > 0 || SlamStacks > 0)
			{
				string TooltipToGet = GetInstance<OrchidMod>().GetLocalizationKey("Misc.GuardianGrants");
				switch(GuardStacks)
				{
					case > 0: TooltipToGet += "Guard"; break;
				}
				switch (SlamStacks)
				{
					case > 0: TooltipToGet += "Slam"; break;
				}
				if (GuardStacks == SlamStacks) TooltipToGet += "Same";

				tooltips.Insert(index + 1, new TooltipLine(Mod, "GuardianGrants", Language.GetText(TooltipToGet).Format(GuardStacks, SlamStacks))
				{
					OverrideColor = new Color(175, 255, 175)
				});
			}
		}
	}
}
