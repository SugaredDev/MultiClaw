namespace MultiClaw.Core
{

    public enum VersionType
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

    public enum VersionType
    {

        Development = Core.VersionType.Development,
        Playtest = Core.VersionType.Playtest,
        Showcase = Core.VersionType.Showcase,
        Demo = Core.VersionType.Demo,
        Release = Core.VersionType.Release

    }

    public static class VersionTypeExtensions
    {

        public static Core.VersionType ToCore(this VersionType type) =>(Core.VersionType)type;

    }

}
