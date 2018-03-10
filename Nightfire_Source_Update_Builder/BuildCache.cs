using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Nightfire_Source_Update_Builder;
using Mono.Options;

namespace Nightfire_Source_Update_Builder
{
    class BuildCache
    {
        private static string BuildName = String.Empty;
        private static string BUILD_NAME_EMPTY_STRING_ERROR_MESSAGE = "Got empty build name, stopping... Available options are master / upcoming, try --help?";
        public static string INVALID_STRING = "Invalid";
        public static bool genDiffsOnly = false;
        public static bool performCompression = true;
        public static string diffsUrl = String.Empty;
        public static string versionFileName = "version.txt";

        // these are the available options, note that they set the variables
        public static OptionSet options = new OptionSet {
                { "r|releasebuild=", "Tells the program which build we're generating, master / upcoming",  n => BuildName = n },
                { "gendiffsonly", "Tells the program to only generate differences",  v => { genDiffsOnly = true;  performCompression = false; setBuildName(genDiffsOnly ? getBuildNameDiffs() : getBuildName()); } },
                { "diffsurl=", "Passes a url to download the changeset from",  n => { diffsUrl = n; } },
        };

        public static bool isBuildNameNullOrEmpty()
        {
            return BuildName.Length < 1;
        }

        public static string getBuildName()
        {
            return !isBuildNameNullOrEmpty() ? BuildName : INVALID_STRING;
        }

        public static string getBuildNameDiffs()
        {
            return $"{BuildName}-diffs";
        }

        public static bool isDiffsURLNullOrEmpty()
        {
            return diffsUrl.Length < 1;
        }

        public static string getDiffsURL()
        {
            return diffsUrl;
        }

        public static void setBuildName(string name)
        {
            BuildName = name;
        }

        public static string getMainCacheDirectory(string mainChSetDir)
        {
            return $"{mainChSetDir}-{getBuildName()}";
        }

        public static string SetupReleaseBuildName(string[] args)
        {
            List<string> extra;
            try
            {
                // parse the command line
                extra = options.Parse(args);

                if (isBuildNameNullOrEmpty())
                {
                    Console.WriteLine(BUILD_NAME_EMPTY_STRING_ERROR_MESSAGE);
                }
            }
            catch (OptionException e)
            {
                // output some error message
                Console.WriteLine("Didn't receive any valid parameters. " + BUILD_NAME_EMPTY_STRING_ERROR_MESSAGE);
                Console.WriteLine("Try `--help' for more information.");
            }
            return getBuildName();
        }

        public static void BuildCacheDirectory(string DirName)
        {
            Directory.CreateDirectory(Path.GetFullPath(DirName)); //Create the dir with the changeset name one directory back
        }

        public static void evalShouldCompress(string fileName, string cacheDirName)
        {
            FileInfo fStream = new FileInfo(fileName);
            if (performCompression)
            {
                Compressor.CompressFile(fStream, cacheDirName);
            }
            else
            {
                string newFilePath = Path.GetFullPath(Path.Combine(cacheDirName, fileName));
                Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                fStream.CopyTo(newFilePath);
            }
        }

        public static void DoByFileEditMode(ChangeSets chSet, ChangeSets.ChangeSetC item, string cacheDirName)
        {
            switch (item.mode)
            {
                case "add":
                    evalShouldCompress(item.filename, cacheDirName);
                    break;
                case "delete":
                    string fPath = Compressor.getFilePathAndCompressionAppended($"{Path.Combine(cacheDirName, item.filename)}");
                    File.Delete(fPath);
                    break;
            }
        }

        public static void DoByDirectoryEditMode(ChangeSets chSet, ChangeSets.ChangeSetC item, string cacheDirName)
        {
            string fPath = Path.Combine(cacheDirName, item.filename);
            switch (item.mode)
            {
                case "add-dir":
                    if (!Directory.Exists(fPath))
                    {
                        if (File.Exists(fPath))
                            File.Delete(fPath); //We can't have a file and a directory with the same name
                        Directory.CreateDirectory(fPath);
                    }
                    break;
                case "delete":
                    Directory.Delete(fPath, true);
                    break;
            }
        }

        public static void handleFileOrDirectory(ChangeSets.ChangeSetC chSetPtr, string cacheDirName)
        {
            ChangeSets cPtr = ChangeSets.getChangeSetsClassPtr();
            switch (chSetPtr.filetype)
            {
                case "file":
                    DoByFileEditMode(cPtr, chSetPtr, cacheDirName);
                    break;
                case "directory": 
                    DoByDirectoryEditMode(cPtr, chSetPtr, cacheDirName); //Directory removals start from C:
                    break;
            }
        }

        public static void GenerateCacheFromChangeset(ChangeSets chSetPtr, bool oldChangesetExists)
        {
            //Evaluate if we're copying files from the new changeset or from integrity... Ofcourse if an old changeset exists it means we incremented...
            ChangeSets.CHANGESET_TYPES type = oldChangesetExists ? ChangeSets.CHANGESET_TYPES.CHANGESET_NEW : ChangeSets.CHANGESET_TYPES.CHANGESET_INTEGRITY_CURRENT;
            
            string cacheDirName = getMainCacheDirectory(chSetPtr.getMainChangeSetDir());
            ChangeSets ptr = ChangeSets.getChangeSetsClassPtr();
            if (ptr.GetChangeSetCount(type) < 1)
            {
                Console.WriteLine($"No changes to add of type \"{ptr.GetChangeSetName(type)}\" into the destination folder {cacheDirName}.");
                return;
            }

            BuildCacheDirectory(cacheDirName);
            foreach (ChangeSets.ChangeSetC chSet in ChangeSets.getAppropriateListForType(type))
            {
                //Begin moving every individual file... Maybe use Path.Combine?
                //Handle files and directories like this for now...
                handleFileOrDirectory(chSet, cacheDirName);
            }
        }
    }
}

public static partial class Hooks
{
    public static bool CouldSetupReleaseBuildName(string[] args)
    {
        return BuildCache.SetupReleaseBuildName(args) != BuildCache.INVALID_STRING;
    }
}