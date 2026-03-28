using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common;
using OrchidMod.Common.ModObjects;
using OrchidMod.Content.Guardian.Weapons.Gauntlets;
using OrchidMod.Content.Shapeshifter.Accessories;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Projectiles.Gauntlets
{
	public class ThoriumYewGauntletProjectile : OrchidModGuardianProjectile
	{
		Vector2 InitialVelocity = Vector2.Zero;
		Vector2 NPCImpactPoint = Vector2.Zero;
		Vector2 NPCImpactVelocity = Vector2.Zero;
		int TimeSpent = 0;
		bool Flip = false;
		bool QuickPull = false;

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
			if (Projectile.ai[1] > -1 || TimeSpent > 25)
			{
				return false;
			}

			return base.CanHitNPC(target);
		}

		public override void AI()
		{
			TimeSpent++;

			if (!Initialized)
			{
				Initialized = true;
				InitialVelocity = Projectile.velocity;
				Flip = Projectile.velocity.X < 0;
				SoundEngine.PlaySound(SoundID.DD2_BallistaTowerShot.WithPitchOffset(0.5f), Projectile.Center);
			}

			Vector2 target = Owner.Center;
			Projectile gauntlet = Main.projectile[(int)Projectile.ai[2]];
			if (gauntlet.active && gauntlet.ModProjectile is GuardianGauntletAnchor && gauntlet.owner == Owner.whoAmI)
				target = gauntlet.Center;
			else if (IsLocalOwner) 
				Projectile.Kill();

			if (QuickPull)
			{ // this field exists for netsync purposes
				QuickPull = false;
				Vector2 velocity = Vector2.Normalize(Projectile.Center - Owner.MountedCenter);
				Owner.velocity = velocity * 15f;
				Vector2 thruPlatforms = Collision.TileCollision(Owner.Center, velocity * 0.05f, Owner.width, Owner.height, true, true, (int)Owner.gravDir);
				if (thruPlatforms.Length() > 0) Owner.position += thruPlatforms;
			}

			if (Projectile.ai[1] >= 0)
			{
				Projectile.tileCollide = false;
				NPC npc = Main.npc[(int)Projectile.ai[1]];
				if (npc.active && !npc.friendly && npc.life > 0)
				{
					float OwnerHitboxArea = Owner.width * (Owner.height + (Owner.mount.Active ? Owner.mount.HeightBoost : 0));
					bool heavyTarget = (npc.knockBackResist <= 0.5f || npc.immortal || (float)(npc.width * npc.height)/OwnerHitboxArea > 16f || npc.boss || (npc.realLife != -1 && Main.npc[npc.realLife].active && Main.npc[npc.realLife].boss));
					if (NPCImpactPoint == Vector2.Zero)
					{
						SoundEngine.PlaySound(SoundID.DD2_JavelinThrowersAttack, Projectile.Center);
						NPCImpactPoint = Projectile.Center - npc.Center;
						NPCImpactVelocity = Projectile.velocity;
						Projectile.timeLeft = 60;
						Projectile.friendly = false;

						if (IsLocalOwner) Main.SetCameraLerp(0.1f, 10);
					}

					Projectile.Center = NPCImpactPoint - NPCImpactVelocity + npc.Center;
					NPCImpactVelocity *= 0.75f;
					Owner.RemoveAllGrapplingHooks();

					if (heavyTarget) 
					{
						Vector2 velocity = Vector2.Normalize(Projectile.Center - Owner.MountedCenter);
						Owner.velocity = velocity * 15f;
						Vector2 thruPlatforms = Collision.TileCollision(Owner.Center, velocity * 0.05f, Owner.width, Owner.height, true, true, (int)Owner.gravDir);
						if (thruPlatforms.Length() > 0) Owner.position += thruPlatforms;
					}
					else npc.velocity = Vector2.Normalize(Owner.MountedCenter - Projectile.Center) * 15f * npc.knockBackResist;

					if (Owner.Center.Distance(Projectile.Center) < 32f)
					{
						Projectile.ai[1] = -1;
						TimeSpent = 35;
						Projectile.velocity = Vector2.Normalize(target - Projectile.Center) * 15f;
						Owner.velocity.X *= 0.5f;
						Owner.velocity.Y *= 0.75f;
					}
				}
				else
				{
					Projectile.ai[1] = -1;
					TimeSpent = 35;
					Projectile.velocity = Vector2.Normalize(target - Projectile.Center) * 15f;
				}
				// if (Collision.CheckAABBvAABBCollision(Owner.position + Owner.velocity - Owner.Hitbox.Size() * 0.5f, Owner.Hitbox.Size() * 2f, npc.position - npc.Hitbox.Size() * 0.5f, npc.Hitbox.Size() * 2f) && !npc.immortal && !Owner.GetModPlayer<OrchidGuardian>().GuardianGauntletParry && Owner.immuneTime == 0) 
				// {
				// 	Owner.GetModPlayer<OrchidPlayer>().PlayerImmunity = 30;
				// 	Owner.immuneTime = 30;
				// 	Owner.immune = true;
				// }
			}
			else if (TimeSpent > 20)
			{
				if (TimeSpent <= 35) Projectile.velocity -= InitialVelocity * (TimeSpent - 20) * 0.02f;
				else
				{
					Projectile.velocity = Vector2.Normalize(target - Projectile.Center) * Projectile.velocity.Length();
					Projectile.tileCollide = false;
				}

				float itemDistance = 32f;
				foreach (Item item in Main.item) {
					if (item.active && item.Center.Distance(Projectile.Center) <= itemDistance)
						item.velocity = Vector2.Normalize(Projectile.Center - item.Center) * 15f;
				}

				if (target.Distance(Projectile.Center) < 32f)
					Projectile.Kill();
				
			}
			else
				Projectile.rotation = Projectile.velocity.ToRotation();
						
		}

		public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
		{
			if (Projectile.timeLeft > 20 && !target.CountsAsACritter)
			{
				if (target.life > 0)
				{
					Projectile.ai[1] = target.whoAmI;
					Projectile.netUpdate = true;
					guardian.AddGuard();
				}
				else if (Owner.HeldItem.ModItem is ThoriumYewGauntlet gauntlet && gauntlet.PullOnKill)
				{
					QuickPull = true;
					Projectile.netUpdate = true;
				}
			}
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


				Projectile.velocity = Vector2.Normalize(target - Projectile.Center) * 15f;
			}

			return false;
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(QuickPull);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			QuickPull = reader.ReadBoolean();
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			SpriteEffects spriteEffects = SpriteEffects.None;
			if (Flip)	spriteEffects = SpriteEffects.FlipVertically;

			if (Owner.HeldItem.ModItem is OrchidModGuardianGauntlet && Owner.HeldItem.type != ModContent.ItemType<ThoriumYewGauntlet>())
			{ // Draw chain between hook and gauntlet
				Vector2 target = Owner.Center;

				foreach(Projectile proj in Main.projectile) 
				if (proj.active && proj.owner == Projectile.owner && proj.ModProjectile is GuardianGauntletAnchor anchor && ((Projectile.ai[0] == 2 && anchor.OffHandGauntlet) || (Projectile.ai[0] == 1 && !anchor.OffHandGauntlet))) 
				{
					target = proj.Center;
					break;
				}
				
				Texture2D chainTexture = ModContent.Request<Texture2D>("OrchidMod/Content/Guardian/Weapons/Gauntlets/ThoriumYewGauntlet_Chain", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
				Vector2 chainDirection = Projectile.Center - (target + Vector2.UnitY * Owner.gfxOffY);
				Vector2 segment = Vector2.Normalize(chainDirection) * chainTexture.Height * 0.66f;

				int nbSegments = 0;

				while(chainDirection.Length() > (segment * nbSegments).Length()) nbSegments++;

				while (nbSegments > 0)
				{
					nbSegments--;
					chainDirection -= segment;
					Vector2 chainPos = target + chainDirection - Main.screenPosition;
					spriteBatch.Draw(chainTexture, chainPos, null, lightColor, 0f, chainTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
				}
			}

			Texture2D projTexture = TextureAssets.Projectile[Projectile.type].Value;
			Vector2 drawPosition = Projectile.Center - Main.screenPosition;
			spriteBatch.Draw(projTexture, drawPosition, null, lightColor, Projectile.rotation, projTexture.Size() * 0.5f, Projectile.scale, spriteEffects, 0f);

			return false;
		}
	}
}