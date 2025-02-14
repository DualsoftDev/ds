using System;

namespace Dual.Common.Core
{
    // System.Version 이 serialize 가 잘 안되어, 대체용
    public class VersionEx
    {
        private VersionEx() {}     // for JSON
        public VersionEx(int major, int minor, int build, int revision)
        {
            Major = major;
            Minor = minor;
            Build = build;
            Revision = revision;
        }
        public VersionEx(Version version)
        {
            Major = version.Major;
            Minor = version.Minor;
            Build = version.Build;
            Revision = version.Revision;
        }
        public VersionEx(VersionEx version)
        {
            Major = version.Major;
            Minor = version.Minor;
            Build = version.Build;
            Revision = version.Revision;
        }

        public int Major { get; set; }
        public int Minor { get; set; }
        public int Build { get; set; }
        public int Revision { get; set; }
        public override string ToString()
        {
            return $"{Major}.{Minor}.{Build}";
        }
    }
}
