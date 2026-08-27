using Microsoft.Xna.Framework;
using OrchidMod.Common.Attributes;
using OrchidMod.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace OrchidMod.Content.Guardian.Weapons.FencingBlades;

[CrossmodContent("ThoriumMod")]
public class ThoriumTemplarFencingBlade : OrchidModGuardianFencingBlade
{
	public NPC HitTarget;
	public int HitCount = 0;
	 
	public override FencingBladeAttackProfile ChargedProfile(Projectile anchor) => FencingBladeAttackID.MultiSwing with
	{ Quantity = 3, BendAmount = 0.02f, Damage = 0.8f, ControlAngle = MathHelper.Pi / 6f };
	
	public override FencingBladeAttackProfile ReinforcedProfile(Projectile anchor) => FencingBladeAttackID.SingleSwing with
		{ Quantity = 7, UsesFocusProjectile = true };
		
	public override void SafeSetDefaults()
	{
		Item.useTime = 35;
		Item.damage = 108;
		Item.rare = ItemRarityID.Green;
		Item.width = 52;
		Item.height = 48;
		Item.knockBack = 3f;
		Item.shootSpeed = 15f;
		Item.value = Item.sellPrice(0, 3);
	}

	public override void UpdateInventory(Player player)
	{
		if (player.HeldItem.ModItem is not ThoriumTemplarFencingBlade && HitTarget != null && !HitTarget.active)
		{
			HitTarget = null;
			HitCount = 0;
		}
	}

	public override Color GetColor(Player player, OrchidGuardian guardian, Projectile anchor) => Color.Lerp(new Color(188, 0, 62), new Color(255, 137, 137), Main.rand.NextFloat());

	public override void OnHit(Player player, OrchidGuardian guardian, NPC target, Projectile projectile, NPC.HitInfo hit)
	{
		if (projectile.ModProjectile is FencingBladeSlashProjectile slash && slash.Strong)
		{
			if (HitTarget == null) HitTarget = target;
			if (target == HitTarget)
			{
				if (++HitCount == 7)
				{
					HitTarget = null;
					HitCount = 0;
					
					SoundEngine.PlaySound(SoundID.Item4, target.Center);
					OrchidUtils.SpawnDustCircle(target.Center, 4f, 20, DustID.LifeCrystal);
					Item.NewItem(projectile.GetSource_OnHit(target), Entity.Center, ItemID.Heart);
				}
			}
			else
			{
				HitTarget = null;
				HitCount = 0;
			}
		}
	}

	public override void ExtraAIFencingBlade(Player player, OrchidGuardian guardian, Projectile anchor)
	{
		if (anchor.ModProjectile is GuardianFencingBladeAnchor fencingBlade)
		{
			if (anchor.ai[0] is 0 or 1f)
			{
				HitTarget = null;
				HitCount = 0;
			}
			foreach (Item item in Main.ActiveItems)
			{
				if (item.type == ItemID.Heart && item.Distance(player.Center) < 20f)
					item.velocity += item.DirectionTo(player.Center) * 0.1f;
			}
		}
	}
}