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
        public float SnowflakeStacks;    
        public Vector2 Tip;

        public override void SafeSetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.value = Item.sellPrice(0, 2);
            Item.rare = ItemRarityID.Pink;
            Item.useTime = 18;
            ParryDuration = 60;
            Item.knockBack = 4f;
            Item.shootSpeed = 8f;
            Item.damage = 140;
            GuardStacks = 1;
            SlamStacks = 1;
            JabChargeGain = 1.4f;
        }

        // public override void NetSend(BinaryWriter writer)
        // {
        //     writer.Write(SnowflakeStacks);
        // }
        //
        // public override void NetReceive(BinaryReader reader)
        // {
        //     SnowflakeStacks = reader.ReadSingle();
        // }

        public override void HoldItemFrame(Player player)
        {
	        if (player.dead)
		        SnowflakeStacks = 0;
	        else if (SnowflakeStacks < 10f)
		        DoFullChargeIndicator(player, 0.01f);
	        //
	        // Projectile projectile = Main.projectile.FirstOrDefault(proj => proj.whoAmI < Main.maxProjectiles && proj.active && proj.owner == Main.myPlayer && proj.type == AnchorType);
	        // if (projectile != null && projectile.localAI[1] != 0)
	        // {
		       //  OrchidPlayer modPlayer = player.GetModPlayer<OrchidPlayer>();
		       //  modPlayer.keepSelected = player.selectedItem;
		       //  modPlayer.autoRevertSelectedItem = true;
	        // }
        }

        public override void ExtraAIQuarterstaff(Player player, OrchidGuardian guardian, Projectile projectile)
        {
	        Tip = projectile.Center - Vector2.UnitY.RotatedBy(projectile.rotation + MathHelper.PiOver4) * projectile.width * 0.25f;
	        switch (projectile.ai[0])
	        {
		        case > 1f when (int)projectile.localAI[1] == 1:
			        if (IsLocalPlayer(player))
			        {
				        Projectile.NewProjectileDirect(
						        projectile.GetSource_FromAI(),
						        Tip,
						        Vector2.Normalize(Main.MouseWorld - player.MountedCenter) * Item.shootSpeed,
						        ModContent.ProjectileType<ThoriumBoreanStriderQuarterstaffProjectile>(),
						        guardian.GetGuardianDamage(Item.damage * 2f),
						        2f,
						        Main.myPlayer,
						        projectile.whoAmI,
						        0,
						        SwingSpeed * player.GetAttackSpeed(DamageClass.Melee)
					        )
					        .localAI[0] = projectile.ai[1];
			        }
			        projectile.localAI[1] = 2;
			        projectile.netUpdate = true;
			        break;
		        case <= 1 when (int)projectile.localAI[1] == 2:
			        projectile.localAI[1] = 0;
			        projectile.netUpdate = true;
			        break;
	        }

	        // // Code borrowed from FlamingQuarterstaff
	        // bool bigAttack = projectile.ai[0] > 14 || projectile.ai[2] < 0;
	        // if (Main.rand.NextBool(bigAttack ? 1 : 4))
	        // {
		       //  Dust dust = Dust.NewDustDirect(Tip - new Vector2(8),
			      //   12,
			      //   12,
			      //   DustID.HallowSpray,
			      //   SpeedY: -Main.rand.NextFloat(3f),
			      //   Scale: 0.75f);
		       //  switch (Main.rand.Next(10))
		       //  {
			      //   default:
				     //    dust.velocity *= 0.25f;
				     //    dust.velocity += player.velocity * 0.5f;
				     //    dust.scale *= 2.5f;
				     //    goto case 8;
			      //   case 6:
			      //   case 7:
			      //   case 8:
				     //    dust.noGravity = true;
				     //    dust.velocity *= 0.8f;
				     //    if (bigAttack)
				     //    {
					    //     if (projectile.ai[0] > 14) //swing
						   //      dust.velocity += new Vector2(
							  //       -player.direction * (float)Math.Cos(projectile.ai[0] * 0.2f),
							  //       -1).RotatedBy(projectile.rotation + MathHelper.PiOver4) * Main.rand.NextFloat(4f,
							  //       8f);
					    //     else //counter
						   //      dust.velocity += new Vector2(1,
							  //       -1).RotatedBy(projectile.rotation + Main.rand.NextFloat(MathHelper.PiOver2)) * Main.rand.NextFloat(8f);
					    //     if (Main.rand.NextBool())
					    //     {
						   //      dust.scale += Main.rand.NextFloat(2f);
						   //      dust.velocity *= Main.rand.NextFloat(0.2f,
							  //       0.6f);
					    //     }
	        //
					    //     dust.fadeIn += Main.rand.NextFloat(2.5f);
				     //    }
				     //    else if (projectile.ai[0] <= -30 && projectile.ai[0] >= -39) //jab
				     //    {
					    //     dust.velocity += new Vector2(-1,
						   //      1).RotatedBy(projectile.rotation) * (projectile.ai[0] + 30) * Main.rand.NextFloat(0.6f,
						   //      1.2f);
					    //     dust.fadeIn += Main.rand.NextFloat(1f);
				     //    }
	        //
				     //    break;
			      //   case 9:
				     //    dust.scale *= Main.rand.NextFloat(0.5f,
					    //     1f);
				     //    break;
		       //  }
	        // }
        }

        public override void OnAttack(Player player, OrchidGuardian guardian, Projectile projectile, bool jabAttack, bool counterAttack)
        {
            Dust swingDust = Dust.NewDustDirect(projectile.Center, projectile.width * 2, projectile.height * 2, DustID.HallowSpray, Scale: 0.75f);
            swingDust.noGravity = true;
            
            if (projectile.ModProjectile is GuardianQuarterstaffAnchor anchor && anchor.Ding && !jabAttack && projectile.localAI[1] == 0 && SnowflakeStacks >= 10f)
            {
	            projectile.localAI[1] = 1;
	            SoundEngine.PlaySound(SoundID.Item28);
	            SnowflakeStacks = 0;
	            projectile.netUpdate = true;

                // Item.NetStateChanged();
            }
        }

        public override void OnHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit, bool jabAttack, bool counterAttack)
        {
            if (projectile.ModProjectile is GuardianQuarterstaffAnchor anchor) {
	            for (int i = 0; i < 15; i++)
	            {
		            Dust.NewDustDirect(target.Center, 10, 10, DustID.MagicMirror, Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(-8f, 8f), 175, Scale: 1.5f).noGravity = true;
	            }
                if (anchor.FirstHit && projectile.localAI[1] == 0)
                {
	                if (SnowflakeStacks < 10)
	                {
		                float toAdd;
		                if (counterAttack) toAdd = 5f;
		                else if (!jabAttack) toAdd = 2.5f;
		                else toAdd = 1;
		                DoFullChargeIndicator(player, toAdd);
	                }

	                // Item.NetStateChanged();
                }

                Mod thoriumMod = OrchidMod.ThoriumMod;
                if (thoriumMod != null)
                {
	                int debuffType = thoriumMod.Find<ModBuff>("Freezing").Type;
	                target.AddBuff(debuffType, 120);
                }
            }
        }

    //     public override void QuarterstaffPostDrawUI(SpriteBatch spriteBatch, Player player, ref Color lightColor, Projectile projectile)
    //     {
	   //      Vector2 position = (player.position + new Vector2(player.width * 0.5f, player.height + player.gfxOffY + 12)).Floor();
	   //      Vector2 drawpos = new Vector2(position.X + 22, position.Y - 94 * player.gravDir + 3f * (player.gravDir - 1)) - Main.screenPosition;
    //
	   //      Texture2D snowflakeUIOff = ModContent.Request<Texture2D>(Texture + "_UIOff").Value;
	   //      Texture2D snowflakeUIOn = ModContent.Request<Texture2D>(Texture + "_UIOn").Value;
	   //      Texture2D snowflakeUIReady = ModContent.Request<Texture2D>(Texture + "_UIReady").Value;
	   //      
	   //      if (OrchidMod.OrchidClientConfig.GuardianThoriumBoreanStriderColorUI)
		  //       snowflakeUIOn = ModContent.Request<Texture2D>(ModContent.GetInstance<IceGauntletProjectile>().Texture).Value;
    //
	   //      
	   //      
	   //      int val = 26;
	   //      float stacks = SnowflakeStacks / 10f; 
	   //      while (stacks < 1)
	   //      {
		  //       stacks += 0.0385f;
		  //       val--;
	   //      }
	   //      Rectangle rectangle = snowflakeUIOff.Bounds;
	   //      rectangle.Height = val;
	   //      rectangle.Y = snowflakeUIOff.Height - val;
	   //      
	   //      if (SnowflakeStacks >= 10)
				// spriteBatch.Draw(snowflakeUIReady, drawpos - new Vector2(2f, 2f), null, Color.White * 0.8f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
	   //      
	   //      spriteBatch.Draw(snowflakeUIOff, drawpos, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
	   //      drawpos.Y += snowflakeUIOff.Height - val;
	   //      spriteBatch.Draw(snowflakeUIOn, drawpos, rectangle, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
    //
    //     }

        private void DoFullChargeIndicator(Player player, float toAdd)
        {
	        
	        if ((SnowflakeStacks += toAdd) >= 10f)
	        {
		        SnowflakeStacks = 10f;
		        SoundEngine.PlaySound(SoundID.Item4);
		        for (int i = 0; i < 10; i++)
		        {
			        Dust dust = Dust.NewDustPerfect(player.Center, DustID.FrostHydra, Main.rand.NextVector2CircularEdge(2.5f, 2.5f), Scale: 2f);
			        dust.noGravity = true;
		        }
	        }
        }
    }
}

