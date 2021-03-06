﻿using System;
using System.IO;

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
        public static string Repeat(char c, int n)
        {
            return new string(c, n);
        }

        public static void FatalError(string error)
        {
            Console.WriteLine($"[FATAL]: {error}");
            Environment.Exit(1);
        }

        public static void LogInfo(string text, bool exit = false)
        {
            Console.WriteLine($"[INFO]: {text}");

            if (exit)
                Environment.Exit(0);
        }

        public static void FStreamReplace(FileInfo fileStream, string absoluteTargetPath)
        {
            if (File.Exists(absoluteTargetPath))
                File.Delete(absoluteTargetPath);

            fileStream.CopyTo(absoluteTargetPath);
        }
    }
}
