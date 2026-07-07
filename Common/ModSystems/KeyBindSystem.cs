using Terraria.ModLoader;

namespace KillYou3000.Common.ModSystems;

public class KeyBindSystem : ModSystem
{
    public static ModKeybind ItemInteractions { get; private set;}
    public static ModKeybind ItemInteractions2 { get; private set;}

    public override void Load()
    {
        ItemInteractions = KeybindLoader.RegisterKeybind(Mod, "ItemInteractions", "Mouse3");
        ItemInteractions2 = KeybindLoader.RegisterKeybind(Mod, "ItemInteractions2", "Mouse2");
    }
}