using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OrchidMod.Common;
using OrchidMod.Content.General.Prefixes;
using OrchidMod.Content.Guardian.Weapons.Warhammers;
using OrchidMod.Utilities;
using ReLogic.Content;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Projectiles.Warhammers
{

    // this is basically just GuardianWarhammerAnchor.cs, just with some stuff tweaked to act as a rear warhammer
    // ex. the ToyWarhammers cannot block, so there's no use having the blocking code
	public class ToyWarhammerProjectile : OrchidModGuardianAnchor
	{

        public override string Texture => "OrchidMod/Assets/Textures/Misc/Invisible";

		public List<Vector2> OldPosition;
		public List<float> OldRotation;

		public ToyWarhammers HammerItem;
		public Texture2D HammerTexture;

		public int range = 0;
		public int HitCount = 0;
		public bool penetrate;
		public bool WeakHit = false;
		public bool NeedNetUpdate = false;
		public int hitboxOffset;


		public bool Ding = false;

		public bool WeakThrow => Projectile.ai[0] == 1;

		public override void SafeSetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = false;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
			Projectile.scale = 1f;
			Projectile.timeLeft = 600;
			Projectile.alpha = 255;
			Projectile.tileCollide = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;

			OldPosition = [];
			OldRotation = [];
		}

		public override void OnSpawn(IEntitySource source)
		{
			Player player = Main.player[Projectile.owner];
			OrchidGuardian guardian = player.GetModPlayer<OrchidGuardian>();
			Item item = player.inventory[player.selectedItem];

			if (item == null || item.ModItem is not ToyWarhammers hammerItem)
			{
				if (Projectile.owner == Main.myPlayer) Projectile.Kill();
			}
			else
			{
				HammerItem = hammerItem;
				HammerTexture = TextureAssets.Item[hammerItem.Item.type].Value;
				hitboxOffset = (int)(HammerTexture.Width * guardian.GuardianWeaponScale * hammerItem.Item.scale / 2f);
				DrawOriginOffsetX = DrawOriginOffsetY = hitboxOffset;

				if (HammerItem.hasSpecialHammerTexture) HammerTexture = ModContent.Request<Texture2D>(hammerItem.HammerTexture, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

				range = HammerItem.Range;
				penetrate = HammerItem.Penetrate;
				Projectile.netUpdate = true;
				Projectile.localNPCHitCooldown = hammerItem.HitCooldown;
			}
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			OrchidGuardian guardian = owner.GetModPlayer<OrchidGuardian>();

			if (HammerItem != null)
			{
				Projectile.scale = HammerItem.Item.scale * guardian.GuardianWeaponScale;
				if (IsLocalOwner)
				{ // OnSpawn() is called too early, guardian.GuardianWeaponScale is always equal to 1f
					hitboxOffset = (int)(HammerTexture.Width * guardian.GuardianWeaponScale * HammerItem.Item.scale / 2f);
				}

				if (NeedNetUpdate)
				{
					NeedNetUpdate = false;
					Projectile.netUpdate = true;
				}

				
				if (Projectile.ai[1] <= 0) // Held
				{
					if (owner.dead || owner.HeldItem.ModItem is not ToyWarhammers hammerItem)
					{
						if (Projectile.owner == Main.myPlayer)
							Projectile.Kill();
					}
					else
					{
						Projectile.timeLeft = 600;
						Projectile.spriteDirection = -owner.direction;

						if (Projectile.ai[1] == 0)
						{
							if (WeakHit)
							{ // Projectiles just did a weak charge swing, kill it
								Projectile.Kill();
								return;
							}

							owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, MathHelper.Pi + guardian.GuardianItemCharge * 0.006f * Projectile.spriteDirection); // set arm position (90 degree offset since arm starts lowered)
							Vector2 armPosition = owner.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, MathHelper.Pi - guardian.GuardianItemCharge * 0.006f * Projectile.spriteDirection) - (new Vector2(owner.Center.X, owner.Center.Y) - new Vector2(owner.Center.X, owner.Center.Y).Floor());
							Projectile.Center = armPosition - new Vector2(((hitboxOffset + hammerItem.HoldOffset) * 2 + 0.3f * guardian.GuardianItemCharge + (float)Math.Sin(MathHelper.Pi / 210f * guardian.GuardianItemCharge) * 10f) * owner.direction * 0.4f, ((hitboxOffset + hammerItem.HoldOffset) * 2 - (hitboxOffset + hammerItem.HoldOffset) * 0.014f * guardian.GuardianItemCharge) * 0.4f);
						}
						else
						{
							if (Projectile.ai[1] < -60f) // Makes easier to sync the behaviour after a weak slam
							{
								Projectile.ai[1] = -60f;
								WeakHit = true;
								guardian.GuardianItemCharge = 0;
							}

							if (Projectile.ai[1] == -60f)
							{ // First frame of the swing
								SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, Projectile.Center);
								Projectile.friendly = true;
								Projectile.netUpdate = true;
								ResetHitStatus(false);
								Projectile.ResetLocalNPCHitImmunity();
								Projectile.localNPCHitCooldown = -1;
							}

							Projectile.velocity = Vector2.UnitX * 0.001f * owner.direction; // So enemies are KBd in the right direction

							float SwingOffset = (float)Math.Sin(MathHelper.Pi / 60f * Projectile.ai[1]);
							Vector2 arm = owner.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, MathHelper.Pi - (guardian.GuardianItemCharge * 0.006f) * Projectile.spriteDirection);
							owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, MathHelper.Pi + (guardian.GuardianItemCharge * 0.006f + SwingOffset * (3f + guardian.GuardianItemCharge * 0.006f)) * Projectile.spriteDirection);
							Vector2 armPosition = owner.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, MathHelper.Pi - (guardian.GuardianItemCharge * 0.006f + SwingOffset * (3f + guardian.GuardianItemCharge * 0.006f)) * Projectile.spriteDirection) - (new Vector2(owner.Center.X, owner.Center.Y) - new Vector2(owner.Center.X, owner.Center.Y).Floor());
							Projectile.Center = armPosition - new Vector2((hitboxOffset * 2 + 0.3f * guardian.GuardianItemCharge + (float)Math.Sin(MathHelper.Pi / 210f * guardian.GuardianItemCharge) * 10f) * owner.direction * 0.4f + (armPosition.X - arm.X) * (2.5f + hitboxOffset * 0.07f), (armPosition.Y - arm.Y) * -(1.1f + hitboxOffset * 0.03f) + (210f - guardian.GuardianItemCharge) * 0.075f);

							if (guardian.GuardianChain > 0f && Projectile.ai[1] < -20)
							{
								Vector2 chainDirection = Vector2.Normalize(Projectile.Center - armPosition);
								float chainOffset = guardian.GuardianChain;
								if (Projectile.ai[1] < -52) chainOffset = (chainOffset / 8f) * (Projectile.ai[1] + 60);
								if (Projectile.ai[1] > -35) chainOffset += (chainOffset / 15f) * (-Projectile.ai[1] - 35);

								Projectile.Center += chainDirection * chainOffset;
							}

							float toAdd = 30f / HammerItem.Item.useTime * HammerItem.SwingSpeed * owner.GetTotalAttackSpeed(DamageClass.Melee);
							if (Projectile.ai[1] < -40) Projectile.ai[1] += toAdd * 1.5f;
							else
							{
								Projectile.ai[1] += toAdd * 0.66f;
								Projectile.friendly = false;
								Projectile.netUpdate = true;
							}

							if (Projectile.ai[1] >= 0f)
							{
								Projectile.ai[1] = 0f;
								Projectile.friendly = false;
								Projectile.netUpdate = true;
							}
						}

					}
				}
				else // Thrown
				{
					Projectile.tileCollide = (Projectile.timeLeft < 598 && range > 0); // Delay helps preventing the hammer from instantly despawning if launched from inside a tile

					if (range == HammerItem.Range)
					{ // First frame of the throw
						SoundEngine.PlaySound(HammerItem.Item.UseSound, owner.Center);
						ResetHitStatus(!WeakThrow);
						Projectile.friendly = true;
						Projectile.netUpdate = true;
						Projectile.ResetLocalNPCHitImmunity();
					}

					OldPosition.Add(new Vector2(Projectile.Center.X, Projectile.Center.Y));
					OldRotation.Add(Projectile.rotation);
					if (OldPosition.Count > 5)
						OldPosition.RemoveAt(0);
					if (OldRotation.Count > 5)
						OldRotation.RemoveAt(0);

					range--;

					if (range < 0)
					{
						float dist = Projectile.Center.Distance(owner.Center);
						Vector2 vel = Vector2.Normalize(owner.Center - Projectile.Center) * HammerItem.ReturnSpeed;

						if (range < -30)
						{
							vel *= 1 - (30 - range) * 0.15f;
							Projectile.velocity = -vel;
						}
						else
						{
							vel *= 0.5f;
							Projectile.velocity += vel;
						}

						if (dist < 30f && owner.whoAmI == Main.myPlayer) Projectile.Kill();

						if (range < -60)
						{
							Projectile.friendly = false;
							Projectile.netUpdate = true;
						}
					}

					if (WeakThrow)
						Projectile.rotation += 0.25f * (Projectile.velocity.X > 0 ? 1 : -1);
					else
						Projectile.rotation += Projectile.velocity.Length() / 30f * (Projectile.velocity.X > 0 ? 1f : -1f) * 1.2f;
				}
			}
		}

		public override void ModifyDamageHitbox(ref Rectangle hitbox)
		{
			hitbox.X -= hitboxOffset;
			hitbox.Y -= hitboxOffset;
			hitbox.Width += hitboxOffset * 2;
			hitbox.Height += hitboxOffset * 2;
		}

		public override void SafeModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (HammerItem != null)
			{
				if (Projectile.ai[1] < 0) // Swing hit
				{
					if (Main.LocalPlayer.GetModPlayer<OrchidGuardian>().GuardianItemCharge >= 180f)
						modifiers.FinalDamage *= HammerItem.SwingDamage;
					else
						modifiers.FinalDamage *= HammerItem.SwingDamage * 1.5f;
				}
				else // Throw hit
				{
					if (target.lifeMax > 5)
					{
						modifiers.FinalDamage *= HammerItem.ThrowDamage * (1f - 0.25f * HitCount);
						if (HitCount < 3) HitCount++;
					}
				}
			}
		}

		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
		{
			if (Projectile.ai[1] < 0) // Less damage for melee hits
			{
				if (Main.LocalPlayer.GetModPlayer<OrchidGuardian>().GuardianItemCharge < 180f)
					modifiers.FinalDamage *= 0.5f;
				else
					modifiers.FinalDamage *= 0.75f;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
			
			if (Projectile.velocity.X != oldVelocity.X)
				Projectile.velocity.X = -oldVelocity.X;
			if (Projectile.velocity.Y != oldVelocity.Y)
				Projectile.velocity.Y = -oldVelocity.Y;
			
			SoundEngine.PlaySound(ToyWarhammers.SqueakSound, Projectile.Center);
			return false;
		}

		public override void SafeOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone, Player player, OrchidGuardian guardian)
		{
			if (Projectile.ai[1] > 0)
			{ // Throw
				bool weak = WeakThrow;
				if (FirstHit)
				{
					if (!weak)
					{
						guardian.AddSlam(HammerItem.SlamStacks);
						guardian.AddGuard(HammerItem.GuardStacks);
					}
				}
				SoundEngine.PlaySound(ToyWarhammers.SqueakSound, Projectile.Center);

				if (!penetrate && target.lifeMax > 5)
				{
					range = -40;
					Projectile.netUpdate = true;
				}
			}
			else
			{ // Melee Swing
				if (FirstHit)
				{
					if (guardian.GuardianItemCharge > 0f)
					{
						guardian.GuardianItemCharge += 60f * HammerItem.SwingChargeGain * player.GetTotalAttackSpeed(DamageClass.Melee);
						if (guardian.GuardianItemCharge > 210f)
							guardian.GuardianItemCharge = 210f;
					}
				}
				SoundEngine.PlaySound(ToyWarhammers.SqueakSound, Projectile.Center);
			}
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(HammerItem.Item.type);
			writer.Write(range);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			int itemtype = reader.ReadInt32();
			range = reader.ReadInt32();

			if (HammerItem == null)
			{
				OrchidGuardian guardian = Main.player[Projectile.owner].GetModPlayer<OrchidGuardian>();
				guardian.GuardianItemCharge = 0f;

				Item item = new(itemtype);
				if (item.ModItem is ToyWarhammers hammerItem)
				{
					HammerItem = hammerItem;

					
					if (Main.netMode != NetmodeID.Server)
					{
						HammerTexture = TextureAssets.Item[hammerItem.Item.type].Value;
						hitboxOffset = (int)(HammerTexture.Width * hammerItem.Item.scale / 2f);
						DrawOriginOffsetX = DrawOriginOffsetY = hitboxOffset;
						//Projectile.width = (int)(HammerTexture.Width * hammerItem.Item.scale);
						//Projectile.height = (int)(HammerTexture.Height * hammerItem.Item.scale);
						
						if (HammerItem.hasSpecialHammerTexture) HammerTexture =  ModContent.Request<Texture2D>(hammerItem.HammerTexture, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
					}

					Projectile.scale = hammerItem.Item.scale * guardian.GuardianWeaponScale;

					range = HammerItem.Range;
					penetrate = false;
				}
			}
		}

		public override bool OrchidPreDraw(SpriteBatch spriteBatch, ref Color lightColor)
		{
			if (HammerTexture == null) return false;
			Player player = Main.player[Projectile.owner];
			OrchidGuardian guardian = player.GetModPlayer<OrchidGuardian>();
			Rectangle drawRectangle = HammerTexture.Frame();

			float rotationBonus = 0f;

			SpriteEffects effect;
			if (Projectile.spriteDirection == 1)
			{
				effect = SpriteEffects.FlipHorizontally;
				rotationBonus += MathHelper.PiOver2;
			}
			else
			{
				effect = SpriteEffects.None;
				rotationBonus -= MathHelper.PiOver2;
			}

			Vector2 posproj = Projectile.Center;
			//float rotaproj = Projectile.rotation;
			if (player.gravDir == -1)
			{
				if (Projectile.ai[1] <= 0)
					posproj.Y = (player.Bottom.Floor() + player.position.Floor()).Y - posproj.Y;
				//rotaproj += MathHelper.Pi;
				effect = effect == SpriteEffects.None ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			}

			var color = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f), Color.White);
			var position = posproj - Main.screenPosition + Vector2.UnitY * player.gfxOffY;

			if (Projectile.ai[1] == 0)
				rotationBonus += guardian.GuardianItemCharge * 0.0065f * player.gravDir * Projectile.spriteDirection;

			if (Projectile.ai[1] < 0)
			{
				float SwingOffset = (float)Math.Sin(MathHelper.Pi / 60f * Projectile.ai[1]);
				rotationBonus += (guardian.GuardianItemCharge * 0.0065f + SwingOffset * (3.5f + guardian.GuardianItemCharge * 0.006f)) * player.gravDir * Projectile.spriteDirection;
			}

			if (guardian.GuardianChain > 0f && guardian.GuardianChainTexture != null)
			{ // Verveine wants to consume a shoebox
				Texture2D chainTexture = ModContent.Request<Texture2D>(guardian.GuardianChainTexture, AssetRequestMode.ImmediateLoad).Value;
				Vector2 chainDirection = Vector2.Normalize(Projectile.Center - player.Center);
				float chainOffset = guardian.GuardianChain;
				if (Projectile.ai[1] < -52) chainOffset = (chainOffset / 8f) * (Projectile.ai[1] + 60);
				if (Projectile.ai[1] > -35) chainOffset += (chainOffset / 15f) * (-Projectile.ai[1] - 35);

				while (chainOffset > 0f)
				{
					Vector2 chainPos = position - chainDirection * (chainOffset + 5.4f);
					chainOffset -= chainTexture.Height * 0.66f;
					spriteBatch.Draw(chainTexture, chainPos, null, color, 0f, chainTexture.Size() * 0.5f, 1f, effect, 0f);
				}
			}

			if (Projectile.ai[1] != 0)
			{
				for (int i = 0; i < OldPosition.Count; i++)
				{
					color = Lighting.GetColor((int)(OldPosition[i].X / 16f), (int)(OldPosition[i].Y / 16f), Color.White) * (WeakThrow ? (0.35f * i) - 0.65f : (0.15f * i));
					position = OldPosition[i] - Main.screenPosition + Vector2.UnitY * player.gfxOffY;

					spriteBatch.Draw(HammerTexture, position, drawRectangle, color, OldRotation[i] + rotationBonus, drawRectangle.Size() * 0.5f, Projectile.scale, effect, 0f);
				}
			}

			spriteBatch.Draw(HammerTexture, position, drawRectangle, color, Projectile.rotation + rotationBonus, drawRectangle.Size() * 0.5f, Projectile.scale, effect, 0f);

			return false;
        }
	}
}