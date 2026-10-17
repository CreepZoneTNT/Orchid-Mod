using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common.Global.NPCs;
using OrchidMod.Common.ModSystems;
using OrchidMod.Content.Guardian.Buffs.Debuffs;
using OrchidMod.Content.Guardian.Misc;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian
{
	internal class GuardianGlobalNPC : GlobalNPC
	{
		public float KatarBleed = 0;
		public int KatarBleedTimer = 0;
		public int StunTimer = -1;
		public float StunKnockdown = 0;
		private static Texture2D StunStarTexture;
		public override bool InstancePerEntity => true;

		public override void Load()
		{
			StunStarTexture ??= ModContent.Request<Texture2D>(ModContent.GetInstance<GreathammerStunDebuff>().Texture + "_Stars", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
		}

		public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
		{
			if (projectile.ModProjectile is OrchidModGuardianAnchor anchor && npc.ModNPC != null)
			{
				npc.ModNPC.OnHitByItem(anchor.Owner, anchor.Owner.HeldItem, hit, damageDone);
			}
		}

		public override void PostAI(NPC npc)
		{
			if (StunTimer >= 0)
			{
				if (npc.HasBuff<GreathammerStunDebuff>())
				{
					if (npc.knockBackResist > 0f)
					{
						float strength = (float)(Math.Log10(npc.knockBackResist + 0.01) * 0.45 + 0.5);
						npc.velocity.X = MathHelper.Lerp(npc.velocity.X, 0, strength);
						npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y - StunKnockdown, 0, strength);
						if (StunKnockdown < 8) StunKnockdown += 0.25f;
						npc.velocity.Y += StunKnockdown;
					}
				}
				else
				{
					StunTimer = -1;
					StunKnockdown = 0;
				}
			}
		}

		public override void UpdateLifeRegen(NPC npc, ref int damage)
		{
			if (KatarBleed > 1)
			{
				if (npc.lifeRegen > 0)
				{
					npc.lifeRegen = 0;
				}

				if (damage < 0)
				{
					damage = 0;
				}

				damage += (int)(KatarBleed * 0.5f);
				npc.lifeRegen -= (int)KatarBleed;

				KatarBleedTimer++;
				if (KatarBleedTimer >= 60)
				{
					KatarBleedTimer = 0;
					KatarBleed = (int)(KatarBleed * 0.5f);
				}
			}
			else
			{
				KatarBleedTimer = 0;
				KatarBleed = 0;
			}
		}
		public override void DrawEffects(NPC npc, ref Color drawColor)
		{
			if (KatarBleed > 0)
			{
				int intensity = (int)(KatarBleed * 1.5f);
				if (intensity > 250) intensity = 250;
				int red = drawColor.R;
				int green = drawColor.G;
				int blue = drawColor.B;
				red += intensity;
				green -= (int)(intensity * 0.5f);
				blue -= (int)(intensity * 0.5f);
				if (red > 255) red = 255;
				if (green < 0) green = 0;
				if (blue < 0) blue = 0;
				Color newColor = new Color(red, green, blue);
				drawColor = newColor.MultiplyRGBA(drawColor);

				if (Main.rand.NextBool((int)(1 + (350 - intensity) * 0.01f)))
				{
					Dust dust = Dust.NewDustDirect(npc.position + new Vector2(4, 4), npc.width - 8, npc.height - 8, DustID.Blood);
					dust.velocity *= 0.75f;
				}
			}
		}

		public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (StunTimer > 0)
			{
				Color color = new Color(191, 191, 191, 63) * Math.Min(StunTimer / 30f, 1f);
				Vector2 origin = npc.Center - Vector2.UnitY * (npc.height / 2 + 10) - screenPos;
				for (int i = 0; i < 3; i++)
				{
					int frame = (StunTimer + i * 24) / 9 % 8;
					SpriteEffects effect = frame > 5 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
					if (frame > 4) frame = 8 - frame;
					Vector2 offs = Vector2.UnitX.RotatedBy(StunTimer * 0.08f - i * 2.0944f) * (10f + npc.width * 0.25f);
					offs.Y *= 0.3f;
					spriteBatch.Draw(StunStarTexture, origin + offs, new Rectangle(12 * frame, 0, 10, 12), color, 0, new Vector2(5, 6), 1, effect, 0);
				}
			}
		}

		public override void OnKill(NPC npc)
		{
			if (npc.GetGlobalNPC<OrchidGlobalNPC>().GuardianHit && !npc.SpawnedFromStatue && OrchidMiscModSystem.SlamDropCooldown >= 300 && !npc.CountsAsACritter)
			{ // Slam pickups drop logic (every 10 sec, not if there are more than 2 nearby slams)
				OrchidMiscModSystem.SlamDropCooldown = 0;
				int slamType = ModContent.ItemType<Slam>();
				int count = 0;
				foreach (Item item in Main.item)
				{
					if (item.type == slamType && item.Center.Distance(npc.Center) < 160f && item.active)
					{
						count++;
						if (count == 3)
						{
							break;
						}
					}
				}

				if (count < 3)
				{
					Item.NewItem(npc.GetSource_Death(), npc.getRect(), slamType);
				}
			}
		}

		public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
		{
			if (StunTimer > 0) return false;
			return base.CanHitPlayer(npc, target, ref cooldownSlot);
		}
	}
}
