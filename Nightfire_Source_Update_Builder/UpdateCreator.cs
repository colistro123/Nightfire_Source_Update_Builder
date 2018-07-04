using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Mono.Options;

namespace Nightfire_Source_Update_Builder
{

    class UpdateCreator
    {
        private const int minimal_ver = 1;
        static public string NO_BASENAME_ERROR = "No basename was provided, you can provide such using -b or -basename, E.G. -b \"nightfiresource_upcoming\"";
        static public string BaseName = String.Empty;
        public static OptionSet options = new OptionSet {
                { "b|basename=", "The main directory to look into.",  n => { BaseName = n.Length < 1 ? String.Empty : n; } },
        };
        /*
         Loop through the entire directory structure passed by sDir and generate an info.xml containing all the hashes.
         Generating an update
            - Read files (recursively, go into the dirs) from the folder which will contain all the diffs, this is a separate folder
            - Compare against the old change set (if there is one, if not just add all the files and create the changeset_1.xml)
            - Create a list in the following format:
                * <ContentFile hash="13179176312914318240461532918490631599201219143227" filename="nightfiresource\base1.fgd" filesize="339355" mode="add" type="file" />
            - Generate the integrity file which will contain all the files and their hashes.
            - Keep track of deleted / created folders.
        */
        private void DirGenerateCaches(string origDir, int version)
        {
            var chSet = ChangeSets.getChangeSetsClassPtr(origDir);
            var chSetFolderName = BuildCache.getMainCacheDirectory(chSet.getMainChangeSetDir());

            if (version > minimal_ver)
            {
                if (!chSet.LoadIntegrityfile(chSetFolderName))
                {
                    Console.WriteLine("No integrity file yet...");
                }
            }

            bool changeSetExists = chSet.DoesChangeSetExist(ChangeSets.CHANGESET_TYPES.CHANGESET_INTEGRITY_OLD);

            BuildCache.VerifyBootstrapper(chSetFolderName); //First and foremost check the bootstrapper version

            DiscordNotify.FormatDiscordPost($"\ud83d\udd27 Building a new {chSetFolderName} update!");
            DiscordNotify.SendDiscordPost(DiscordNotify.discordId, DiscordNotify.discordToken).Wait();

            try
            {
                IEnumerable<string> files = Directory.EnumerateFiles(origDir, "*.*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    var hashFuncs = new Hashing();
                    string SHA1Hash = hashFuncs.genFileHash(file); //Generate our sha1 file hash which we'll use later on

                    string fileSize = new System.IO.FileInfo(file).Length.ToString();
                    //If the changeset list exists
                    if (changeSetExists)
                    {
                        //Huge perf hit, requires optimization... Although I can't see a better way to optimize this right now...
                        ChangeSets.MatchesResult flags = chSet.DoesFileHashMatch(ChangeSets.CHANGESET_TYPES.CHANGESET_INTEGRITY_OLD, file, SHA1Hash);
                        bool matches_hash = (flags & ChangeSets.MatchesResult.matches_hash) != 0;
                        bool matches_filename = (flags & ChangeSets.MatchesResult.matches_filename) != 0;
                        //If there was a change from the previous changeset to now
                        if (!matches_hash)
                        {
                            //Add the change as an edit if it matches the filename in the hashes file, if not, it means we've added a file.
                            if (matches_filename)
                                chSet.AddToChangeSet(ChangeSets.CHANGESET_TYPES.CHANGESET_NEW, SHA1Hash, file, "file", fileSize, "edit");
                            else
                                chSet.AddToChangeSet(ChangeSets.CHANGESET_TYPES.CHANGESET_NEW, SHA1Hash, file, "file", fileSize, "add");
                        }
                    }
                    else
                    {
                        //If there's no changeset we're on the first revision
                        chSet.AddToChangeSet(ChangeSets.CHANGESET_TYPES.CHANGESET_NEW, SHA1Hash, file, "file", fileSize, "add");
                    }
                    chSet.AddToChangeSet(ChangeSets.CHANGESET_TYPES.CHANGESET_INTEGRITY_CURRENT, SHA1Hash, file, "file", fileSize, "add");
                }

                //Get the directory structure
                var directories = DirectorySearch.GetDirectories(origDir, "*", SearchOption.AllDirectories);
                foreach (string dir in directories)
                {
                    //Add every single directory to our current integrity file
                    chSet.AddToChangeSet(ChangeSets.CHANGESET_TYPES.CHANGESET_INTEGRITY_CURRENT, "", dir, "directory", "0", "add-dir");
                }

                //Check for newly added folders
                if (File.Exists(chSet.genSetName(chSetFolderName, "integrity.xml")))
                    chSet.checkForNewlyAddedFolders(origDir);

                //Check for any deleted files and folders by comparing the last integrity file to the actual directory structure (Adds to the new changeset list only)
                if (File.Exists(chSet.genSetName(chSetFolderName, "integrity.xml")))
                    chSet.checkForDeletedFiles_Dirs(chSet.genSetName(chSetFolderName, "integrity.xml"));

                //Create the directory (if it doesn't exist) where we'll have our changesets
                System.IO.Directory.CreateDirectory($"{chSetFolderName}-changesets");
                
                //Finally
                chSet.WriteChangeSetToXML(chSet.genSetName(chSetFolderName, $"changeset_{version}.xml"), ChangeSets.CHANGESET_TYPES.CHANGESET_NEW);
                chSet.WriteChangeSetToXML(chSet.genSetName(chSetFolderName, "integrity.xml"), ChangeSets.CHANGESET_TYPES.CHANGESET_INTEGRITY_CURRENT);

                /* Once everything's done, call BuildCache.Init to compress and copy the files over to the new directory.
                    Evaluate if we're copying from the new changeset or the entire integrity file, it should always be CHANGESET_NEW since full integrity is added on it at first but whatever...
                */
                BuildCache.GenerateCacheFromChangeset(chSet, changeSetExists);

                //And last but not least, tell cloudflare to get rid of caches...
                CloudflarePurge cf = CloudflarePurge.getCloudflarePurgeClassPtr();
                if (cf.getAPIShouldPurgeCloudflare())
                    cf.PurgeCache(cf.getAPIEmail(), cf.getAPIKey());

                //Optionally, notify on discord
                TryReadAndSendDiscordUpdateChangelog(chSet, chSetFolderName, version);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        void TryReadAndSendDiscordUpdateChangelog(ChangeSets chSet, string sDir, int version)
        {
            //Don't even bother...
            if (!DiscordNotify.discordSetup)
                return;

            //Get the changelog data
            string changeLog = GetVersioningFileData();
            //Format the version filename
            string setName = $"{chSet.genSetName(sDir, $"version_{version}.txt")}";
            //Write the version file
            File.WriteAllText(setName, changeLog);
            //URL
            Uri baseUri = new Uri(DiscordNotify.discordMoreURL);
            Uri fullUri = new Uri(baseUri, setName);
            //Format the post
            DiscordNotify.FormatDiscordPost(String.Format("\ud83d\udd27 An update of version {0} has been released to {1}!{2}{3}",
                version,
                BuildCache.getMainCacheDirectory(chSet.getMainChangeSetDir()),
                Utils.Repeat('\n', 2),
                changeLog), $"Read the the full changelog at: {fullUri}");
            //Send it
            DiscordNotify.SendDiscordPost(DiscordNotify.discordId, DiscordNotify.discordToken).Wait();
        }
        public string GetVersioningFileData()
        {
            String fileData = String.Empty;
            if (File.Exists(BuildCache.versionFileName))
            {
                var lines = File.ReadLines(BuildCache.versionFileName, Encoding.UTF8);
                foreach (var line in lines)
                {
                    fileData += $"{line}\n";
                }
            }
            return fileData.Length > 0 ? fileData : "No changelog was provided.";
        }
        public void MoveIntoProjectDir(string dirName)
        {
            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            Directory.SetCurrentDirectory(dirName); //Move into the directory
        }
        public void StartParsingMainDir(string[] args)
        {
            List<string> extra;
            try
            {
                // parse the command line
                extra = options.Parse(args);

                if (BaseName.Length < 1)
                {
                    Console.WriteLine(NO_BASENAME_ERROR);
                }
            }
            catch (OptionException e)
            {
                // output some error message
                Console.WriteLine("Didn't receive any valid parameters. " + NO_BASENAME_ERROR);
                Console.WriteLine("Try `--help' for more information.");
            }

            MoveIntoProjectDir(BaseName); //Move into the directory

            var xmlFuncs = new XMLMgr();
            if (!File.Exists("caches.xml"))
            {
                Console.WriteLine("caches.xml doesn't exist.");
                return;
            }

            string outID, outVersion;

            Dictionary<String, String> dirToParse = xmlFuncs.ReadFromCacheFile("caches.xml"); //Read our caches file, provided it is there
            var firstElement = dirToParse.FirstOrDefault();
            outID = firstElement.Key;
            outVersion = firstElement.Value;

            if (outID.Length != 0 && Directory.Exists(outID))
            {
                DirGenerateCaches(outID, Convert.ToInt32(outVersion));
            }
            else
            {
                Console.WriteLine("An error occured while trying to read caches.xml or the directory doesn't exist!");
            }
        }
    }
}