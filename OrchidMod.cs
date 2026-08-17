using OrchidMod.Content.Alchemist;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using OrchidMod.Utilities;
using OrchidMod.Common;
using System;
using Terraria.ModLoader.Core;
using System.Reflection;
using OrchidMod.Common.Attributes;
using OrchidMod.Common.Global.NPCs;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Shaders;
using OrchidMod.Content.Shapeshifter;
using Terraria.ModLoader.IO;
using OrchidMod.Content.Guardian;
using OrchidMod.Content.Guardian.Projectiles.Misc;
using Terraria.Audio;

namespace OrchidMod
{
	public partial class OrchidMod : Mod
	{
		public static OrchidMod Instance { get; private set; }
		public static Mod ThoriumMod { get; private set; }
		public static Mod BetterCaves { get; private set; }
		public static Mod Consolaria { get; private set; }
		
		public static OrchidClientConfig OrchidClientConfig { get; private set; }
		
		public static OrchidServerConfig OrchidServerConfig { get; private set; }

		public static List<AlchemistHiddenReactionRecipe> AlchemistReactionRecipes;

		public OrchidMod()
		{
			Instance = this;
			ContentAutoloadingEnabled = false;
		}

		public void LoadContent()
		{
			LoaderUtils.ForEachAndAggregateExceptions((
				from t in AssemblyManager.GetLoadableTypes(Code)
				where !t.IsAbstract && !t.ContainsGenericParameters
				where t.IsAssignableTo(typeof(ILoadable))
				where t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null
				where AutoloadAttribute.GetValue(t).NeedsAutoloading
				select t).OrderBy((Type type) => type.FullName, StringComparer.InvariantCulture), delegate (Type t)
				{
					var instance = (ILoadable)Activator.CreateInstance(t, true);

					if (ModContent.GetInstance<OrchidServerConfig>().LoadCrossmodContentWithoutRequiredMods)
					{
						AddContent(instance);
						return;
					}

					var atr = t.GetCustomAttribute<CrossmodContentAttribute>();

					if (atr is null)
					{
						AddContent(instance);
						return;
					}

					var hasAllMods = true;

					foreach (var mod in atr.Mods)
					{
						hasAllMods &= ModLoader.HasMod(mod);
					}

					if (hasAllMods)
					{
						AddContent(instance);
						return;
					}
				}
			);
		}

		public void LoadShaders()
		{
			GameShaders.Misc["OrchidMod:HorizonGlow"] = new MiscShaderData(Assets.Request<Effect>("Assets/Effects/HorizonGlow"), "HorizonShaderPass");
		}

		public override void Load()
		{
			LoadContent();
			LoadShaders();

			ThoriumMod = OrchidUtils.GetModWithPossibleNull("ThoriumMod");
			BetterCaves = OrchidUtils.GetModWithPossibleNull("VervCaves");
			Consolaria = OrchidUtils.GetModWithPossibleNull("Consolaria");

			AlchemistReactionRecipes = AlchemistHiddenReactionHelper.ListReactions();
			
			OrchidClientConfig = ModContent.GetInstance<OrchidClientConfig>();
			OrchidServerConfig = ModContent.GetInstance<OrchidServerConfig>();
		}

		public override void Unload()
		{
			AlchemistReactionRecipes = null;

			OrchidServerConfig = null;
			OrchidClientConfig = null;
			
			ThoriumMod = null;
			BetterCaves = null;
			Instance = null;
		}

		public override void PostSetupContent()
		{
			foreach (var type in Code.GetTypes())
			{
				if (!type.GetInterfaces().Contains(typeof(IPostSetupContent))) continue;

				var instance = (IPostSetupContent)Activator.CreateInstance(type, null);
				instance.PostSetupContent(this);
			}

			ThoriumModCalls();
			//BossChecklistCalls();
			CensusModCalls();
			ColoredDamageTypeModCalls();
			RecipeBrowserModCalls();
			// WikiThisModCalls();
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			OrchidModMessageType msgType = (OrchidModMessageType)reader.ReadByte();
			byte playernumber;
			switch (msgType)
			{
				case OrchidModMessageType.ORCHIDPLAYERSYNCPLAYERGAMBLER:
					playernumber = reader.ReadByte();
					OrchidGambler modPlayerGambler = Main.player[playernumber].GetModPlayer<OrchidGambler>();
					bool cardInDeck = reader.ReadBoolean();
					modPlayerGambler.gamblerHasCardInDeck = cardInDeck;
					break;

				case OrchidModMessageType.GAMBLERCARDINDECKCHANGED:
					playernumber = reader.ReadByte();
					modPlayerGambler = Main.player[playernumber].GetModPlayer<OrchidGambler>();
					cardInDeck = reader.ReadBoolean();
					modPlayerGambler.gamblerHasCardInDeck = cardInDeck;
					if (Main.netMode == NetmodeID.Server)
					{
						var packet = GetPacket();
						packet.Write((byte)OrchidModMessageType.GAMBLERCARDINDECKCHANGED);
						packet.Write(playernumber);
						packet.Write(modPlayerGambler.gamblerHasCardInDeck);
						packet.Send(-1, playernumber);
					}
					break;

				case OrchidModMessageType.SYNCONKILLNPC:
					NPC npcKilled = Main.npc[reader.ReadInt32()];
					npcKilled.GetGlobalNPC<ShapeshifterGlobalNPC>().OnKillShapeshifterGlobalNPC(npcKilled);
					break;

				case OrchidModMessageType.SHAPESHIFTERAPPLYBLEEDTONPC:
					NPC npc = Main.npc[reader.ReadInt32()];
					ShapeshifterGlobalNPC globalNPCShifter = npc.GetGlobalNPC<ShapeshifterGlobalNPC>();
					int bleedowner = reader.ReadInt32();
					int potency = reader.ReadInt32();
					int maxStacks = reader.ReadInt32();
					int timer = reader.ReadInt32();
					bool generalBleed = reader.ReadBoolean();

					globalNPCShifter.ApplyBleed(bleedowner, timer, potency, maxStacks, generalBleed);

					if (Main.netMode == NetmodeID.Server)
					{
						var packet = GetPacket();
						packet.Write((byte)OrchidModMessageType.SHAPESHIFTERAPPLYBLEEDTONPC);
						packet.Write(npc.whoAmI);
						packet.Write(bleedowner);
						packet.Write(potency);
						packet.Write(maxStacks);
						packet.Write(timer);
						packet.Write(generalBleed);
						packet.Send();
					}

					break;

				case OrchidModMessageType.SHAPESHIFTERHOOKDASH:
					byte whoamI = reader.ReadByte();
					Main.player[whoamI].GetModPlayer<OrchidShapeshifter>().ShapeshifterHookDashSync = true;

					if (Main.netMode == NetmodeID.Server)
					{
						var packet = GetPacket();
						packet.Write((byte)OrchidModMessageType.SHAPESHIFTERHOOKDASH);
						packet.Write(whoamI);
						packet.Send(ignoreClient: whoAmI);
					}

					break;

				case OrchidModMessageType.GUARDIANKATARAPPLYBLEEDTONPC:
					npc = Main.npc[reader.ReadInt32()];
					GuardianGlobalNPC globalNPCGuardian = npc.GetGlobalNPC<GuardianGlobalNPC>();
					int bleedAmount = reader.ReadInt32();
					globalNPCGuardian.KatarBleed += bleedAmount;
					SoundEngine.PlaySound(SoundID.NPCHit18.WithPitchOffset(Main.rand.NextFloat(0.2f, 0.5f)), npc.Center);

					if (Main.netMode == NetmodeID.Server)
					{
						var packet = GetPacket();
						packet.Write((byte)OrchidModMessageType.GUARDIANKATARAPPLYBLEEDTONPC);
						packet.Write(npc.whoAmI);
						packet.Write(bleedAmount);
						packet.Send();
					}

					break;

				case OrchidModMessageType.NPCHITBYCLASS: // Received by the server when a player damages a NPC for the first time with a orchid damage class
					OrchidGlobalNPC globalNPC = Main.npc[reader.ReadByte()].GetGlobalNPC<OrchidGlobalNPC>();
					switch (reader.ReadByte())
					{
						default:
							globalNPC.AlchemistHit = true;
							break;
						case 1:
							globalNPC.GamblerHit = true;
							break;
						case 2:
							globalNPC.GuardianHit = true;
							break;
						case 3:
							globalNPC.ShamanHit = true;
							break;
						case 4:
							globalNPC.ShapeshifterHit = true;
							break;
					}
					break;

				default:
					Logger.WarnFormat("OrchidMod: Unknown Message type: {0}", msgType);
					break;
			}
		}

		public override object Call(params object[] args)
		{
			if (args is null)
				throw new ArgumentNullException(nameof(args), "OrchidMod: Call failed, arguments cannot be empty!");
			if (args.Length == 0)
				throw new ArgumentException("OrchidMod: Call failed, must have at least 1 argument!");
			
			if (args[0] is string function)
			{
				switch (function)
				{
					case "GetGuardianSlam" or "GetGuardianSlamMax" or "GetGuardianGuard" or "GetGuardianGuardMax":
						if (args.Length != 2)
							return new ArgumentException($"OrchidMod: {nameof(function)} call failed, must have exactly 2 arguments ([string] call name, [Player] player instance / [int] player index)");
						if (args[1] is not int or Player)
							return new ArgumentException($"OrchidMod: {nameof(function)} call failed, second argument {args[1].GetType().Name} is not an int or a Player!");
						Player guardianPlayer = GetPlayerFromArg(args[1]);
						if (guardianPlayer is null)
							return new NullReferenceException($"OrchidMod: {nameof(function)} call failed, {nameof(guardianPlayer)} is not a valid player instance!");
						OrchidGuardian guardian = guardianPlayer.Guardian();
						return function switch
						{
							"GetGuardianSlam" => guardian.GuardianSlam,
							"GetGuardianSlamMax" => guardian.GuardianSlamMax,
							"GetGuardianGuard" => guardian.GuardianGuard,
							"GetGuardianGuardMax" => guardian.GuardianGuardMax,
							_ => 0
						};
					case "AddProjectileToGuardianBlacklist":
						if (args.Length != 2)
							throw new ArgumentException("OrchidMod: AddProjectileToGuardianBlacklist failed, must have exactly 2 arguments ([string] call name, [int] projectile ID)");
						if (args[1] is not int projectileID)
							throw new Exception($"OrchidMod: AddProjectileToGuardianBlacklist call failed, first argument {args[1].GetType().Name} is not an int!");
						if (projectileID > ProjectileLoader.ProjectileCount || projectileID < 0)
							throw new Exception($"OrchidMod: AddProjectileToGuardianBlacklist call failed, first argument {projectileID} is not a valid projectile ID!");
						
						if (OrchidGuardian.ProjectilesBlockBlacklist.Contains(projectileID))
							Logger.WarnFormat("OrchidMod: OrchidGuardian.ProjectilesBlockBlacklist already contains an entry for {0}", ContentSamples.ProjectilesByType[projectileID].Name);
						OrchidGuardian.ProjectilesBlockBlacklist.Add(projectileID);
						break;
					case "AddHorizonDevName":
						if (args[1] is not string devName)
							throw new Exception($"OrchidMod: AddHorizonDevName call failed, first argument {args[1].GetType().Name} is not a string!");
						if (args[2] is not GuardianHorizonLanceProj.HorizonColor horizonColor)
							throw new Exception($"OrchidMod: AddHorizonDevName call failed, second argument {args[2].GetType().Name} is not a valid HorizonColor entry!");

						if (!GuardianHorizonLanceProj.HorizonColorLoader.TryAdd(devName, horizonColor))
							throw new Exception($"OrchidMod: AddHorizonDevName call failed, there already exists a color entry for the name \"{devName}\"!");
						break;
					
				}
			}
			return false;
		}

		public static Player GetPlayerFromArg(object player)
		{
			return player switch
			{
				int index => Main.player[index],
				Player instance => instance,
				_ => null
			};
		}
	}
}
