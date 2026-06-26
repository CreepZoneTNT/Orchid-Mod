using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common;
using OrchidMod.Content.General.Prefixes;
using OrchidMod.Content.Guardian.Projectiles.Shields;
using OrchidMod.Content.Guardian.Weapons.Misc;
using OrchidMod.Utilities;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using ThoriumMod.Tiles;
using static Terraria.Player;

namespace OrchidMod.Content.Guardian.Projectiles.Misc;

public class GuardianLanternShieldAnchor : OrchidModGuardianParryAnchor
{
	public int LockedOwnerDir = 0;
	public bool Ding = false;
	public bool NeedNetUpdate = false;
	public float SlamTime = 0;

	public int SelectedItem { get; set; } = -1;
	
	public Item GuardianItem => Main.player[Projectile.owner].inventory[SelectedItem];

	
	public Vector3 TorchColor;
	
	public bool Blocking => Projectile.ai[1] > 1;
	public bool Slamming => Projectile.ai[1] < -1;
	public bool Charging => MathF.Abs(Projectile.ai[1]) == 1f;

	public Texture2D ItemTexture;
	
	bool shieldEffectReady = true;
	public Vector2 oldOwnerPos = Vector2.Zero;

	public override void SafeSetDefaults()
	{
		Projectile.width = 16;
		Projectile.height = 16;
		Projectile.friendly = false;
		Projectile.tileCollide = false;
		Projectile.aiStyle = 0;
		Projectile.timeLeft = 60;
		Projectile.penetrate = -1;
		Projectile.netImportant = true;
		Projectile.alpha = 255;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 20;
		Projectile.netImportant = true;
	}
	
	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
	{
		overPlayers.Add(index);
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(SelectedItem);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		SelectedItem = reader.ReadInt32();
	}

	public void OnChangeSelectedItem(Player owner)
	{
		owner.GetModPlayer<OrchidGuardian>().GuardianItemCharge = 0;
		SelectedItem = owner.selectedItem;
		
		Projectile.ai[0] = 0f;
		Projectile.ai[1] = 0f;
		Projectile.ai[2] = 0f;
		Projectile.localAI[1] = 0;
		Projectile.netUpdate = true;
		
		if (GuardianItem.ModItem is GuardianLanternShield guardianItem)
		{
			ItemTexture = ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
			ResetSize();
		}
		else if (IsLocalOwner)
		{
			Projectile.Kill();
		}
	}

	public override void AI()
	{
		// ai[0] controls block duration
		// ai[1] controls charge/attack status
		// (
		// ai[2] is networked rotation

		Player owner = Main.player[Projectile.owner];
		OrchidGuardian guardian = owner.Guardian();

		if (!owner.active || owner.dead || SelectedItem < 0 || owner.HeldItem.ModItem is not GuardianLanternShield || GuardianItem == null || GuardianItem.ModItem is not GuardianLanternShield guardianItem)
		{
			if (IsLocalOwner) Projectile.Kill();
			return;
		}
		else
		{
			if (NeedNetUpdate)
			{
				NeedNetUpdate = false;
				Projectile.netUpdate = true;
			}

			Projectile.timeLeft = 5;
			
			if (IsLocalOwner) // Offhand is always loaded first; no need to do that twice
			{
				if (Projectile.ai[1] >= 0)
				{ // Lock the player direction while slamming
					if (Main.MouseWorld.X > owner.Center.X && owner.direction != 1) owner.ChangeDir(1);
					else if (Main.MouseWorld.X < owner.Center.X && owner.direction != -1) owner.ChangeDir(-1);
					LockedOwnerDir = owner.direction;
				}
				else owner.direction = LockedOwnerDir;
			}

			bool blockInput = Main.mouseRight;
			bool punchInput = Main.mouseLeft;

			if (ModContent.GetInstance<OrchidClientConfig>().GuardianSwapGauntletImputs)
			{
				blockInput = Main.mouseLeft;
				punchInput = Main.mouseRight;
			}

			Vector2 flamePoint = Projectile.Center + (Vector2.UnitY * 4 * Projectile.scale).RotatedBy(Projectile.rotation);
			if (guardianItem.TorchIndex != -1)
			{
				Item torchItem = owner.inventory[guardianItem.TorchIndex];
				if (ItemID.Sets.Torches[torchItem.type] && TileID.Sets.Torch[torchItem.createTile])
				{
					TorchColor = new Vector3(0, 0, 0);
					if (torchItem.type < ItemID.Count && torchItem.createTile == TileID.Torches)
					{
						TorchID.TorchColor(torchItem.placeStyle, out float r, out float g, out float b);
						TorchColor = new Vector3(r, g, b);
					}
					else
					{
						(float r, float g, float b) = (0f, 0f, 0f);
						TileLoader.GetTile(torchItem.createTile)?.ModifyLight((int)(flamePoint.X / 16f), (int)(flamePoint.Y / 16f), ref r, ref g, ref b);
						TorchColor = new Vector3(r, g, b);
					}
					
					if (TorchColor != Vector3.Zero) Lighting.AddLight((int)(flamePoint.X / 16f), (int)(flamePoint.Y / 16f), TorchColor.X, TorchColor.Y, TorchColor.Z);

					Tile tile = Framing.GetTileSafely(flamePoint);
					if (ItemID.Sets.WaterTorches[torchItem.type] || !owner.wet || (tile.LiquidAmount < (flamePoint.Y + owner.gfxOffY) % 16 * 16 && !tile.HasUnactuatedTile))
					{
						bool bigAttack = Projectile.ai[0] < 0 || (Projectile.ai[0] > 0 && Projectile.ai[1] == 3f);
						if (Main.rand.NextBool(bigAttack ? 1 : 3))
						{
							Dust dust = Dust.NewDustDirect(flamePoint - new Vector2(8), 12, 12, torchItem.createTile == TileID.Torches ? TorchID.Dust[torchItem.placeStyle] : TileLoader.GetTile(torchItem.createTile).DustType, Scale: Main.rand.NextFloat(0.5f, 1f), SpeedY: -Main.rand.NextFloat(3f));
							switch (Main.rand.Next(10))
							{
								default:
									dust.velocity *= 0.25f;
									dust.velocity += owner.velocity * 0.5f;
									dust.scale *= 2.5f;
									goto case 8;
								case 6:
								case 7:
								case 8:
									dust.noGravity = true;
									dust.velocity *= 0.8f;
									if (bigAttack)
									{
										if (Projectile.ai[0] < 0) //swing
											dust.velocity += new Vector2(-owner.direction * (float)Math.Cos(-Projectile.ai[0] * 0.2f), -1).RotatedBy(Projectile.rotation + MathHelper.PiOver4) * Main.rand.NextFloat(4f, 8f);
										else //counter
											dust.velocity += new Vector2(1 * owner.direction, -1).RotatedBy(Projectile.rotation + Main.rand.NextFloat(MathHelper.PiOver2)) * Main.rand.NextFloat(8f);
										if (Main.rand.NextBool())
										{
											dust.scale += Main.rand.NextFloat(2f);
											dust.velocity *= Main.rand.NextFloat(0.2f, 0.6f);
										}
										dust.fadeIn += Main.rand.NextFloat(2.5f);
									}
									break;
								case 9:
									dust.scale *= Main.rand.NextFloat(0.5f, 1f);
									break;
							}
						}
					}
				}
			}

			if (Blocking)
			{
				
				Projectile.Center = owner.MountedCenter.Floor() + Vector2.UnitX * 4 * owner.direction;
				Projectile.rotation = 0f;

				Projectile.ai[0]--;

				if (Projectile.ai[1] == 3f) // Blocking
				{
					Vector2 HitboxOrigin = Projectile.Center + Vector2.UnitX * 4 * owner.direction + Vector2.UnitY * (Projectile.gfxOffY - Projectile.height * 0.5f) - new Vector2(4f);

					Vector2 Hitbox = (Vector2.UnitY * Projectile.height);

					Point p1 = new Point((int)HitboxOrigin.X, (int)HitboxOrigin.Y);

					Point p2 = new Point((int)(HitboxOrigin.X + Hitbox.X), (int)(HitboxOrigin.Y + Hitbox.Y));

					for (int l = 0; l < Main.projectile.Length; l++)
					{
						Projectile proj = Main.projectile[l];
						if (proj.active && proj.hostile && proj.damage > 0 && !OrchidGuardian.ProjectilesBlockBlacklist.Contains(proj.type))
						{
							if (GuardianShieldAnchor.LineIntersectsRect(p1, p2, proj.Hitbox) || proj.Hitbox.Intersects(Projectile.Hitbox))
							{
								guardian.OnBlockProjectile(Projectile, proj);
								if (shieldEffectReady)
								{
									guardian.OnBlockProjectileFirst(Projectile, proj);
									shieldEffectReady = false;
									SoundEngine.PlaySound(SoundID.Item37.WithPitchOffset(Main.rand.NextFloat(0.4f, 0.6f)), owner.MountedCenter);
								}
								proj.Kill();
								SoundEngine.PlaySound(SoundID.Dig, owner.MountedCenter);
							}
						}
					}

					for (int k = 0; k < Main.maxNPCs; k++)
					{
						NPC target = Main.npc[k];
						if (target.active && !target.dontTakeDamage && !target.friendly && GuardianShieldAnchor.LineIntersectsRect(p2, p1, target.Hitbox))
						{
							bool contained = false;
							foreach (BlockedEnemy blockedEnemy in guardian.GuardianBlockedEnemies)
							{
								if (blockedEnemy.npc == target)
								{ // Enemy already blocked, reset the timer
									blockedEnemy.time = (int)Projectile.ai[0] + 60;
									contained = true;
									break;
								}
							}

							if (!contained)
							{ // First time blocking an enemy
								guardian.OnBlockNPCNew(Projectile, target);
								guardian.GuardianBlockedEnemies.Add(new BlockedEnemy(target, (int)Projectile.ai[0] + 60));
								if (guardianItem.TorchIndex != -1) 
									target.AddBuff(guardianItem.TorchTypeDebuff(owner.inventory[guardianItem.TorchIndex].type), 120);
								SoundEngine.PlaySound(SoundID.Dig, owner.MountedCenter);
							}

							if (target.knockBackResist > 0f)
							{ // Push enemy if possible
								Vector2 push = Projectile.Center - owner.MountedCenter;
								push.Normalize();
								push += owner.MountedCenter - oldOwnerPos;
								target.velocity = push;
							}

							guardian.OnBlockNPC(Projectile, target);
							if (shieldEffectReady)
							{ // First parry stuff
								guardian.OnBlockNPCFirst(Projectile, target);
								shieldEffectReady = false;
								SoundEngine.PlaySound(SoundID.Item37.WithPitchOffset(Main.rand.NextFloat(0.4f, 0.6f)), owner.MountedCenter);
							}
						}
					}
						
					if (guardian.GuardianShowDebugVisuals)
					{

						Vector2 vector = Hitbox;
						vector.Normalize();
						for (int i = 0; i < Hitbox.Length(); i++)
						{
							Vector2 pos = HitboxOrigin + vector * i;
							Dust dust = Main.dust[Dust.NewDust(pos, 0, 0, DustID.Torch)];
							dust.velocity *= 0f;
							dust.noGravity = true;
						}
					}
				}
				else if (Projectile.ai[1] == 2f) // Parry
				{
					guardian.GuardianParry = true;
					guardian.GuardianParryBuffer = true;

					if (owner.immune)
					{
						if (owner.eocHit != -1 && owner.eocDash > 0)
							guardian.DoParryItemParry(Main.npc[owner.eocHit]);
						else
						{
							Projectile.ai[0] = 0f;
							//refund remaining duration as guards if interrupted by owner becoming immune from another source
							guardian.GuardianGuardRecharging += Projectile.ai[0] / (guardianItem.ParryDuration * guardianItem.Item.GetGlobalItem<GuardianPrefixItem>().GetBlockDuration() * guardian.GuardianParryDuration);
							Rectangle rect = owner.Hitbox;
							rect.Y -= 64;
							CombatText.NewText(guardian.Player.Hitbox, Color.LightGray, Language.GetTextValue("Mods.OrchidMod.UI.GuardianItem.Interrupted"), false, true);
						}
					}
				}
				
				if (Projectile.ai[0] <= 0f)
				{
					Projectile.ai[0] = 0f;
					Projectile.ai[1] = blockInput ? 1f : punchInput ? -1f : 0f;
				}
			}
			else if (Slamming)
			{
				if (Projectile.ai[0] == 0f) // Register base slam length
				{
					SlamTime = 35f / owner.GetAttackSpeed<MeleeDamageClass>();
					Projectile.ai[0] = -SlamTime;
					guardian.GauntletPunchCooldown = (int)SlamTime / 2 - 1;
				}
				float animTime = -Projectile.ai[0] / SlamTime;
				float addedDistance = MathF.Sin((animTime - 0.33f) * ((1 - animTime) * 5.5f - 4.4f) - 0.2f) * -animTime * 20f;
				Projectile.Center = owner.MountedCenter.Floor() + new Vector2(4 * owner.direction, 0) + Vector2.UnitY.RotatedBy(Projectile.ai[2]) * addedDistance;
				
				if (!IsLocalOwner)
				{ // Rotates the player in the direction of the punch for other clients
					Vector2 puchDir = (Projectile.ai[2] + MathHelper.PiOver2).ToRotationVector2();
					if (puchDir.X > 0 && owner.direction != 1) owner.ChangeDir(1);
					else if (puchDir.X < 0 && owner.direction != -1) owner.ChangeDir(-1);
				}
				else if (-Projectile.ai[0] == SlamTime)
				{ // Slam just started, make projectile
					int damage = guardian.GetGuardianDamage(guardianItem.Item.damage);
					
					if (owner.boneGloveItem != null && !owner.boneGloveItem.IsAir && owner.boneGloveTimer == 0)
					{ // Bone glove compatibility, from vanilla code
						owner.boneGloveTimer = 60;
						Vector2 center = owner.Center;
						Vector2 vector = owner.DirectionTo(owner.ApplyRangeCompensation(0.2f, center, Main.MouseWorld)) * 10f;
						Projectile.NewProjectile(owner.GetSource_ItemUse(owner.boneGloveItem), center.X, center.Y, vector.X, vector.Y, ProjectileID.BoneGloveProj, 25, 5f, owner.whoAmI);
					}

					int projectileType = ModContent.ProjectileType<NightShieldProjAlt>();
					float strikeVelocity = guardianItem.StrikeVelocity * guardianItem.Item.GetGlobalItem<GuardianPrefixItem>().GetSlamDistance() * owner.GetTotalAttackSpeed(DamageClass.Melee);
					Vector2 velocity = Vector2.UnitY.RotatedBy((Main.MouseWorld - owner.MountedCenter).ToRotation() - MathHelper.PiOver2) * strikeVelocity * 0.25f;
					Projectile punchProj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, velocity, projectileType, guardian.GetGuardianDamage(GuardianItem.damage), 1f, owner.whoAmI);
					
					Ding = false;
					guardianItem.PlayPunchSound(owner, guardian, Projectile);
				}
				
				if (Projectile.ai[2] < 1f && Projectile.ai[2] > -1f)
				{ // Offset the gauntlet when aiming down
					int offset = 2;
					if (Projectile.ai[2] < 0.7f && Projectile.ai[2] > -0.7f) offset += 2;
					if (Projectile.ai[2] < 0.4f && Projectile.ai[2] > -0.4f) offset += 2;
					Projectile.position.Y += offset;
					Projectile.position.X -= offset * owner.direction;
				}

				Projectile.rotation = Projectile.ai[2];
				if (owner.direction == 1) Projectile.rotation += MathHelper.Pi;

				Projectile.ai[0]++;
				if (Projectile.ai[0] >= 0)
				{
					Projectile.ai[0] = 0f;
					Projectile.ai[1] = blockInput ? 1f : punchInput ? -1f : 0f;
					Projectile.ai[2] = 0f;

					if (owner.direction == -1) Projectile.rotation += MathHelper.Pi;
				}
			}
			else
			{
				if (Charging)
				{
					guardian.GuardianItemCharge += 30f / GuardianItem.useTime * (owner.GetTotalAttackSpeed(DamageClass.Melee) * 2f - 1f);
					if (guardian.GuardianItemCharge > 180f)
					{
						if (!Ding && IsLocalOwner)
						{
							if (ModContent.GetInstance<OrchidClientConfig>().GuardianAltChargeSounds) SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, owner.Center);
							else SoundEngine.PlaySound(SoundID.MaxMana, owner.Center);
							Ding = true;
						}

						guardian.GuardianItemCharge = 180f;
					}

					if (owner.whoAmI == Main.myPlayer) 
					{
						if (Projectile.ai[1] == 1f && !blockInput)
						{
							guardian.GuardianItemCharge = 0;
							
							float blockTime = guardianItem.ParryDuration * GuardianItem.GetGlobalItem<GuardianPrefixItem>().GetBlockDuration() * guardian.GuardianBlockDuration;
							if (guardian.GuardianItemCharge >= 180f)
							{
								blockTime *= guardianItem.BlockDurationMult;

								SoundEngine.PlaySound(SoundID.Item73);
								guardian.AddGuard();
								Projectile.ai[1] = 3f;
							}
							else if (guardian.UseGuard(1, true))
							{
								owner.immuneTime = 0;
								guardian.modPlayer.PlayerImmunity = 0;
								owner.immune = false;
								guardian.GuardianParry = true; //remind the player that they are in fact parrying because the projectile ai runs on a slight delay
								guardian.UseGuard();
								Projectile.ai[1] = 2f;
							}
							
							guardianItem.PlayGuardSound(owner, guardian, Projectile);
									
							Projectile.ai[0] = (int)blockTime;
							NeedNetUpdate = true;
							
						}
						else if (Projectile.ai[1] == -1f && !punchInput)
						{
							guardian.GuardianItemCharge = 0;

							if (IsLocalOwner)
							{
								Projectile.ai[0] = 0f;
								Projectile.ai[1] = -2f;
								Projectile.ai[2] = Vector2.Normalize(Main.MouseWorld - owner.MountedCenter).ToRotation() - MathHelper.PiOver2;
								Projectile.netUpdate = true;
							}
						}
						else
						{
							Projectile.Center = owner.MountedCenter.Floor() + new Vector2(-(4 + guardian.GuardianItemCharge * 0.033f) * owner.direction, 4);
							Projectile.rotation = MathHelper.PiOver2;
						}
					}
				}
				else
				{
					Projectile.Center = owner.MountedCenter.Floor() + new Vector2((-6 + guardian.GuardianItemCharge * 0.01f) * owner.direction, 6);

					if (owner.velocity.X != 0)
					{
						Projectile.position.X -= 2 * owner.direction;
						Projectile.position.Y -= 2;
						Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4 * owner.direction * 0.5f;
					}
					else
					{
						Projectile.rotation = MathHelper.Pi - MathHelper.PiOver4 * owner.direction;
					}

				}
			}
		}
		
		 // Composite arm stuff for the front arm (the back arm is disabled while holding gauntlets)
		float rotation = (Projectile.Center + new Vector2(6 * owner.direction, Slamming ? 2 : Charging ? 8 : 6) - owner.MountedCenter.Floor()).ToRotation();
		Player.CompositeArmStretchAmount compositeArmStretchAmount = CompositeArmStretchAmount.ThreeQuarters; // Tweak the arm based on punch direction if necessary
		if (Charging) compositeArmStretchAmount = CompositeArmStretchAmount.Quarter;
		if (Projectile.ai[0] < -0.55f && (Projectile.ai[2] > -2.25f || Projectile.ai[2] < -4f)) compositeArmStretchAmount = CompositeArmStretchAmount.Full;
		owner.SetCompositeArmFront(true, compositeArmStretchAmount, rotation - MathHelper.PiOver2);
		
		
		oldOwnerPos = owner.MountedCenter;
	}
	
	
	public void ResetSize()
	{
		int length = (int)Math.Sqrt(2 * (ItemTexture.Width * GuardianItem.scale * ItemTexture.Width * GuardianItem.scale));
		Projectile.width = length + 4;
		Projectile.height = length + 4;
		Projectile.scale = GuardianItem.scale;
	}
	
	public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
	{
		if (SelectedItem < 0 || SelectedItem > 58) return false;
		if (GuardianItem.ModItem is not GuardianLanternShield) return false;
		if (ItemTexture == null) return false;

		Player player = Main.player[Projectile.owner];
		OrchidGuardian guardian = player.GetModPlayer<OrchidGuardian>();
		Color color = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f), Color.White);
		
		var effect = SpriteEffects.None;
		if (player.direction != 1)
		{
			if (player.velocity.X != 0 && !Blocking || (player.GetModPlayer<OrchidGuardian>().GuardianItemCharge > 0 && Projectile.ai[1] != 0) || Slamming) effect = SpriteEffects.FlipVertically;
			else effect = SpriteEffects.FlipHorizontally;
		}

		float drawRotation = Projectile.rotation;
		Vector2 posproj = Projectile.Center;
		if (player.gravDir == -1)
		{
			drawRotation = -drawRotation;
			posproj.Y = (player.Bottom + player.position).Y - posproj.Y + (posproj.Y - player.Center.Y) * 2f;
			if (effect == SpriteEffects.FlipVertically)
			{
				effect = SpriteEffects.None;
			}
			else if (effect == SpriteEffects.FlipHorizontally)
			{
				effect = SpriteEffects.None;
				drawRotation += MathHelper.Pi;
			}
			else if (effect == SpriteEffects.None)
			{
				effect = SpriteEffects.FlipVertically;
			}
		}

		var drawPosition = Vector2.Transform(posproj - Main.screenPosition + Vector2.UnitY * player.gfxOffY, Main.GameViewMatrix.EffectMatrix);
		float rotation = Projectile.rotation;
		spriteBatch.Draw(ItemTexture, drawPosition, null, color, drawRotation, ItemTexture.Size() * 0.5f, Projectile.scale, effect, 0f);
		
		Vector2 flamePoint = Projectile.Center - Vector2.UnitX.RotatedBy(drawRotation + MathHelper.Pi * 0.75f) * 4 * Projectile.scale;
		if (GuardianItem.ModItem is GuardianLanternShield guardianItem && guardianItem.TorchIndex != -1)
		{
			Item torchItem = Owner.inventory[guardianItem.TorchIndex];
			if (ItemID.Sets.Torches[torchItem.type] && TileID.Sets.Torch[torchItem.createTile])
			{
				Texture2D flameTexture = null;
				Rectangle frame = default;
				if (torchItem.type < ItemID.Count && torchItem.createTile == TileID.Torches)
				{
					flameTexture = TextureAssets.Flames[0].Value;
					frame = flameTexture.Frame(6, 24, 1, torchItem.placeStyle);
				}
				else
				{
					ModTile torchTile = TileLoader.GetTile(torchItem.createTile);
					flameTexture = ModContent.Request<Texture2D>(torchTile.Texture + "_Flame").Value;
					if (flameTexture == null) flameTexture = ModContent.Request<Texture2D>(torchTile.Texture + "Flame").Value;

					frame = flameTexture.Frame(6, 1, 1, 0);
				}
				
				Tile tile = Framing.GetTileSafely(flamePoint);
				if (ItemID.Sets.WaterTorches[torchItem.type] || !Owner.wet || (tile.LiquidAmount < (flamePoint.Y + Owner.gfxOffY) % 16 * 16 && !tile.HasUnactuatedTile))
				{
					for (int k = 0; k < 5; k++)
					{
						Main.spriteBatch.Draw(flameTexture, flamePoint + Main.rand.NextVector2Square(-1.5f, 1.5f) + new Vector2(0, player.gfxOffY) - Main.screenPosition, frame, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
					}
				}
			}
		}

		return false;
	}
}