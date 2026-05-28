using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common.Global.Items;
using OrchidMod.Content.General.Prefixes;
using OrchidMod.Content.Guardian;
using OrchidMod.Content.Guardian.Projectiles.Warhammers;
using OrchidMod.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.Initializers;

namespace OrchidMod.Content.Guardian.Weapons.Warhammers
{
    public class ToyWarhammers : OrchidModGuardianHammer
    {
	    
        public static SoundStyle SqueakSound = new SoundStyle("OrchidMod/Assets/Sounds/Squeak") { PitchRange = (-0.2f, 0.2f), MaxInstances = 5 };

        public override void SafeSetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.value = Item.sellPrice(0, 7, 50);
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = SoundID.DD2_MonkStaffSwing;
            Item.knockBack = 3;
            Item.shootSpeed = 20f;
            Item.damage = 60;
            Item.useTime = 10;
            Range = 60;
            TileBounce = true;
            GuardStacks = 1;
            ReturnSpeed = 1.8f;
            BlockDuration = 60;
            hasSpecialHammerTexture = true;
            HoldOffset = -2f;
            CannotBlock = true;
        }

        public override bool? UseItem(Player player)
        {
	        var guardian = player.GetModPlayer<OrchidGuardian>();

	        if (Main.mouseLeft)
	        { // If the player does a valid RMB input (the weapon can block) or a mouseleft input - so we don't create a proejctile for nothing in edge cases
		        int damage = guardian.GetGuardianDamage(Item.damage);
		        
		        int projTypeMain = ModContent.ProjectileType<GuardianHammerAnchor>();
		        Projectile projMain = Projectile.NewProjectileDirect(Item.GetSource_FromThis(), player.Center, Vector2.Zero, projTypeMain, damage, Item.knockBack, player.whoAmI);
		        projMain.CritChance = (int)(player.GetCritChance<GuardianDamageClass>() + player.GetCritChance<GenericDamageClass>() + Item.crit);

		        bool altExists = false;
		        Projectile projAlt = GetAltProjectile(player, out ToyWarhammerProjectile _);
		        if (projAlt == null)
		        {
			        int projTypeAlt = ModContent.ProjectileType<GuardianHammerAnchor>();
			        projAlt = Projectile.NewProjectileDirect(Item.GetSource_FromThis(), player.Center, Vector2.Zero, projTypeAlt, damage, Item.knockBack, player.whoAmI);
			        projAlt.CritChance = (int)(player.GetCritChance<GuardianDamageClass>() + player.GetCritChance<GenericDamageClass>() + Item.crit);
		        }
		        else altExists = true;

		        guardian.GuardianItemCharge = 0f;
		        return true;
	        }

	        return false;
        }

		public override void ExtraAI(Player player, OrchidGuardian guardian, Projectile projectile)
		{
			Projectile altProj = GetAltProjectile(player, out ToyWarhammerProjectile altAnchor);
			if (altProj != null && altAnchor != null && altProj.ai[1] <= 0)
			{
				if (altProj.ai[1] == 0)
				{
					if (projectile.ai[1] == 1 && guardian.GuardianItemCharge < 210f)
					{
						guardian.GuardianItemCharge += 30f / Item.useTime * player.GetTotalAttackSpeed(DamageClass.Melee);

						if (guardian.GuardianItemCharge > 210f) guardian.GuardianItemCharge = 210f;
					}

					if (player.whoAmI == Main.myPlayer)
					{
						if (!player.controlUseItem)
						{
							if (player.boneGloveItem != null && !player.boneGloveItem.IsAir && player.boneGloveTimer == 0)
							{ // Bone glove compatibility, from vanilla code
								player.boneGloveTimer = 60;
								Vector2 center = player.Center;
								Vector2 vector = player.DirectionTo(player.ApplyRangeCompensation(0.2f, center, Main.MouseWorld)) * 10f;
								Projectile.NewProjectile(player.GetSource_ItemUse(player.boneGloveItem), center.X, center.Y, vector.X, vector.Y, ProjectileID.BoneGloveProj, 25, 5f, player.whoAmI);
							}

							if (guardian.GuardianItemCharge > 10f)
							{ // Hammer is charged enough to be thrown (or can't be thrown)
								altProj.ai[1] = 1;

								Vector2 dir = Vector2.Normalize(Main.MouseWorld - player.Center) * Item.shootSpeed * (IgnoreHammerThrowVelocity ? 1f : guardian.GuardianHammerThrowVelocity);

								if (guardian.ThrowLevel() < 4)
								{
									dir *= (0.3f * (guardian.ThrowLevel() + 2) / 3);
									altProj.damage = (int)(altProj.damage * 0.75f);
									altProj.knockBack = (int)(altProj.knockBack / 3f);
									altProj.ai[0] = 1f;
								}

								altProj.velocity = dir;
								altProj.rotation = dir.ToRotation();
								altProj.direction = altProj.spriteDirection;
								altProj.netUpdate = true;

								guardian.GuardianItemCharge = 0;
							}
							else
							{ // charged too little, hammer is swung
								altProj.ai[1] = -61f;
								altProj.netUpdate = true;
							}
						}
						else if (Main.mouseRight)
						{
							if (player.boneGloveItem != null && !player.boneGloveItem.IsAir && player.boneGloveTimer == 0)
							{ // Bone glove compatibility, from vanilla code
								player.boneGloveTimer = 60;
								Vector2 center = player.Center;
								Vector2 vector = player.DirectionTo(player.ApplyRangeCompensation(0.2f, center, Main.MouseWorld)) * 10f;
								Projectile.NewProjectile(player.GetSource_ItemUse(player.boneGloveItem), center.X, center.Y, vector.X, vector.Y, ProjectileID.BoneGloveProj, 25, 5f, player.whoAmI);
							}

							altProj.ai[1] = -60f;
							altProj.netUpdate = true;
						}
					}
				}
			}
		}

		public override void OnSwing(Player player, OrchidGuardian guardian, Projectile projectile, bool FullyCharged)
		{
			Projectile altProj = GetAltProjectile(player, out ToyWarhammerProjectile altAnchor);
			if (altProj != null && altAnchor != null && altProj.ai[1] == 0)
			{
				if (player.boneGloveItem != null && !player.boneGloveItem.IsAir && player.boneGloveTimer == 0)
				{
					// Bone glove compatibility, from vanilla code
					player.boneGloveTimer = 60;
					Vector2 center = player.Center;
					Vector2 vector = player.DirectionTo(player.ApplyRangeCompensation(0.2f, center, Main.MouseWorld)) * 10f;
					Projectile.NewProjectile(player.GetSource_ItemUse(player.boneGloveItem), center.X, center.Y, vector.X, vector.Y, ProjectileID.BoneGloveProj, 25, 5f, player.whoAmI);
				}

				altProj.ai[1] = -60f;
				altProj.netUpdate = true;
			}
		}

		public override void OnThrow(Player player, OrchidGuardian guardian, Projectile projectile, bool Weak)
		{
			if (projectile.ModProjectile is GuardianHammerAnchor)
			{
				Projectile altProj = GetAltProjectile(player, out ToyWarhammerProjectile altAnchor);
				if (altProj != null && altAnchor != null && altProj.ai[1] == 0)
				{
					if (player.boneGloveItem != null && !player.boneGloveItem.IsAir && player.boneGloveTimer == 0)
					{ // Bone glove compatibility, from vanilla code
						player.boneGloveTimer = 60;
						Vector2 center = player.Center;
						Vector2 vector = player.DirectionTo(player.ApplyRangeCompensation(0.2f, center, Main.MouseWorld)) * 10f;
						Projectile.NewProjectile(player.GetSource_ItemUse(player.boneGloveItem), center.X, center.Y, vector.X, vector.Y, ProjectileID.BoneGloveProj, 25, 5f, player.whoAmI);
					}

					if (guardian.GuardianItemCharge > 10f)
					{ // Hammer is charged enough to be thrown (or can't be thrown)
						altProj.ai[1] = 1;

						Vector2 dir = Vector2.Normalize(Main.MouseWorld - player.Center) * Item.shootSpeed * (IgnoreHammerThrowVelocity ? 1f : guardian.GuardianHammerThrowVelocity);

						if (guardian.ThrowLevel() < 4)
						{
							dir *= (0.3f * (guardian.ThrowLevel() + 2) / 3);
							altProj.damage = (int)(altProj.damage * 0.75f);
							altProj.knockBack = (int)(altProj.knockBack / 3f);
							altProj.ai[0] = 1f;
						}

						altProj.velocity = dir;
						altProj.rotation = dir.ToRotation();
						altProj.direction = altProj.spriteDirection;
						altProj.netUpdate = true;

						guardian.GuardianItemCharge = 0;
					}
					else
					{ // charged too little, hammer is swung
						altProj.ai[1] = -61f;
						altProj.netUpdate = true;
					}
					projectile.Kill();
				}
			}
		}

		public override void OnMeleeHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, float knockback, bool crit, bool FullyCharged) => SoundEngine.PlaySound(SqueakSound);

        public override void OnThrowHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, float knockback, bool crit, bool Weak) => SoundEngine.PlaySound(SqueakSound);

        public override void OnThrowTileCollide(Player player, OrchidGuardian guardian, Projectile projectile, Vector2 oldVelocity) => SoundEngine.PlaySound(SqueakSound);

        public Projectile GetAltProjectile(Player player, out ToyWarhammerProjectile altAnchor)
        {
	        Projectile altProj = Main.projectile.FirstOrDefault(proj => proj.whoAmI < Main.maxProjectiles && proj.active && proj.owner == Main.myPlayer && proj.type == ModContent.ProjectileType<ToyWarhammerProjectile>());
	        altAnchor = null;
	        if (altProj != null && altProj.ModProjectile is ToyWarhammerProjectile anchor)
	        {
		        altAnchor = anchor;
		        return altProj;
	        }
	        return null;
        }
    }
}

