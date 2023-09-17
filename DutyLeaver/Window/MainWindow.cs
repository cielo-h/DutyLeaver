using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using System;
using System.Numerics;

namespace DutyLeaverPlugin.Windows;

public class MainWindow : Window, IDisposable
{
    protected static float Spacing => ImGui.GetStyle().ItemSpacing.X * ImGuiHelpers.GlobalScale;
    public MainWindow() : base(
        "DutyLeaverConfig", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize |ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
    }

    public void Dispose()
    {
    }
    public override void Draw()
    {
        DrawAutomatically();
        DrawCustomLeaveCommand();
    }
    private void DrawAutomatically()
    {
        bool IsAutomaticallyLeave = DutyLeaver.p.Configuration.IsAutomaticallyLeave;
        bool IsAutomaticallyCommence = DutyLeaver.p.Configuration.IsAutomaticallyCommence;
        if (ImGui.Checkbox("Automatically leave duty when duty completed.", ref IsAutomaticallyLeave))
        {
            DutyLeaver.p.Configuration.IsAutomaticallyLeave = IsAutomaticallyLeave;
            DutyLeaver.p.Configuration.Save();
            if (DutyLeaver.p.Configuration.IsAutomaticallyLeave)
            {
                try { Svc.DutyState.DutyCompleted += DutyLeaver.p.OnDutyComplete; }
                catch (Exception e) { PluginLog.Debug($"{e}"); }
            }
            if (!DutyLeaver.p.Configuration.IsAutomaticallyLeave)
            {
                try
                { Svc.DutyState.DutyCompleted += DutyLeaver.p.OnDutyComplete; }
                catch (Exception e) { PluginLog.Debug($"{e}"); }
            }
        }
        if (DutyLeaver.p.Configuration.IsAutomaticallyLeave)
        {
            DragInt(DutyLeaver.p.Configuration.delaycomplete, x => DutyLeaver.p.Configuration.delaycomplete = x, "Delay(ms)##1", 1, 1, 10000, "%i");
        }
    }
    private void DrawCustomLeaveCommand()
    {
        bool IsCustomizeLeaveCommand = DutyLeaver.p.Configuration.IsCustomizeLeaveCommand;
        string CustomLeaveCommand = DutyLeaver.p.Configuration.leavedutycommand;
        if (ImGui.Checkbox("Custom leave duty command on chat.", ref IsCustomizeLeaveCommand))
        {
            DutyLeaver.p.Configuration.IsCustomizeLeaveCommand = IsCustomizeLeaveCommand;
            DutyLeaver.p.Configuration.Save();
        }
        if (DutyLeaver.p.Configuration.IsCustomizeLeaveCommand)
        {
            if (ImGui.InputText("##1", ref CustomLeaveCommand, 255))
            {
                DutyLeaver.p.Configuration.leavedutycommand = CustomLeaveCommand;
                DutyLeaver.p.Configuration.Save();
            }
            DragInt(DutyLeaver.p.Configuration.delayleavecommand, x => DutyLeaver.p.Configuration.delayleavecommand = x, "Delay(ms)##3", 1, 1, 10000, "%i");
        }
    }
    private static bool SameLine(float spacing)
    {
        if (spacing != 0.0f)
        {
            ImGui.SameLine(0.0f, spacing);
            return true;
        }
        return false;
    }

    private static void Dummy(float spacing = 0.0f)
    {
        if (!SameLine(spacing))
            ImGui.Dummy(ImGui.GetStyle().ItemSpacing * ImGuiHelpers.GlobalScale);
    }

    private void UIElement<T>(Func<T, (bool, T)> function, T value, Action<T> setter, float spacing)
    {
        Dummy(spacing);
        SameLine(spacing);

        var result = function(value);

        if (result.Item1)
        {
            setter(result.Item2);
            DutyLeaver.p.Configuration.Save();
        }
    }

    protected void CheckBox(bool value, Action<bool> setter, string label, float spacing = 0.0f)
        => this.UIElement((param) => { return (ImGui.Checkbox(label, ref param), param); }, value, setter, spacing);

    protected void ColorEdit4(Vector4 value, Action<Vector4> setter, string label, float spacing = 0.0f)
        => this.UIElement((param) => { return (ImGui.ColorEdit4(label, ref param, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar), param); }, value, setter, spacing);

    protected void DragFloat(float value, Action<float> setter, string label, float speed, float min, float max, string format, float spacing = 0.0f)
        => this.UIElement((param) => { return (ImGui.DragFloat(label, ref param, speed, min, max, format, ImGuiSliderFlags.AlwaysClamp), param); }, value, setter, spacing);

    protected void DragFloat(float valueLeft, float valueRight, Action<float> setterLeft, Action<float> setterRight, string label, float speed, float min, float max, string format, float spacing = 0.0f)
    {
        this.UIElement((param) => { return (ImGui.DragFloat($"##{label}", ref param, speed, min, max, format, ImGuiSliderFlags.AlwaysClamp), param); }, valueLeft, setterLeft, spacing);
        this.UIElement((param) => { return (ImGui.DragFloat(label, ref param, speed, min, max, format, ImGuiSliderFlags.AlwaysClamp), param); }, valueRight, setterRight, Spacing);
    }

    protected void DragInt(int value, Action<int> setter, string label, int speed, int min, int max, string format, float spacing = 0.0f)
        => this.UIElement((param) => { return (ImGui.DragInt(label, ref param, speed, min, max, format, ImGuiSliderFlags.AlwaysClamp), param); }, value, setter, spacing);

    protected void DragInt(int valueLeft, int valueRight, Action<int> setterLeft, Action<int> setterRight, string label, int speed, int min, int max, string format, float spacing = 0.0f)
    {
        this.UIElement((param) => { return (ImGui.DragInt($"##{label}", ref param, speed, min, max, format, ImGuiSliderFlags.AlwaysClamp), param); }, valueLeft, setterLeft, spacing);
        this.UIElement((param) => { return (ImGui.DragInt(label, ref param, speed, min, max, format, ImGuiSliderFlags.AlwaysClamp), param); }, valueRight, setterRight, Spacing);
    }
    protected static void Text(string[] texts)
    {
        Dummy();
        foreach (var item in texts)
            ImGui.Text(item);
    }

    protected static void CollapsingHeader(string label, Action action)
    {
        if (ImGui.CollapsingHeader(label, ImGuiTreeNodeFlags.DefaultOpen))
        {
            action();
            Dummy();
        }
    }

    protected static void Tooltip(string message, float width = 420.0f)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(width * ImGuiHelpers.GlobalScale);
            ImGui.Text(message);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }
}
