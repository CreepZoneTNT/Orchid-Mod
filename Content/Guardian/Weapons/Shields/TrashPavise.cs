using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using OrchidMod.Assets;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace OrchidMod.Content.Guardian.Weapons.Shields
{
	public class TrashPavise : OrchidModGuardianShield
	{
		
		private SoundStyle BonkSound = new ("OrchidMod/Assets/Sounds/PVZShieldHit_", 2) { PitchRange = (-0.1f, 0.1f), MaxInstances = 10 };
		
		public bool FirstBlock = false;
		public Vector2 ShakeAmount = Vector2.Zero;
		private Vector2 OldShakeAmount = Vector2.Zero;
		private List<int> BlockedEnemies;
		
		public override void SafeSetDefaults()
		{
			Item.value = Item.sellPrice(0, 0, 0, 0);
			Item.width = 38;
			Item.height = 38;
			Item.noUseGraphic = true;
			Item.UseSound = SoundID.Item1;
			Item.knockBack = 16f;
			Item.damage = 54;
			Item.rare = ItemRarityID.Gray;
			Item.useTime = 24;
			distance = 32f;
			slamDistance = 55f;
			blockDuration = 60;
			blockRotation = 5f;
			BlockedEnemies = [];
		}

		public override void ExtraAIShield(Player player, OrchidGuardian guardian, Projectile shield)
		{
			if (shield.ModProjectile is GuardianShieldAnchor)
			{
				if (shield.ai[0] > 0)
				{
					if (ShakeAmount.Length() > 0)
					{
						OldShakeAmount = ShakeAmount;
						shield.Center += OldShakeAmount;
						ShakeAmount -= Vector2.Normalize(ShakeAmount) * MathF.Max(ShakeAmount.Length() % 0.4f, 0);
						if (ShakeAmount.Length() > 8f)
							ShakeAmount *= 8f / ShakeAmount.Length();
						shield.Center -= ShakeAmount;
					}
				}
				else
				{
					if (ShakeAmount.Length() > 0f)
						ShakeAmount = OldShakeAmount = Vector2.Zero;
					if (BlockedEnemies.Count > 0)
					{
						Gore.NewGoreDirect(shield.GetSource_FromAI(), shield.Center, Main.rand.NextVector2Circular(0.5f, 0.5f), GoreID.FartCloud1);
						Gore.NewGoreDirect(shield.GetSource_FromAI(), shield.Center, Main.rand.NextVector2Circular(0.5f, 0.5f), GoreID.FartCloud2);
						Gore.NewGoreDirect(shield.GetSource_FromAI(), shield.Center, Main.rand.NextVector2Circular(0.5f, 0.5f), GoreID.FartCloud3);
						// SoundEngine.PlaySound(SoundID.Item16);
						BlockedEnemies.Clear();
					}
				}
				
			}
		}

		public override void Push(Player player, OrchidGuardian guardian, Projectile shield, NPC npc)
		{
			if (!BlockedEnemies.Contains(npc.whoAmI))
			{
				if (BlockedEnemies.Count == 0) FirstBlock = true;
				// SoundEngine.PlaySound(BonkSound with { Volume = FirstBlock ? 1f : 0.5f }, shield.Center);
				BlockedEnemies.Add(npc.whoAmI);
			}
		}

		public override bool Block(Player player, OrchidGuardian guardian, Projectile shield, Projectile projectile)
		{
			FirstBlock = true;
			ShakeAmount -= Vector2.Normalize(projectile.velocity) * 2f;
			Dust.NewDustDirect(shield.Center + 0.5f * (projectile.Center - shield.Center), 2, 2, DustID.Lead, -projectile.velocity.X * 0.5f, -projectile.velocity.Y * 0.5f);
			// SoundEngine.PlaySound(BonkSound with { Volume = FirstBlock ? 1f : 0.5f }, shield.Center);
			return true;
		}

		public override bool BlockEnd(Player player, OrchidGuardian guardian, Projectile shield)
		{
			FirstBlock = false;
			return true;
		}

		// public override void SlamHit(Player player, OrchidGuardian guardian, Projectile shield, NPC npc, bool WeakSlam) => npc.AddBuff(BuffID.Stinky, WeakSlam ? 100 : 300);
	}
}
