using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters.Types;

public interface ICustomizeEditor {
	public void SetCustomization(CustomizeIndex index, byte value);
	public byte GetCustomization(CustomizeIndex index);

	public ICustomizeBatch Prepare();
}

public interface ICustomizeBatch {
	public ICustomizeBatch SetCustomization(CustomizeIndex index, byte value);
	
	public void Dispatch();
}