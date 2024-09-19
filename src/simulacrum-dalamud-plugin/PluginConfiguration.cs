using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace Simulacrum;

public class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; }

    [JsonIgnore] private IDalamudPluginInterface? _pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
    }

    public void Save()
    {
        ArgumentNullException.ThrowIfNull(_pluginInterface);
        _pluginInterface.SavePluginConfig(this);
    }
}