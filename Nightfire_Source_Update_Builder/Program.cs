/*
         Planning:

         - Use cloudflare to save bandwidth :-)
         
         - Changesets will be saved in NightfireSource/nightfiresource-changesets
         - A caches.xml file will contain the id for the last changeset
         - Changeset file names will have the following format changeset_ChangeSetID.xml (changeset_1.xml)
         - Integrity file names will have the following name being integrity.xml
         
         Generating an update
            - Read files (recursively, go into the dirs) from the folder which will contain all the diffs, this is a separate folder
            - Compare against the integrity file (if there is one, if not just add all the files and create integrity.xml)
            - Create the integrity.xml list:
                * <ContentFile hash="13179176312914318240461532918490631599201219143227" filename="nightfiresource\base1.fgd" filesize="339355" mode="add" type="file" />
            - Generate the integrity file which will contain all the files and their hashes.
            - Check if the file mode is either: edit/replace, delete, add, add-dir, etc.
                To do this, compare the last changeset against the content folder and if one of the files isn't there, mark as deleted. 
                    Deletion is all that matters.
            - Keep track of which folders were removed / added, again, check backwards with the integrity.xml file...
         
         Updating:
             - Check if there's a caches.xml locally
                - If there is:
                    - Read from the caches.xml on the server and see if our version id is lower than the one on the server.
                    - If it's lower:
                        - Download the new changeset.
                            - Compare our last local hashes (from the previous update, duh) with the ones on the new change set (but only if it has been updated before, otherwise, check with the downloaded integrity.xml file)
                                - Update appropriately
                - Otherwise
                    - Just download everything since it's a fresh directory or they deleted the changeset and we can't trust it
                    - Place / replace the integrity file in the changesets folder.
         
         Checking integrity:
            - Open the integrity file.
            - Put all the hashes and file paths in a list.
            - Compare hashes file by file recursively.
                - If one of them is different, re-download from the server.
*/

namespace Nightfire_Source_Update_Builder
{
    class Program
    {
        static void Main(string[] args)
        {
            //CompressionTest.BeginCompressDecompTests();
            HookFunctions.RunAll(args); //Run all the functions
#if TRUE
            //Get what SetupCloudflareCredentials returned...
            if ((bool)HookFunctions.GetReturnValueFromFunc("SetupCloudflareCredentials"))
            {
                //What's our target directory name / what are we building?
                if ((bool)HookFunctions.GetReturnValueFromFunc("CouldSetupReleaseBuildName"))
                {
                    var updateObj = new UpdateCreator();
                    updateObj.StartParsingMainDir();
                }
            }
#endif
        }
    }
}