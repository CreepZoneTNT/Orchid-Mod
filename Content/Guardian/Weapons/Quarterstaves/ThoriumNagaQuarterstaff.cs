using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using OrchidMod.Common.Attributes;
using OrchidMod.Content.Guardian.Projectiles.Quarterstaves;

namespace OrchidMod.Content.Guardian.Weapons.Quarterstaves 
{

	[CrossmodContent("ThoriumMod")]
    public class ThoriumNagaQuarterstaff : OrchidModGuardianQuarterstaff
    {
	    public bool altJab = true;
	    private int altJabResetTimer = 0; 
        
        public override void SafeSetDefaults()
        {
            Item.width = 48;
            Item.height = 48;
            Item.value = Item.sellPrice(0, 2);
            Item.rare = ItemRarityID.LightRed;
            Item.UseSound = SoundID.Item71.WithPitchOffset(0.5f).WithVolumeScale(0.5f);
            Item.useTime = 14;
            ParryDuration = 55;
            Item.knockBack = 6f;
            Item.damage = 126;
            Item.shootSpeed = 15f;
            JabStyle = 2;
            JabSpeed = 0.9f;
            JabDamage = 0.75f;
            JabChargeGain = 1.5f;
            SwingStyle = 0;
            SwingSpeed = 0.8f;
            GuardStacks = 1;
            SlamStacks = 1;
        }

        public override void SafeHoldItem(Player player)
        {
            player.trident = true;
            
            if (altJabResetTimer > 0)
            {
	            altJabResetTimer--;
	            if (altJabResetTimer < 0)
	            {
		            altJab = true;
		            altJabResetTimer = 0;
	            }
            }
        }

        public override void UpdateInventory(Player player)
        {
	        if (player.HeldItem != Item && !altJab)
	        {
		        altJab = true;
		        altJabResetTimer = 0;
	        }
        }

        public override void OnParryQuarterstaff(Player player, OrchidGuardian guardian, Entity aggressor, Projectile anchor)
        {
	        Vector2 direction = Main.rand.NextVector2Unit();
	        float targetVelocity = Item.shootSpeed;
	        
	        if (aggressor is NPC)
	        {
		        Vector2 projectedDir = player.velocity + aggressor.velocity;
		        float distance = projectedDir.Length();
		        if (distance >= 160f) distance = 160f;
		        targetVelocity = MathHelper.Clamp(distance / 19.792f, 4f, Item.shootSpeed);
		        direction = Vector2.Normalize(projectedDir);
	        }
	        
	        
	        int damage = player.GetWeaponDamage(Item);
	        int projectileType = ModContent.ProjectileType<ThoriumNagaQuarterstaffProjectile>();
	        Projectile booble = Projectile.NewProjectileDirect(Item.GetSource_FromAI(), player.Center, direction * targetVelocity, projectileType, damage, 2f, anchor.owner);
	        booble.CritChance = guardian.GetGuardianCrit(Item.crit);
        }

        public override void OnAttack(Player player, OrchidGuardian guardian, Projectile projectile, bool jabAttack, bool counterAttack)
		{
			if (projectile.ModProjectile is GuardianQuarterstaffAnchor && !counterAttack)
			{
				if (!jabAttack)
				{
					Vector2 tipPosition = projectile.Center - Vector2.UnitY.RotatedBy(projectile.rotation + MathHelper.PiOver4) * projectile.width * 0.4f;
					Vector2 velocity = Vector2.UnitY.RotatedBy(projectile.ai[1]);
						
					int damage = guardian.GetGuardianDamage(Item.damage * 1.2f);
					int projectileType = ModContent.ProjectileType<ThoriumNagaQuarterstaffProjectileAlt>();
					Projectile.NewProjectileDirect(projectile.GetSource_FromAI(), tipPosition, velocity * Item.shootSpeed * 0.8f, projectileType, damage, Item.knockBack, player.whoAmI);
				}
				else
				{
					Vector2 direction = Vector2.UnitY.RotatedBy(projectile.ai[1]);
					
					if (IsLocalPlayer(player))
					{
						altJab = !altJab;
						altJabResetTimer = 120;
						
						float distance = (Main.MouseWorld - player.Center).Length();
						if (distance >= 800f) distance = 800f;
						float targetVelocity = MathHelper.Clamp(distance / 19.792f, 4f, Item.shootSpeed);

						int damage = player.GetWeaponDamage(Item);
						int projectileType = ModContent.ProjectileType<ThoriumNagaQuarterstaffProjectile>();
						Projectile booble = Projectile.NewProjectileDirect(Item.GetSource_FromAI(), player.Center, direction * targetVelocity + player.velocity * 0.5f, projectileType, damage, 0f, projectile.owner);
						booble.CritChance = guardian.GetGuardianCrit(Item.crit);
					}
					
					for (int i = 0; i < 10; i++)
					{
						Dust dust = Dust.NewDustPerfect(player.Center + direction, DustID.TintableDustLighted, Main.rand.NextVector2CircularEdge(2.5f, 2.5f), Scale: 2f, newColor: ThoriumNagaQuarterstaffProjectile.GetOwnerColor(player.whoAmI));
						dust.noGravity = true;
					}

					SoundEngine.PlaySound(SoundID.Item111 with { PitchVariance = 0.8f, Volume = 0.5f }, player.Center + direction);
				}
			}
		}

		public override bool PreJabAI(Player player, OrchidGuardian guardian, Projectile anchor)
		{
			int direction = -altJab.ToDirectionInt();
			
			anchor.rotation = anchor.ai[1] - MathHelper.PiOver4 + (float)Math.Cos(0.102f * (-anchor.ai[0] - 9)) * 1.9f * player.direction * direction + MathHelper.Pi;
			anchor.Center = player.MountedCenter.Floor() + Vector2.UnitY.RotatedBy(anchor.ai[1] + (float)Math.Cos(0.102f * (-anchor.ai[0] - 9)) * 1.8f * player.direction * direction) * 24f;
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, MathHelper.PiOver4 * player.direction + anchor.ai[1] + 0.1f - (float)Math.Cos(0.102f * (-anchor.ai[0] - 9)) * -player.direction * direction);
			player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, anchor.ai[1] - 0.1f + (float)Math.Cos(0.102f * (-anchor.ai[0]- 9)) * 0.2f * -player.direction * direction);

			return false;
		}

		public override void AddRecipes()
		{
			var thoriumMod = OrchidMod.ThoriumMod;
			if (thoriumMod != null)
			{
				CreateRecipe()
				.AddTile(TileID.MythrilAnvil)
				.AddIngredient(thoriumMod, "AbyssalChitin", 8)
				.Register();
			}
		}
    }    
    
    // "boobie"
    //		-Watercooler
    /*
	  █▓▓▓▓▓▓▓▓█        
	 ▓▓▒▒▒▒▒▒▒▒▓▓       
	 █▓▓▓▓▓▓▓▓▓▓█       
	 █▓▓▒▒▒▒▒▒▓▓█       
	 █▓▓▓▓▓▓▓▓▓▓█   ███ 
	 ▓▓▒▓▓▓▓▓▓▒▓▓   █▓▓ 
	 █▓▓▓▓▓▓▓▓▓▓█   █▒▓ 
	 ▓▓▓▓▓▓▓▓▓▓▓▓  █▓▓▓█
	█▒██▓▓▓▓▓▓██▒█ █▒▒▒█
	█▒▒▒▒▒▒▒▒▒▒▒▒█▒▓▒▒▒█
	█▒▒▒▒▒▒▒▒▒▒▒▒█▒▓▒▒▒█
	█▒▒▓▓▓▓▓▓▓▓▒▒█▒▓▒▒▒█
	█▒█▒▒▒▒▒▒▒▒█▒█ █▒▒▒█
	█▒█▒▓█▒▒██▒█▒█ █▓▓▓█
	█▒█▒▒▒▒▒▒▒▒█▒█  █▓▓ 
	█▓██████████▒█      
	 █▒▒▒▒▒▒▒▒▒▒█       
     */
}
