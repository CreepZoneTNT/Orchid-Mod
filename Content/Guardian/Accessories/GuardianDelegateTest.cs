using Microsoft.Xna.Framework;
using OrchidMod.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace OrchidMod.Content.Guardian.Accessories;

public class GuardianDelegateTest : OrchidModGuardianItem
{
	public override string Texture => "OrchidMod/Content/Guardian/Accessories/GuardianTest";
	
	public override void SafeSetDefaults()
	{
		Item.width = 24;
		Item.height = 28;
		Item.value = Item.sellPrice(0, 0, 30, 0);
		Item.rare = -11;
		Item.accessory = true;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		OrchidGuardian guardian = player.Guardian();
		guardian.onUseSlamDelegate += OnUseSlam;
		guardian.addGuardDelegate += AddGuard;
		guardian.onBlockFirstDelegate += OnBlockFirst;
		guardian.useSlamDelegate += UseSlam;
	}

	public void AddGuard(Player player, OrchidGuardian guardian, int nb)
	{
		if (nb > 1)
			CombatText.NewText(player.getRect(), Color.White, "Bazinga");
	}

	public void OnUseSlam(Player player, OrchidGuardian guardian)
	{
		Projectile proj = Projectile.NewProjectileDirect(player.GetSource_FromAI(), player.Center, Main.rand.NextVector2Unit(MathHelper.Pi, MathHelper.Pi) * 6f, ProjectileID.NailFriendly, guardian.GetGuardianDamage(80), 10f);
		proj.DamageType = ModContent.GetInstance<GuardianDamageClass>();
	}
	
	public void OnBlockFirst(Player player, OrchidGuardian guardian, Projectile anchor, Entity aggressor, ref int toAdd, bool parry)
	{
		if (aggressor is NPC)
		{
			((NPC)aggressor).AddBuff(BuffID.Daybreak, 60);
			
			if (parry && guardian.IsPerfectParry(15, ref toAdd))
			{
				CombatText.NewText(player.getRect(), Color.White, "Bazinga");
				SoundEngine.PlaySound(SoundID.Item4, player.Center);
			}
		}
	}
	public void OnBlockFirst2(Player player, OrchidGuardian guardian, Projectile anchor, Entity aggressor, ref int toAdd, bool parry)
	{
		if (aggressor is NPC or Projectile)
		{
			CombatText.NewText(player.getRect(), Color.White, "Bazinga");
			SoundEngine.PlaySound(SoundID.Item4, player.Center);
			if (anchor.ModProjectile is not GuardianHammerAnchor && guardian.IsPerfectParry(15, ref toAdd) && aggressor is NPC)
				((NPC)aggressor).AddBuff(BuffID.Daybreak, 60);
		}
	}

	public bool UseSlam(Player player, OrchidGuardian guardian, int nb) => !Main.rand.NextBool(3);
}