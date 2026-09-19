using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Assets;
using OrchidMod.Utilities;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace OrchidMod.Content.Guardian.Projectiles.Quarterstaves
{
	public class ThoriumNagaQuarterstaffProjectile : OrchidModGuardianProjectile
	{
		private static Texture2D TextureBurst;
		public int TimeSpent = 0;
		public float Expand = 0f;

		public List<int> EnemiesHitByPop;
		
		public override void SafeSetDefaults()
		{
			Projectile.width = 36;
			Projectile.height = 36;
			Projectile.timeLeft = 900;
			Projectile.scale = 1f;
			Projectile.penetrate = -1;
			Projectile.alpha = 255;
			Projectile.friendly = true;
			Projectile.usesIDStaticNPCImmunity = true;
			Projectile.idStaticNPCHitCooldown = 10;
			Projectile.tileCollide = true;

			EnemiesHitByPop = [];
			
			TextureBurst ??= ModContent.Request<Texture2D>(OrchidAssets.MiscPath + "Extra_9", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
		}

		public override void OnSpawn(IEntitySource source)
		{
			Projectile.scale = 0f;
			// Projectile.originalDamage = Projectile.damage;
			// Projectile.damage = (int)(Projectile.originalDamage * 0.1f);
			if (Main.player[Projectile.owner].ownedProjectileCounts[Type] >= 10)
			{
				Projectile oldest = null;
				int maxTimeSpent = 0;
				foreach (Projectile bable in Main.ActiveProjectiles)
				{
					if (bable.type == Type && bable.owner == Projectile.owner && bable.whoAmI != Projectile.whoAmI && bable.ModProjectile is ThoriumNagaQuarterstaffProjectile bubble && bubble.TimeSpent > maxTimeSpent)
					{
						oldest = bable;
						maxTimeSpent = bubble.TimeSpent;
					}
				}
				oldest?.Kill();
			}
		}

		public override void AI()
		{
			TimeSpent++;

			if (Projectile.scale < 1.5f)
			{
				Vector2 oldCenter = Projectile.Center;
				Projectile.scale += 0.05f;
				Projectile.width = (int)(36 * Projectile.scale);
				Projectile.height = (int)(36 * Projectile.scale);
				Projectile.Center = oldCenter;
			}

			Projectile.velocity *= 0.95f;
			if (Projectile.velocity.Length() < 0.1f)
			{
				Projectile.velocity = Vector2.Zero;
				Projectile.Center += Vector2.UnitY * MathF.Sin(TimeSpent * MathHelper.Pi / 135f) * 0.1f;
			}

			Projectile.ai[0] = 15f + MathF.Sin(TimeSpent * MathHelper.Pi / 180f) * 3f;
			Projectile.ai[1] = 15f + MathF.Sin((TimeSpent + 90) * MathHelper.Pi / 180f) * 3f;

			Projectile.rotation = 0.1f * MathF.Sin(TimeSpent * MathHelper.Pi / 150f);

			foreach (Projectile beble in Main.ActiveProjectiles)
			{
				if (beble.type == Type && (beble.owner == Projectile.owner || Main.player[beble.owner].team == Owner.team) && beble.identity != Projectile.identity && (beble.Center - Projectile.Center).Length() <= 18f * (Projectile.scale + beble.scale))
				{
					beble.velocity -= beble.DirectionTo(Projectile.Center) * beble.Distance(Projectile.Center) * 0.25f;
					Projectile.velocity -= Projectile.DirectionTo(beble.Center) * Projectile.Distance(beble.Center) * 0.25f;
					SoundEngine.PlaySound(SoundID.Item154, Projectile.Center);
					Projectile.netUpdate = true;
					beble.netUpdate = true;
				}
			}
			
			Lighting.AddLight(Projectile.Center, GetOwnerColor(Projectile.owner).ToVector3() * 0.1f);

			if (Projectile.timeLeft <= 10 && Projectile.ai[2] == 1)
			{
				if (Projectile.timeLeft == 10)
				{
					SoundEngine.PlaySound(SoundID.Item21, Projectile.Center);
					SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
					bool ccw = Main.rand.NextBool();
					float dustRot = Main.rand.NextFloat(MathHelper.TwoPi);
					for (int i = 0; i < 3; i++)
					{
						for (int j = 40; j > 0; j--)
						{
							Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB, newColor: GetOwnerColor(Projectile.owner));
							dust.velocity = Vector2.UnitX.RotatedBy(dustRot + MathHelper.TwoPi * i / 3f) * (j * 0.16f + 1.25f) * Projectile.scale;
							dust.scale *= 3f - j * 0.05f;
							dust.noGravity = true;
							if (ccw) dustRot -= 0.2f - j * 0.0015f;
							else dustRot += 0.2f - j * 0.0015f;
							dust.alpha = 127;
						}
					}
					
					for (int i = 0; i < (10); i++)
						Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, Main.rand.NextVector2Unit() * Main.rand.NextFloat(Projectile.ai[2] == 1 ? 32f : 8f), newColor: GetOwnerColor(Projectile.owner), Scale: Main.rand.NextFloat(0.5f, 1f))
							.noGravity = true;

					SoundEngine.PlaySound(SoundID.Item54, Projectile.Center);
					Projectile.netUpdate = true;
					
				}
				
				Expand += 0.75f * MathF.Pow(0.95f, Projectile.timeLeft - 10);
				if (Expand > 5f)
					Expand = 5f;
				Projectile.ResetImmunity();
				
				foreach (Projectile boble in Main.ActiveProjectiles)
				{
					if (boble.type == Type && boble.ai[2] == 0 && (boble.owner == Projectile.owner || Main.player[boble.owner].team == Owner.team) && boble.identity != Projectile.identity && (boble.Center - Projectile.Center).Length() <= 18f * (Projectile.scale * (1 + Expand) + boble.scale))
					{
						boble.ai[2] = 1;
						boble.timeLeft = 10;
						boble.netUpdate = true;
					}
				}

				Projectile.velocity *= float.Epsilon;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			Bounce(oldVelocity, 0.95f);
			Projectile.netUpdate = true;
			return false;
		}

		public override bool? CanHitNPC(NPC target)
		{
			float radius = 18.5f * MathF.Sqrt(2) * Projectile.scale;
			if (Projectile.ai[2] == 1) radius *= 6f;
			return IsValidTarget(target) && target.Distance(Projectile.Center) <= radius && !EnemiesHitByPop.Contains(target.whoAmI);
		}

		public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
		{
			if (Projectile.timeLeft <= 10 && Projectile.ai[2] == 1)
				EnemiesHitByPop.Add(target.whoAmI);
		}

		public override void SafeModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (Projectile.ai[2] != 1)
			{
				modifiers.FinalDamage *= Utils.Remap(Projectile.velocity.Length(), 0f, 15f, 0.05f, 0.75f);
				modifiers.ArmorPenetration += 5f;
			}
			else
			{
				modifiers.FinalDamage *= Utils.Remap(Expand, 0, 6, 2f, 3f);
				if (target.aiStyle is NPCAIStyleID.Worm or NPCAIStyleID.TheDestroyer && Projectile.ai[0] == 0f && target.type != NPCID.SolarCrawltipedeTail && target.type != NPCID.StardustWormHead)
				{
					modifiers.FinalDamage *= 0.25f;
					int clumpCount = 0;
					foreach (NPC worm in Main.ActiveNPCs)
					{
						if (worm.type == target.type && worm.realLife == target.realLife && target.realLife != -1 && worm.Distance(Projectile.Center) <= 18f * Expand)
							clumpCount++;
					}

					modifiers.FinalDamage /= clumpCount;
				}
			}
		}

		public override void ModifyDamageHitbox(ref Rectangle hitbox)
		{
			if (Projectile.ai[2] != 1) return;
			
			int size = 90;
			hitbox.X -= size;
			hitbox.Y -= size;
			hitbox.Width += size * 2;
			hitbox.Height += size * 2;
		}

		public override void OnKill(int timeLeft)
		{
			if (timeLeft == 0)
			{
				for (int i = 0; i < (10); i++)
					Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, Main.rand.NextVector2Unit() * Main.rand.NextFloat(Projectile.ai[2] == 1 ? 32f : 8f), newColor: GetOwnerColor(Projectile.owner), Scale: Main.rand.NextFloat(0.5f, 1f))
						.noGravity = true;

				SoundEngine.PlaySound(SoundID.Item54, Projectile.Center);
			}
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			if (Projectile.ai[2] == 1)
			{
				spriteBatch.End(out SpriteBatchSnapshot burstSnapshot);
				spriteBatch.Begin(burstSnapshot with {BlendState = BlendState.Additive});
				Main.EntitySpriteDraw(TextureBurst, Projectile.Center - Main.screenPosition, null, GetOwnerColor(Projectile.owner) * (0.75f + 0.55f * Projectile.timeLeft / 10f), Projectile.rotation, TextureBurst.Size() * 0.5f, Projectile.scale * (1 + Expand) * 0.04f, SpriteEffects.None);

				spriteBatch.End();
				spriteBatch.Begin(burstSnapshot);
				return false;
			}
			
			spriteBatch.End(out SpriteBatchSnapshot snapshot);
			spriteBatch.Begin(snapshot with {BlendState = BlendState.Additive});
			
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			if (Main.netMode != NetmodeID.SinglePlayer && Owner.team != 0)
			{
				Texture2D outline = ModContent.Request<Texture2D>(Texture + "_Outline").Value;
				Main.EntitySpriteDraw(outline, Projectile.Center - Main.screenPosition, null, Main.teamColor[Owner.team], Projectile.rotation, outline.Size() * 0.5f, Projectile.scale * new Vector2(Projectile.ai[0] / 15f, Projectile.ai[1] / 15f), SpriteEffects.None, 0f);
			}

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Lighting.GetColor(Projectile.Center.ToTileCoordinates(), GetOwnerColor(Projectile.owner)) * 1.25f, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * new Vector2(Projectile.ai[0] / 15f, Projectile.ai[1] / 15f), SpriteEffects.None);
			
			spriteBatch.End();
			spriteBatch.Begin(snapshot);
			
			return false;
		}

		public static Color GetOwnerColor(int whoAmI)
		{
			Color playerColor = new (83, 128, 128);
			
			if (Main.netMode != NetmodeID.SinglePlayer)
			{
				Player player = Main.player[whoAmI];
				byte[] bytes = Encoding.UTF8.GetBytes(player.name);
				int total = 0;
				for (int i = 0; i < bytes.Length; i++)
					total += bytes[i] * (int)MathF.Pow(256, i);
				total = (int)(total % MathF.Pow(256, bytes.Length));
				
				Main.rand.SetSeed(total);
				playerColor = new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256), 255);
		
				if (player.team != 0)
					playerColor = Color.Lerp(playerColor, Main.teamColor[player.team], 0.5f);
			}
			
		
			return playerColor;
		}
	}
}