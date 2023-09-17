using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace DutyLeaverPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool IsAutomaticallyLeave = false;
        public bool IsAutomaticallyCommence = false;
        public bool IsCustomizeLeaveCommand = false;
        public bool IsCustomizeEnterCommand = false;
        public string leavedutycommand = "!leaveduty";
        public string enterdutycommand = "!enterduty";
        public int delaycomplete = 3000;
        public int delaycommence = 1000;
        public int delayleavecommand = 500;
        public int delayentercommand = 500;

        [NonSerialized]
        private DalamudPluginInterface? PluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }

    }
}
