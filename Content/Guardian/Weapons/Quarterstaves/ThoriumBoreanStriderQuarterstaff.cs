using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod;
using OrchidMod.Common.Attributes;
using OrchidMod.Common.ModObjects;
using OrchidMod.Content.Guardian;
using OrchidMod.Content.Guardian.Projectiles.Gauntlets;
using OrchidMod.Content.Guardian.Projectiles.Quarterstaves;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Weapons.Quarterstaves
{
	[CrossmodContent("ThoriumMod")]
    public class ThoriumBoreanStriderQuarterstaff : OrchidModGuardianQuarterstaff
    {
        public Vector2 Tip;

        public override void SafeSetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.value = Item.sellPrice(0, 2);
            Item.rare = ItemRarityID.Pink;
            Item.useTime = 50;
            ParryDuration = 60;
            Item.knockBack = 4f;
            Item.shootSpeed = 8f;
            Item.damage = 380;
            GuardStacks = 2;
            SlamStacks = 2;
            JabChargeGain = 1.4f;
            ChargeRate = 0.25f;
        }

        public override void HoldItemFrame(Player player)
        {
	        if (Main.rand.NextBool(3))
	        {
		        Dust dust = Dust.NewDustDirect(player.Center - new Vector2(Item.width * 0.5f + 4, Item.width * 0.5f + 4), Item.width + 8, Item.width + 8, DustID.HallowSpray, Scale: 0.75f);
		        dust.noGravity = true;
	        }
        }

        public override void ExtraAIQuarterstaff(Player player, OrchidGuardian guardian, Projectile projectile)
        {
	        Tip = projectile.Center - Vector2.UnitY.RotatedBy(projectile.rotation + MathHelper.PiOver4) * projectile.width * 0.25f;

	        projectile.friendly = false;

	        // Code borrowed from FlamingQuarterstaff
	        bool bigAttack = projectile.ai[0] > 14 || projectile.ai[2] < 0;
	        if (Main.rand.NextBool(bigAttack ? 1 : 4))
	        {
	         Dust dust = Dust.NewDustDirect(Tip - new Vector2(8), 12, 12, DustID.HallowSpray, SpeedY: -Main.rand.NextFloat(3f), Scale: 0.75f);
	         switch (Main.rand.Next(10))
	         {
		         default:
			         dust.velocity *= 0.25f;
			         dust.velocity += player.velocity * 0.5f;
			         dust.scale *= 2.5f;
			         goto case 8;
		         case 6:
		         case 7:
		         case 8:
			         dust.noGravity = true;
			         dust.velocity *= 0.8f;
			         if (bigAttack)
			         {
				         if (projectile.ai[0] > 14) //swing
					         dust.velocity += new Vector2(-player.direction * (float)Math.Cos(projectile.ai[0] * 0.2f), -1).RotatedBy(projectile.rotation + MathHelper.PiOver4) * Main.rand.NextFloat(4f, 8f);
				         else //counter
					         dust.velocity += new Vector2(1, -1).RotatedBy(projectile.rotation + Main.rand.NextFloat(MathHelper.PiOver2)) * Main.rand.NextFloat(8f);
				         if (Main.rand.NextBool())
				         {
					         dust.scale += Main.rand.NextFloat(2f);
					         dust.velocity *= Main.rand.NextFloat(0.2f, 0.6f);
				         }
	        
				         dust.fadeIn += Main.rand.NextFloat(2.5f);
			         }
			         else if (projectile.ai[0] <= -30 && projectile.ai[0] >= -39) //jab
			         {
				         dust.velocity += new Vector2(-1, 1).RotatedBy(projectile.rotation) * (projectile.ai[0] + 30) * Main.rand.NextFloat(0.6f, 1.2f);
				         dust.fadeIn += Main.rand.NextFloat(1f);
			         }
	        
			         break;
		         case 9:
			         dust.scale *= Main.rand.NextFloat(0.5f, 1f);
			         break;
	         }
	        }
        }

        public override void OnAttack(Player player, OrchidGuardian guardian, Projectile projectile, bool jabAttack, bool counterAttack)
        {
            for (int i = 0; i < 15; i++)
            {
	            Dust swingDust = Dust.NewDustDirect(Tip - new Vector2(4, 4), 8, 8, DustID.HallowSpray, Scale: 0.75f);
	            swingDust.noGravity = true;
            }
            
            if (IsLocalPlayer(player))
            {
	            Vector2 velocity = Vector2.Normalize(Main.MouseWorld - player.MountedCenter) * Item.shootSpeed;
	            if (jabAttack)
		            velocity *= 0.4f;

	            int grantsRSS = !counterAttack || (jabAttack && projectile.ai[0] != 1) ? 1 : 0;
	            
	            Projectile flake = Projectile.NewProjectileDirect(projectile.GetSource_FromAI(), Tip, velocity, ModContent.ProjectileType<ThoriumBoreanStriderQuarterstaffProjectile>(), guardian.GetGuardianDamage(Item.damage), 2f, Main.myPlayer, projectile.whoAmI, grantsRSS);
	            if (!jabAttack || counterAttack) // Swings and counters
		            ((ThoriumBoreanStriderQuarterstaffProjectile)flake.ModProjectile).Strong = true;
	            else 
		            flake.damage = (int)(flake.damage * 0.12f);
            }
        }

        public override void OnHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit, bool jabAttack, bool counterAttack) { }
    }
}

