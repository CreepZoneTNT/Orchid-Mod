using System;
using Microsoft.Xna.Framework;
using OrchidMod.Common.ModObjects;
using System.Collections.Generic;
using OrchidMod.Assets;
using Terraria;
using Terraria.ID;

namespace OrchidMod.Content.Guardian.Projectiles.Quarterstaves
{
	public class ThoriumNagaQuarterstaffProjectileDash : OrchidModGuardianProjectile
	{
		public List<Vector2> OldPosition;
		public List<float> OldRotation;

		public Projectile Anchor;

		public override string Texture => OrchidAssets.InvisiblePath;

		public override void SafeSetDefaults()
		{
			Projectile.width = 64;
			Projectile.height = 64;
			Projectile.friendly = true;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 62;
			Projectile.scale = 1f;
			Projectile.penetrate = -1;
			Projectile.alpha = 255;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 60;
			Projectile.tileCollide = true;
			Strong = true;
		}

		public override void AI()
		{
			Player owner = Owner;
			OrchidPlayer orchidPlayer = owner.GetModPlayer<OrchidPlayer>();
			
			Projectile.Center = Anchor.Center;

			if (Projectile.owner == Main.myPlayer)
			{
				ref Vector2 forcedVelocity = ref orchidPlayer.ForcedVelocityVector;
				forcedVelocity = Vector2.UnitX.RotatedBy(forcedVelocity.ToRotation().AngleTowards(Projectile.AngleTo(Main.MouseWorld), MathHelper.Pi/(Projectile.timeLeft > 55 ? 15 : 60))) * forcedVelocity.Length() * 0.99f;
				Anchor.ai[1] = Vector2.Normalize(forcedVelocity).ToRotation() - MathHelper.PiOver2;
				Anchor.Center = owner.MountedCenter.Floor() + Vector2.UnitY.RotatedBy(Anchor.ai[1]) * (38f - (float)Math.Sin(0.0523f * (30 - Anchor.ai[0])) * 24f);
			}
			else
			{
				if (Projectile.timeLeft == 62)
				{
					orchidPlayer.ForcedVelocityVector = Projectile.velocity;
					orchidPlayer.ForcedVelocityTimer = 60;
					orchidPlayer.PlayerImmunity = 20;
					orchidPlayer.ForcedVelocityUpkeep = 0f;
				}
			}
		}

		public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
		{
			fallThrough = true;
			return true;
		}
	}
}