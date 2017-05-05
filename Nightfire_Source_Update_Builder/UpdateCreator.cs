using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Nightfire_Source_Update_Builder
{

    class UpdateCreator
    {
        private const int minimal_ver = 1;
        public void InitUpdate()
        {
            StartParsingMainDir();
        }

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
        private void DirGenerateCaches(string sDir, int version)
        {
            var chSet = new ChangeSets();
            if (version > minimal_ver)
            {
                //int chVersion = version - 1;
                if (!chSet.LoadIntegrityfile(sDir))
                {
                    Console.WriteLine("No integrity file yet...");
                }
            }

            bool changeSetExists = chSet.DoesChangeSetExist(ChangeSets.CHANGESET_TYPES.CHANGESET_INTEGRITY_OLD);

            try
            {
                IEnumerable<string> files = Directory.EnumerateFiles(sDir, "*.*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    var hashFuncs = new Hashing();
                    string SHA1Hash = hashFuncs.genFileHash(file); //Generate our sha1 file hash which we'll use later on

                    string fileSize = new System.IO.FileInfo(file).Length.ToString();
                    //If the changeset list exists
                    if (changeSetExists)
                    {
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
                var directories = DirectorySearch.GetDirectories(sDir, "*", SearchOption.AllDirectories);
                foreach (string dir in directories)
                {
                    //Add every single directory to our current integrity file
                    chSet.AddToChangeSet(ChangeSets.CHANGESET_TYPES.CHANGESET_INTEGRITY_CURRENT, "", dir, "directory", "0", "add-dir");
                }

                //Check for newly added folders
                if (File.Exists(chSet.genSetName(sDir, "integrity.xml")))
                    chSet.checkForNewlyAddedFolders(chSet.genSetName(sDir, "integrity.xml"), sDir);

                //Check for any deleted files and folders by comparing the last integrity file to the actual directory structure (Adds to the new changeset list only)
                if (File.Exists(chSet.genSetName(sDir, "integrity.xml")))
                    chSet.checkForDeletedFiles_Dirs(chSet.genSetName(sDir, "integrity.xml"));

                //Create the directory (if it doesn't exist) where we'll have our changesets
                System.IO.Directory.CreateDirectory(String.Format("{0}-changesets", sDir));
                
                //Finally
                chSet.WriteChangeSetToXML(chSet.genSetName(sDir, String.Format("changeset_{0}.xml", version))/*String.Format("nightfiresource-changesets/changeset_{0}.xml", version)*/, ChangeSets.CHANGESET_TYPES.CHANGESET_NEW);
                chSet.WriteChangeSetToXML(chSet.genSetName(sDir, "integrity.xml"), ChangeSets.CHANGESET_TYPES.CHANGESET_INTEGRITY_CURRENT);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void StartParsingMainDir()
        {
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