using System;

using Dalamud.Plugin;
using Dalamud.Interface;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.UI;

using Ktisis.History;
using Ktisis.Services;
using Ktisis.Interface;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Overlay;
using Ktisis.Structs.Actor;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";
		public string CommandName = "/ktisis";

		public static string Version = $"Alpha {GetVersion()}";

		public static Configuration Configuration { get; private set; } = null!;
		public static UiBuilder UiBuilder { get; private set; } = null!;

		public static bool IsInGPose => DalamudServices.PluginInterface.UiBuilder.GposeActive && IsGposeTargetPresent();
		public unsafe static bool IsGposeTargetPresent() => (IntPtr)DalamudServices.Targets->GPoseTarget != IntPtr.Zero;

		public unsafe static GameObject? GPoseTarget
			=> IsInGPose ? DalamudServices.ObjectTable.CreateObjectReference((IntPtr)DalamudServices.Targets->GPoseTarget) : null;
		public unsafe static Actor* Target => GPoseTarget != null ? (Actor*)GPoseTarget.Address : null;

		public static string GetVersion() {
			var ver = typeof(Ktisis).Assembly.GetName().Version!.ToString();
			return ver.Substring(0, ver.LastIndexOf("."));
		}

		public Ktisis(DalamudPluginInterface pluginInterface) {
			DalamudServices.Init(pluginInterface);
			Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			UiBuilder = pluginInterface.UiBuilder;

			if (Configuration.IsFirstTimeInstall) {
				Configuration.IsFirstTimeInstall = false;
				//Information.Show();
			}
			if (Configuration.LastPluginVer != Version) {
				Configuration.LastPluginVer = Version;
			}

			Configuration.Validate();

			// Init interop stuff

			InteropService.Init();
			Interop.Methods.Init();
			Interop.StaticOffsets.Init();

			Interop.Hooks.ActorHooks.Init();
			Interop.Hooks.ControlHooks.Init();
			Interop.Hooks.EventsHooks.Init();
			Interop.Hooks.GuiHooks.Init();
			Interop.Hooks.PoseHooks.Init();

			Input.Init();

			// Register command

			DalamudServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "/ktisis - Show the Ktisis interface."
			});

			//pluginInterface.UiBuilder.OpenConfigUi += ConfigGui.Toggle;
			pluginInterface.UiBuilder.DisableGposeUiHide = true;
			pluginInterface.UiBuilder.Draw += KtisisGui.Draw;

			EditorService.Init();
			HistoryManager.Init();

			//References.LoadReferences(Configuration);

			unsafe {
				var ui = (UIModule*)DalamudServices.GameGui.GetUIModule();
				ui->ExitGPose();
			}
		}

		public void Dispose() {
			DalamudServices.CommandManager.RemoveHandler(CommandName);
			DalamudServices.PluginInterface.SavePluginConfig(Configuration);
			//Services.PluginInterface.UiBuilder.OpenConfigUi -= ConfigGui.Toggle;

			OverlayWindow.DeselectGizmo();

			Interop.Hooks.ActorHooks.Dispose();
			Interop.Hooks.ControlHooks.Dispose();
			Interop.Hooks.EventsHooks.Dispose();
			Interop.Hooks.GuiHooks.Dispose();
			Interop.Hooks.PoseHooks.Dispose();

			InteropService.Dispose();

			Data.Sheets.Cache.Clear();

			//if (EditEquip.Items != null)
				//EditEquip.Items = null;

			Input.Dispose();
			HistoryManager.Dispose();

			/*foreach (var (_, texture) in References.Textures) {
				texture.Dispose();
			}*/
		}

		private void OnCommand(string command, string arguments) {
			switch (arguments) {
				case "about":
				case "info":
				case "information":
					//Information.Toggle();
					break;
				case "cfg":
				case "config":
				case "configure":
				case "configuration":
					//ConfigGui.Toggle();
					break;
				default:
					KtisisGui.GetWindowOrCreate<Sidebar>().Toggle();
					break;
			}
		}
	}
}