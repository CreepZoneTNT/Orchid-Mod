using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace OrchidMod.Content.Guardian.Projectiles.Katars
{
	// Modified version of ThoriumYewGauntletProjectile.cs
	public class ThoriumDreadKatarProjectile : OrchidModGuardianProjectile
	{
		public Vector2 InitialVelocity = Vector2.Zero;
		public int TimeSpent;

		public override void SafeSetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 120;
			Projectile.penetrate = -1;
			Projectile.friendly = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.alpha = 255;
		}

		public override bool? CanHitNPC(NPC target)
		{
			if (Projectile.ai[1] > -1 || TimeSpent > 25) return false;

			return base.CanHitNPC(target);
		}

		NPC HitNPC;
		public override void AI()
		{
			TimeSpent++;

			if (TimeSpent < 30 && IsLocalOwner && !Main.dedServ)
			{
				NPC homingTarget = null;
				float distanceClosest = 360f;
				float lowestScore = distanceClosest;
				foreach (NPC npc in Main.npc)
				{
					float distance = Projectile.Center.Distance(npc.Center); 
					float score = distance;
					if (npc.CircularizeHitbox(useEntity1LargestDim: false).HasPointInCircle(Main.MouseWorld))
						score *= 0.5f;
					if (npc.boss)
						score *= 0.5f;
					else if (npc.type == NPCID.TargetDummy)
						score *= 1.5f;

					if (IsValidTarget(npc) && distance < distanceClosest)
					{
						distanceClosest = distance;
						if (score < lowestScore)
						{
							homingTarget = npc;
							lowestScore = score;
						}
					}
				}
				
				if (homingTarget != null)
				{
					Vector2 newVelocity = Vector2.Normalize(homingTarget.Center - Projectile.Center) * 0.8f;
					Projectile.velocity = Projectile.velocity * 0.95f + newVelocity;
				}
			}
		}

		public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
		{
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (TimeSpent < 35)
			{
				TimeSpent = 35;
				SoundEngine.PlaySound(SoundID.Dig, Projectile.position);

				Vector2 target = Owner.Center;
				Projectile gauntlet = Main.projectile[(int)Projectile.ai[2]];
				if (gauntlet.active && gauntlet.ModProjectile is GuardianGauntletAnchor && gauntlet.owner == Owner.whoAmI)
					target = gauntlet.Center;
				else if (IsLocalOwner)
					Projectile.Kill();

				Projectile.velocity = Vector2.Normalize(target - Projectile.Center) * InitialVelocity.Length();
			}

			return false;
		}


		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			SpriteEffects spriteEffects = SpriteEffects.None;

			Texture2D projTexture = TextureAssets.Projectile[Projectile.type].Value;
			spriteBatch.Draw(projTexture, Projectile.Center - Main.screenPosition, null, Color.GreenYellow, Projectile.rotation, projTexture.Size() * 0.5f, Projectile.scale * 1.1f, spriteEffects, 0f);
			spriteBatch.Draw(projTexture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, projTexture.Size() * 0.5f, Projectile.scale, spriteEffects, 0f);

			return false;
		}
	}
}