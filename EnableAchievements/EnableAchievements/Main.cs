using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using PavonisInteractive.TerraInvicta;

namespace EnableAchievements
{
	static class Main
	{
		public static bool enabled;
		public static UnityModManager.ModEntry mod;
		public static Settings settings;

		public static bool Load(UnityModManager.ModEntry modEntry)
		{
			var harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
			mod = modEntry;
			modEntry.OnToggle = OnToggle;
			modEntry.OnGUI = OnGUI;
			modEntry.OnSaveGUI = OnSaveGUI;

			return true;
		}

		public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
		{
			enabled = value;
			//modEntry.Logger.Log($"EnableAchievements set to {value}");
			return true;
		}

		static void OnGUI(UnityModManager.ModEntry modEntry)
		{
			settings.Draw(modEntry);
		}

		static void OnSaveGUI(UnityModManager.ModEntry modEntry)
		{
			settings.Save(modEntry);
		}

		internal class Settings : UnityModManager.ModSettings, IDrawable
		{
			public override void Save(UnityModManager.ModEntry modEntry)
			{
				Save(this, modEntry);
			}

			public void OnChange()
			{
			}

			[Draw("Enable Achievements when console is enabled")]
			public bool allowConsole = false;
			[Draw("Enable Achievements when in Skirmish")]
			public bool allowSkirmish = false;
		}
	}

	[HarmonyPatch(typeof(TIFactionState), "UnlockAchievement")]
	public static class OnContinuePatch
	{
		public static void Postfix(string apiName, TIFactionState __instance)
		{
			try
			{
				if (!Main.enabled)
					return;
				if (GameControl.control.skirmishMode && !Main.settings.allowSkirmish)
					return;
				if (TemplateManager.global.debug_ConsoleActive && !Main.settings.allowConsole)
					return;
				if (SteamManager.Initialized && TIPlayerProfileManager.useMods)
					Traverse.Create(__instance).Method("UnlockSteamAchievement", new Type[] { typeof(string) }).GetValue(new object[] { apiName });
			}
			catch (Exception e)
			{
				Main.mod.Logger.Error(e.ToString());
			}
		}
	}
}
