using System;
using MultiClaw.Core;

namespace MultiClaw
{
    
    public static class Version
    {
        
        // MultiClaw
        public static bool IsType(params MultiClaw.VersionType[] types)
        {
            var active = Constants.GetActiveVersion();
            if (active == null) return false;
            foreach (var type in types)
                if (active.IsVersionType(type.ToCore()))
                    return true;
            return false;
        }

        // MultiClaw.Core
        public static bool IsType(params MultiClaw.Core.VersionType[] types)
        {
            var active = Constants.GetActiveVersion();
            if (active == null) return false;
            foreach (var type in types)
                if (active.IsVersionType(type))
                    return true;
            return false;
        }

    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandAttribute : Core.CommandAttribute
    {
        public CommandAttribute() : base() { }
        public CommandAttribute(string name) : base(name) { }
    }

}