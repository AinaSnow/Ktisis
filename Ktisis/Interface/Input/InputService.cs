using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI;

using Ktisis.Core;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Input;
using Ktisis.Events;
using Ktisis.Interface.Input.Keys;
using Ktisis.Interop;
using Ktisis.Services;

namespace Ktisis.Interface.Input;

[DIService]
public class InputService {
	// Service
	
	private readonly InteropService _interop;
	private readonly IKeyState _keyState;
	private readonly GPoseService _gpose;
	private readonly ConfigService _cfg;
	private readonly IGameGui _gui;

	private ControlHooks? ControlHooks;

	public InputService(
		InteropService _interop,
		IKeyState _keyState,
		GPoseService _gpose,
		ConfigService _cfg,
		IGameGui _gui,
		InitHooksEvent _initHooks
	) {
		this._interop = _interop;
		this._keyState = _keyState;
		this._gpose = _gpose;
		this._cfg = _cfg;
		this._gui = _gui;
		
		_gpose.OnGPoseUpdate += OnGPoseUpdate;
        
		_initHooks.Subscribe(InitHooks);
	}

	private void InitHooks() {
		this.ControlHooks = this._interop.Create<ControlHooks>().Result;
		this.ControlHooks.OnKeyEvent += OnKeyEvent;
	}
	
	// Hotkeys

	private readonly Dictionary<string, HotkeyInfo> Hotkeys = new();

	public void RegisterHotkey(HotkeyInfo hk, Keybind? defaultBind) {
		var cfg = this._cfg.Config;
		if (!cfg.Keybinds.TryGetValue(hk.Name, out var keybind) && defaultBind != null) {
			keybind = defaultBind;
			cfg.Keybinds.Add(hk.Name, keybind);
		}

		if (keybind != null)
			hk.Keybind = keybind;
		
		this.Hotkeys.Add(hk.Name, hk);
	}

	public HotkeyInfo? GetActiveHotkey(VirtualKey key, HotkeyFlags flag) {
		HotkeyInfo? result = null;
		
		var modMax = 0;
		foreach (var (_, hk) in this.Hotkeys) {
			var bind = hk.Keybind;
			if (bind.Key != key || !hk.Flags.HasFlag(flag) || !bind.Mod.All(mod => this._keyState[mod]))
				continue;

			var modCt = bind.Mod.Length;
			if (result != null && modCt < modMax)
				continue;
			
			result = hk;
			modMax = modCt;
		}

		return result;
	}

	public bool TryGetHotkey(string name, out HotkeyInfo? hotkey)
		=> this.Hotkeys.TryGetValue(name, out hotkey);
	
	// Events

	private void OnGPoseUpdate(bool active) {
		if (active)
			this.ControlHooks?.EnableAll();
		else
			this.ControlHooks?.DisableAll();
	}

	private bool OnKeyEvent(VirtualKey key, VirtualKeyState state) {
		if (!this._cfg.Config.Keybinds_Active || !this._gpose.IsInGPose || IsChatInputActive())
			return false;

		var flag = state switch {
			VirtualKeyState.Down => HotkeyFlags.OnDown,
			VirtualKeyState.Held => HotkeyFlags.OnHeld,
			VirtualKeyState.Released => HotkeyFlags.OnRelease,
			_ => throw new Exception($"Invalid key state encountered ({state})")
		};

		var hk = GetActiveHotkey(key, flag);
		return hk?.Handler.Invoke() ?? false;
	}
	
	// Check chat state

	private unsafe bool IsChatInputActive() {
		var module = (UIModule*)this._gui.GetUIModule();
		if (module == null) return false;

		var atk = module->GetRaptureAtkModule();
		return atk != null && atk->AtkModule.IsTextInputActive();
	}
}
