using System;

namespace Nightfire_Source_Update_Builder
{
    class Utils
    {
        /*
         * Simply checks the platform and returns its equivalent slash type.
         * Maybe just do multi-platform stuff later on and delete the stuff below since it's not even used, leaving as a snippet...
        */
        public string CheckPlatform_ReturnPathSlashType()
        {
            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            string slash;
            switch (pid)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    slash = "\\";
                    break;
                case PlatformID.Unix:
                    slash = "/";
                    break;
                default:
                    slash = "/"; //Assume it's unix since anything that fallsthrough Win32NT is Win anyway...
                    break;
            }
            return slash;
        }
    }
}
