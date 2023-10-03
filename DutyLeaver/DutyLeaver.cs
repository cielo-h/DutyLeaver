using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DutyLeaverPlugin.Windows;
using ECommons;
using ECommons.DalamudServices;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Dalamud.Game.Command.CommandInfo;

namespace DutyLeaverPlugin;

internal class DutyLeaver : IDalamudPlugin, IDisposable
{
    internal static DutyLeaver p;
    private delegate void LeaveDutyDelegate(char is_timeout);

    private const string commandName = "/ql";
    private const string commandconfigName = "/dl";
    private LeaveDutyDelegate leaveDungeon;

    private bool requesting = false;

    public string Name => "DutyLeaver";

    [PluginService]
    private static DalamudPluginInterface Interface { get; set; }
    [PluginService]
    private static ICommandManager CommandManager { get; set; }
    [PluginService]
    private static IFramework Framework { get; set; }
    private MainWindow DutyLeaverConfig { get; init; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem = new("DutyLeaver");

    public DutyLeaver()
    {
        p = this;
        Configuration = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(Interface);
        ECommonsMain.Init(Interface, this);
        IntPtr LeaveDuty = Svc.SigScanner.ScanText("40 53 48 83 ec 20 48 8b 05 ?? ?? ?? ?? 0f b6 d9");
        leaveDungeon = Marshal.GetDelegateForFunctionPointer<LeaveDutyDelegate>(LeaveDuty);
        Framework.Update += new IFramework.OnUpdateDelegate(OnFrameworkUpdate);
        CommandManager.AddHandler(commandName, new CommandInfo(new HandlerDelegate(OnCommand))
        {
            HelpMessage = "Leave duty without confirmation window."
        });
        CommandManager.AddHandler(commandconfigName, new CommandInfo(new HandlerDelegate(OnCommand))
        {
            HelpMessage = "Toggle DutyLeaver config window."
        });
        DutyLeaverConfig = new MainWindow();
        WindowSystem.AddWindow(DutyLeaverConfig);
        Interface.UiBuilder.Draw += DrawUI;
        Interface.UiBuilder.OpenConfigUi += DrawConfigUI;
        if(p.Configuration.IsAutomaticallyLeave) Svc.DutyState.DutyCompleted += OnDutyComplete;
        Svc.Chat.ChatMessage += OnCustomizeCommand;
    }
    public void Dispose()
    {
        ECommonsMain.Dispose();
        Svc.DutyState.DutyCompleted -= OnDutyComplete;
        Svc.Chat.ChatMessage -= OnCustomizeCommand;
        DutyLeaverConfig.Dispose();
        this.WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(commandName);
        CommandManager.RemoveHandler(commandconfigName);
        Framework.Update -= new IFramework.OnUpdateDelegate(OnFrameworkUpdate);
    }
    private void OnCommand(string command, string args)
    {
        switch (command)
        {
            case "/ql":
                requesting = true;
                break;
            case "/dl":
                DutyLeaverConfig.Toggle();
                break;
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            if (requesting)
            {
                leaveDungeon('\0');
                requesting = false;
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex.Message, Array.Empty<object>());
        }
    }

    private void DrawUI()
    { this.WindowSystem.Draw(); }

    private void DrawConfigUI()
    { DutyLeaverConfig.Toggle(); }

    internal async void OnDutyComplete(object? sender, ushort e)
    {
        if (p.Configuration.IsAutomaticallyLeave && Svc.Condition[ConditionFlag.BoundByDuty56])
        {
            await Task.Delay(p.Configuration.delaycomplete);
            if (p.Configuration.IsAutomaticallyLeave && Svc.Condition[ConditionFlag.BoundByDuty56])
            {
                requesting = true;
            }
        }
    }
    internal void OnCustomizeCommand(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (message.ToString() == p.Configuration.leavedutycommand && p.Configuration.IsCustomizeLeaveCommand && !Svc.Condition[ConditionFlag.InCombat] && Svc.Condition[ConditionFlag.BoundByDuty56])
        {
            OnCustomLeavecommand();
        }
    }

    internal async void OnCustomLeavecommand()
    {
        await Task.Delay(p.Configuration.delayleavecommand);
        if (p.Configuration.IsCustomizeLeaveCommand && !Svc.Condition[ConditionFlag.InCombat] && Svc.Condition[ConditionFlag.BoundByDuty56]) 
        {
            requesting = true;
        }
    }
}
