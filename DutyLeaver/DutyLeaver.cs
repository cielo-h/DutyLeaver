using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using DutyLeaverPlugin.Windows;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Dalamud.Game.Command.CommandInfo;
using static Dalamud.Game.Framework;
using ClickLib;
using Lumina.Excel.GeneratedSheets;
using ClickLib.Clicks;
using Dalamud.Game.Gui;

namespace DutyLeaverPlugin;

internal class DutyLeaver : IDalamudPlugin, IDisposable
{
    internal static DutyLeaver p;
    private delegate void LeaveDutyDelegate(char is_timeout);

    private const string commandName = "/ql";
    private const string commandcondigName = "/dl";
    private LeaveDutyDelegate leaveDungeon;

    private readonly AddressResolver AddressResolver;

    private bool requesting = false;

    public string Name => "DutyLeaver";

    [PluginService]
    private static DalamudPluginInterface Interface { get; set; }

    [PluginService]
    private static CommandManager CommandManager { get; set; }
    [PluginService]
    private static Framework Framework { get; set; }
    private MainWindow DutyLeaverConfig { get; init; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem = new("DutyLeaver");

    public DutyLeaver()
    {
        p = this;
        Configuration = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(Interface);
        Click.Initialize();
        AddressResolver = new AddressResolver();
        ((BaseAddressResolver)AddressResolver).Setup();
        leaveDungeon = Marshal.GetDelegateForFunctionPointer<LeaveDutyDelegate>(AddressResolver.LeaveDuty);
        Framework.Update += new OnUpdateDelegate(OnFrameworkUpdate);
        CommandManager.AddHandler(commandName, new CommandInfo(new HandlerDelegate(OnCommand))
        {
            HelpMessage = "Leave duty without confirmation window."
        });
        CommandManager.AddHandler(commandcondigName, new CommandInfo(new HandlerDelegate(OnCommand))
        {
            HelpMessage = "Toggle DutyLeaver config window."
        });
        DutyLeaverConfig = new MainWindow();
        ECommonsMain.Init(Interface, this);
        WindowSystem.AddWindow(DutyLeaverConfig);
        Interface.UiBuilder.Draw += DrawUI;
        Interface.UiBuilder.OpenConfigUi += DrawConfigUI;
        if(p.Configuration.IsAutomaticallyLeave) Svc.DutyState.DutyCompleted += OnDutyComplete;
        if (p.Configuration.IsAutomaticallyCommence) Svc.ClientState.CfPop += OnDutyPop;
        Svc.Chat.ChatMessage += OnCustomizeCommand;
    }
    public void Dispose()
    {
        ECommonsMain.Dispose();
        Svc.DutyState.DutyCompleted -= OnDutyComplete;
        Svc.Chat.ChatMessage -= OnCustomizeCommand;
        Svc.ClientState.CfPop -= OnDutyPop;
        DutyLeaverConfig.Dispose();
        this.WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(commandName);
        CommandManager.RemoveHandler(commandcondigName);
        Framework.Update -= new OnUpdateDelegate(OnFrameworkUpdate);
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

    private void OnFrameworkUpdate(Framework framework)
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
            PluginLog.LogError(ex.Message, Array.Empty<object>());
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
    internal async void OnDutyPop(object? sender, ContentFinderCondition e)
    {
        if (p.Configuration.IsAutomaticallyCommence) {
            await Task.Delay(p.Configuration.delaycommence);
            if (p.Configuration.IsAutomaticallyCommence && !Svc.Condition[ConditionFlag.BoundByDuty56] && Svc.Condition[ConditionFlag.WaitingForDutyFinder] && !Svc.Condition[ConditionFlag.WaitingForDuty])
            {
                ClickContentsFinderConfirm.Using(default).Commence();
            }
        }
    }
    internal void OnCustomizeCommand(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (message.ToString() == p.Configuration.leavedutycommand && p.Configuration.IsCustomizeLeaveCommand && !Svc.Condition[ConditionFlag.InCombat] && Svc.Condition[ConditionFlag.BoundByDuty56])
        {
            OnCustomLeavecommand();
        }
        /*if (message.ToString() == p.Configuration.enterdutycommand && p.Configuration.IsCustomizeEnterCommand && !Svc.Condition[ConditionFlag.BoundByDuty56] && Svc.Condition[ConditionFlag.WaitingForDutyFinder] && !Svc.Condition[ConditionFlag.WaitingForDuty])
        {
            OnCustomEntercommand();
        }*/
    }

    internal async void OnCustomLeavecommand()
    {
        await Task.Delay(p.Configuration.delayleavecommand);
        if (p.Configuration.IsCustomizeLeaveCommand && !Svc.Condition[ConditionFlag.InCombat] && Svc.Condition[ConditionFlag.BoundByDuty56]) 
        {
            requesting = true;
        }
    }
    internal async void OnCustomEntercommand()
    {
        await Task.Delay(p.Configuration.delayentercommand);
        if (p.Configuration.IsCustomizeEnterCommand && !Svc.Condition[ConditionFlag.BoundByDuty56] && Svc.Condition[ConditionFlag.WaitingForDutyFinder] && !Svc.Condition[ConditionFlag.WaitingForDuty])
        {
            ClickContentsFinderConfirm.Using(default).Commence();
        }
    }
}
