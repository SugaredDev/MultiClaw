namespace MultiClaw.Core
{

    public enum BranchType
    {
        Development,
        Playtest,
        Showcase,
        Demo,
        Release
    }

}

namespace MultiClaw
{

    public enum BranchType
    {

        Development = Core.BranchType.Development,
        Playtest = Core.BranchType.Playtest,
        Showcase = Core.BranchType.Showcase,
        Demo = Core.BranchType.Demo,
        Release = Core.BranchType.Release

    }

    public static class BranchTypeExtensions
    {

        public static Core.BranchType ToCore(this BranchType type) =>(Core.BranchType)type;

    }

}
