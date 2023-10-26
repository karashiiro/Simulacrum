using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;
using Simulacrum.Drawing;

namespace Simulacrum;

public class DebugWindow : Window
{
    private readonly MaterialScreenManager _materialScreens;
    private readonly MediaSourceManager _mediaSources;

    private TextureWrap? _currentView;

    public DebugWindow(MaterialScreenManager materialScreens, MediaSourceManager mediaSources) : base(
        "Simulacrum Debug")
    {
        _materialScreens = materialScreens;
        _mediaSources = mediaSources;

        IsOpen = true;
    }

    public override void Draw()
    {
        ImGui.Columns(2);

        ImGui.Text("Screens");
        ImGuiTable.DrawTable("Screens", _materialScreens.ScreenEntries, kvp =>
        {
            var (id, screen) = kvp;

            ImGui.TableSetColumnIndex(0);
            if (ImGui.Button("Copy ID###SimulacrumCopyScreenId"))
            {
                Clipboard.SetText(id);
            }

            ImGui.TableSetColumnIndex(1);
            if (ImGui.Button("Show###SimulacrumShowScreen"))
            {
                _currentView = screen.ImGuiTextureWrap;
            }

            ImGui.TableSetColumnIndex(2);
            ImGui.Text(id);

            var location = screen.GetLocation();

            ImGui.TableSetColumnIndex(3);
            ImGui.Text($"{location.World}");

            ImGui.TableSetColumnIndex(4);
            ImGui.Text($"{location.Territory}");

            ImGui.TableSetColumnIndex(5);
            ImGui.Text($"{location.Position.ToVector3()}");
        }, ImGuiTableFlags.Resizable, "", "", "ID", "World", "Territory", "Position");

        ImGui.Spacing();

        ImGui.Text("Media Sources");
        ImGuiTable.DrawTable("Media Sources", _mediaSources.MediaSourceEntries, kvp =>
        {
            var (id, mediaSource) = kvp;

            ImGui.TableSetColumnIndex(0);
            if (ImGui.Button("Copy ID###SimulacrumCopyMediaSourceId"))
            {
                Clipboard.SetText(id);
            }

            ImGui.TableSetColumnIndex(1);
            ImGui.Text(id);

            ImGui.TableSetColumnIndex(2);
            ImGui.Text($"{mediaSource.Size()}");

            ImGui.TableSetColumnIndex(3);
            ImGui.Text($"{mediaSource.PixelSize()}");
        }, ImGuiTableFlags.Resizable, "", "ID", "Size", "Pixel Size");

        ImGui.NextColumn();

        if (_currentView != null)
        {
            ImGui.Image(_currentView.ImGuiHandle, new Vector2(_currentView.Width, _currentView.Height));
        }
    }
}