using System;
using MultiClaw.Core;

namespace MultiClaw
{
    
    public static class Branch
    {

        public static GameBranch Get() => Constants.GetActiveBranch();

        public static bool Is(params BranchType[] types)
        {
            var active = Constants.GetActiveBranch();
            if (active == null) return false;
            foreach (var type in types)
                if (active.IsBranchType(type.ToCore()))
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