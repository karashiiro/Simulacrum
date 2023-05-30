using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Simulacrum;

public class CustomizationWindow : Window
{
    public Vector3 Translation => _translation;
    public Vector3 Scale => _scale;
    public Vector4 Color => _color;

    private Vector3 _translation;
    private Vector3 _scale;
    private Vector4 _color;

    public CustomizationWindow(ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(
        "Simulacrum", flags, forceMainWindow)
    {
        _translation = new Vector3(1, 1, 1);
        _scale = new Vector3(1, 1, 0);
        _color = new Vector4(1, 1, 1, 1);
    }

    public override void Draw()
    {
        ImGui.InputFloat3("Translation", ref _translation);
        ImGui.InputFloat3("Scale", ref _scale);
        ImGui.ColorPicker4("Overlay", ref _color);
    }
}