using System.ComponentModel;
using Terraria.ModLoader.Config;
using KillYou3000.Items.Accessories;

namespace KillYou3000.Config;

public class ClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    public override void OnLoaded() => ProgressiveFishingRod.ClientConfig = this;

    [Range(-1f, 1.5f)]
    [Increment(.1f)]
    [DefaultValue(.5f)]
    public float PullingDelay;
}

public class ServerConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;
    public override void OnLoaded() => ProgressiveFishingRod.ServerConfig = this;
    
    [Range(0, 1000)]
    [Increment(1)]
    [DefaultValue(10)]
    public int MaximumNumberOfFishingLines;
    
    [Range(0, 10000)]
    [Increment(1)]
    [DefaultValue(100)]
    public int MaximumFishingCapacity;
    
}