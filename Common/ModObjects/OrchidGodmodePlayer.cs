using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Terraria;
using Terraria.ModLoader;

namespace OrchidMod.Common.ModObjects;

public class OrchidGodmodePlayer : ModPlayer
{
	public static bool GodModeEnabled;
	
	public static Mod DragonLens;
	
	internal static bool CheatSheetGodMode { get; set; }
	internal static bool HEROsModGodMode { get; set; }
	internal static bool DragonLensGodMode { get; set; }
	
	private static FieldInfo DLGodModeState;
	private static MethodInfo DLGodModeService;
	
	private static Hook _csGodMode_EnabledHook;
	private static Hook _dlGodMode_OnActivateHook;
		
	internal delegate void orig_CSGodMode_Enabled(bool value);
	// HERO's Mod has a built-in Godmode callback modcall
	internal delegate void orig_DLGodMode_OnActivate(object self);
	
	public override void Load()
	{
		if (OrchidUtils.GetModWithPossibleNull("CheatSheet") is { } cheatSheet)
		{
			Mod.Logger.Debug("OrchidGuardian: CheatSheet detected, attempting to find GodMode...");
			Type csGodMode = cheatSheet.Code.GetType("CheatSheet.Menus.GodMode");
			if (csGodMode is not null)
			{
				Mod.Logger.Debug("OrchidGuardian: Found CheatSheet Godmode, attempting to find setter method of property 'Enabled'...");
				PropertyInfo csGodModeProperty = csGodMode.GetProperty("Enabled", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (csGodModeProperty is not null && csGodModeProperty.SetMethod is { } csGodModeService)
				{
					_csGodMode_EnabledHook = new Hook(csGodModeService, Detour_CSGodMode_Enabled);
					if (_csGodMode_EnabledHook.IsApplied) 
						Mod.Logger.DebugFormat("OrchidGuardian: CheatSheet Godmode hook seemingly applied!");
				}
			}
		}
		
		if (OrchidUtils.GetModWithPossibleNull("DragonLens") is { } dragonLens)
		{
			Mod.Logger.Debug("OrchidGuardian: DragonLens detected, attempting to find GodMode...");
			Type dlGodMode = dragonLens.Code.GetType("DragonLens.Content.Tools.Gameplay.Godmode");
			if (dlGodMode != null)
			{
				Mod.Logger.Debug("OrchidGuardian: Found DragonLens Godmode, attempting to find 'godMode' field and 'OnActivate()' method...");
				// DragonLens uses a static bool to store its godMode state and a separate 
				FieldInfo dlGodModeState = dlGodMode.GetField("godMode", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (dlGodModeState is not null) 
					Mod.Logger.Debug("OrchidGuardian: Found DragonLensGodmode.godMode field");
				MethodInfo dlGodModeService = dlGodMode.GetMethod("OnActivate", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (dlGodModeService is not null) 
					Mod.Logger.Debug("OrchidGuardian: Found DragonLensGodmode.OnActivate method");
				if (dlGodModeState is not null && dlGodModeService is not null)
				{
					DLGodModeState = dlGodModeState;

					_dlGodMode_OnActivateHook = new Hook(dlGodModeService, Detour_DLGodMode_OnActivate);
					if (_dlGodMode_OnActivateHook.IsApplied) 
						Mod.Logger.DebugFormat("OrchidGuardian: DragonLens Godmode hook seemingly applied!");

					if (DLGodModeState.GetValue(null) is bool state)
					{
						Mod.Logger.DebugFormat("OrchidGuardian: Setting DragonLens godmode state to {0}", state);
						DragonLensGodMode = state;
					}
				}
			}
		}
	}

	public override void ResetEffects() => GodModeEnabled = CheatSheetGodMode || HEROsModGodMode || DragonLensGodMode;


	private void Detour_CSGodMode_Enabled(orig_CSGodMode_Enabled orig, bool value)
	{
		orig(value);
		CheatSheetGodMode = value;
	}
	
	private void Detour_DLGodMode_OnActivate(orig_DLGodMode_OnActivate orig, object self)
	{
		orig(self);
		if (DLGodModeState?.GetValue(null) is bool state)
		{
			Mod.Logger.DebugFormat("OrchidGuardian: Setting DragonLens godmode state to {0}", state);
			DragonLensGodMode = state;
		}
	}
	
	public override void Unload()
	{
		DragonLens = null;
		DLGodModeState = null;
		DLGodModeService = null;

		CheatSheetGodMode = false;
		HEROsModGodMode = false;
		DragonLensGodMode = false;
		
		_csGodMode_EnabledHook?.Dispose();
		_csGodMode_EnabledHook = null;
		
		_dlGodMode_OnActivateHook?.Dispose();
		_dlGodMode_OnActivateHook = null;
	}
}