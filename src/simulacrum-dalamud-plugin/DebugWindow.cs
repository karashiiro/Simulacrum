using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;
using Simulacrum.Playback;

namespace Simulacrum;

public class DebugWindow : Window
{
    private readonly ScreenManager _screens;
    private readonly MediaSourceManager _mediaSources;

    private TextureWrap? _currentView;

    public DebugWindow(ScreenManager screens, MediaSourceManager mediaSources) : base(
        "Simulacrum Debug")
    {
        _screens = screens;
        _mediaSources = mediaSources;

        IsOpen = true;
    }

    public override void Draw()
    {
        ImGui.Columns(2);

        ImGui.Text("Screens");
        ImGuiTable.DrawTable("Screens", _screens.ScreenEntries, kvp =>
        {
            var (id, screen) = kvp;

            ImGui.TableSetColumnIndex(0);
            if (ImGui.Button("Copy ID###SimulacrumCopyScreenId"))
            {
                Clipboard.SetText(id);
            }

            ImGui.TableSetColumnIndex(1);
            if (screen is MaterialScreen materialScreen && ImGui.Button("Show###SimulacrumShowScreen"))
            {
                _currentView = materialScreen.ImGuiTextureWrap;
            }

            ImGui.TableSetColumnIndex(2);
            ImGui.Text(id);

            var location = screen.GetLocation();

            ImGui.TableSetColumnIndex(3);
            ImGui.Text($"{location.Territory}");

            ImGui.TableSetColumnIndex(4);
            ImGui.Text($"{location.Position.ToVector3()}");
        }, ImGuiTableFlags.Resizable, "", "", "ID", "Territory", "Position");

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