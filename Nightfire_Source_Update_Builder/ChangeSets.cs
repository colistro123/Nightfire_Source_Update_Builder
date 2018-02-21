using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Nightfire_Source_Update_Builder
{
    class ChangeSets
    {
        private static ChangeSets classPtr = null; //Initialize to null by default
        public static string mainChangeSetDir = String.Empty;

        public enum CHANGESET_TYPES
        {
            CHANGESET_INTEGRITY_OLD = 0, //If it's an old integrity changeset we're loading
            CHANGESET_NEW = 1,
            CHANGESET_INTEGRITY_CURRENT = 2,
        }

        //Use this to access the class pointer / allocate, this is kind of like a Singleton
        public static ChangeSets getChangeSetsClassPtr(string mainDir = "")
        {
            if (classPtr == null)
            {
                if(mainDir.Length < 1)
                    throw new ArgumentException("Directory to parse (mainDir) must not be null unless mainChangeSetDir is valid.", "mainDir");

                classPtr = new ChangeSets(mainDir);
            }
            return classPtr;
        }

        public ChangeSets(string mainDir)
        {
            if (mainDir.Length < 1)
                throw new ArgumentException("Directory to parse (mainDir) must not be null.", "mainDir");

            classPtr = this; //Set class ptr to this
            setMainChangeSetDir(mainDir); //Setup the main directory we're working on
        }

        public void setMainChangeSetDir(string dirName)
        {
            mainChangeSetDir = dirName;
        }

        public string getMainChangeSetDir()
        {
            return mainChangeSetDir;
        }

        public string genSetName(string dirName, string fileName)
        {
            return $"{dirName}-changesets/{fileName}";
        }
        //Only for the integrity file
        public bool LoadIntegrityfile(string dirName)
        {
            String chSetName = genSetName(dirName, "integrity.xml");
            if (!File.Exists(chSetName))
            {
                return false;
            }

            PopulateArrFromIntegrityFile(chSetName);

            return true;
        }

        public void PopulateArrFromIntegrityFile(string chSetName)
        {
            StringBuilder result = new StringBuilder();
            foreach (XElement level1Element in XElement.Load(chSetName).Elements("ContentFile"))
            {
                string hash = level1Element.Attribute("hash").Value;
                string filename = level1Element.Attribute("filename").Value;
                string filesize = level1Element.Attribute("filesize").Value;
                string mode = level1Element.Attribute("mode").Value;
                string type = level1Element.Attribute("type").Value;

                AddToChangeSet(CHANGESET_TYPES.CHANGESET_INTEGRITY_OLD, hash, filename, type, filesize, mode);
            }
        }

        public bool WasDirDeleted(string dirPath)
        {
            foreach (DeletedDirsC dir in DeletedDirs)
            {
                if (dirPath == dir.directory)
                {
                    return true;
                }
            }
            return false;
        }

        public void checkForNewlyAddedFolders(string filePath, string currentDir)
        {
            Console.WriteLine("Checking for any new directories...");

            var directories = DirectorySearch.GetDirectories(currentDir, "*", SearchOption.AllDirectories);
            foreach (string dir in directories)
            {
                //Check if any of the dirs we're going through are in the old integrity file
                ChangeSets.MatchesResult flags = DoesFileHashMatch(ChangeSets.CHANGESET_TYPES.CHANGESET_INTEGRITY_OLD, dir, "");
                bool matches_dirname = (flags & ChangeSets.MatchesResult.matches_filename) != 0; //See if it matches the dir name
                //If it doesn't match, it means it's new
                if (!matches_dirname)
                {
                    //Add the newly created folder
                    AddToChangeSet(ChangeSets.CHANGESET_TYPES.CHANGESET_NEW, "", dir, "directory", "0", "add-dir");
                }
            }
        }

        public void checkForDeletedFiles_Dirs(string filePath)
        {
            Console.WriteLine("Checking for any deleted files...");
            foreach (XElement level1Element in XElement.Load(filePath).Elements("ContentFile"))
            {
                string xmlHash = level1Element.Attribute("hash").Value;
                string xmlFilePath = Path.Combine(level1Element.Attribute("filename").Value); //Unix-Win path-fixes
                string xmlFileSize = level1Element.Attribute("filesize").Value;
                string xmlMode = level1Element.Attribute("mode").Value;
                string xmlType = level1Element.Attribute("type").Value;

                switch(xmlType)
                {
                    case "file":
                        if (!File.Exists(xmlFilePath))
                            AddToChangeSet(CHANGESET_TYPES.CHANGESET_NEW, xmlHash, xmlFilePath, xmlType, xmlFileSize, "delete");
                        break;
                    case "directory":
                        if (!Directory.Exists(xmlFilePath))
                        {
                            if (!WasDirDeleted(xmlFilePath))
                            {
                                AddToChangeSet(CHANGESET_TYPES.CHANGESET_NEW, xmlHash, xmlFilePath, xmlType, "0", "delete");
                                AddToDeletedDirs(xmlFilePath);
                            }
                        }
                        break;
                }
            }
        }
        public void WriteChangeSetToXML(string file, CHANGESET_TYPES type)
        {
            using (XmlWriter writer = XmlWriter.Create(file, XMLMgr.xmlWriterSettings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("CacheInfo");

                foreach (ChangeSetC chSet in getAppropriateListForType(type))
                {
                    writer.WriteStartElement("ContentFile");

                    writer.WriteAttributeString("hash", chSet.hash);
                    writer.WriteAttributeString("filename", chSet.filename);
                    writer.WriteAttributeString("filesize", chSet.filesize);
                    writer.WriteAttributeString("mode", chSet.mode);
                    writer.WriteAttributeString("type", chSet.filetype);
                    writer.WriteAttributeString("compType", chSet.compressionType);

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        public enum MatchesResult {
            matches_none = 0,
            matches_filename = 1,
            matches_hash = 1 << 1,
            matches_all = -1,
        }

        public static List<ChangeSetC> getAppropriateListForType(CHANGESET_TYPES type)
        {
            List<ChangeSetC> curList = ChangeSetListNew; //Return new one by default if the specified one isn't found
            switch (type)
            {
                case CHANGESET_TYPES.CHANGESET_INTEGRITY_OLD:
                    curList = ChangeSetListIntegrityOld;
                    break;

                case CHANGESET_TYPES.CHANGESET_NEW:
                    curList = ChangeSetListNew;
                    break;

                case CHANGESET_TYPES.CHANGESET_INTEGRITY_CURRENT:
                    curList = ChangeSetListIntegrity;
                    break;
            }
            return curList;
        }
        public MatchesResult DoesCurCHSetFileHashMatchFileInDir(ChangeSetC chSet, string rootDir, string file, string hash)
        {
            MatchesResult result = MatchesResult.matches_none;
            string changesetFilename = chSet.filename.ToLower().Replace("nightfiresource/", "");
            string path = rootDir + changesetFilename;
            if (Path.GetFullPath(file.ToLower()) == Path.GetFullPath(path.ToLower()))
            {
                result |= MatchesResult.matches_filename;
                if (chSet.hash == hash)
                {
                    result |= MatchesResult.matches_hash;
                }
            }
            return result;
        }

        public MatchesResult DoesFileHashMatch(CHANGESET_TYPES type, string file, string hash)
        {
            MatchesResult result = MatchesResult.matches_none;
            foreach (ChangeSetC chSet in getAppropriateListForType(type))
            {
                if (Path.GetFullPath(file.ToLower()) == Path.GetFullPath(chSet.filename.ToLower()))
                {
                    result |= MatchesResult.matches_filename;
                    if (chSet.hash == hash)
                    {
                        result |= MatchesResult.matches_hash;
                        break;
                    }
                }
            }
            return result;
        }
        public int GetChangeSetCount(CHANGESET_TYPES type)
        {
            int count = 0;
            switch (type)
            {
                case CHANGESET_TYPES.CHANGESET_INTEGRITY_OLD:
                    count = ChangeSetListIntegrityOld.Count();
                    break;

                case CHANGESET_TYPES.CHANGESET_NEW:
                    count = ChangeSetListNew.Count();
                    break;

                case CHANGESET_TYPES.CHANGESET_INTEGRITY_CURRENT:
                    count = ChangeSetListIntegrity.Count();
                    break;
            }
            return count;
        }
        public string GetChangeSetName(CHANGESET_TYPES type)
        {
            string name = string.Empty;
            switch (type)
            {
                case CHANGESET_TYPES.CHANGESET_INTEGRITY_OLD:
                    name = "CHANGESET_INTEGRITY_OLD";
                    break;

                case CHANGESET_TYPES.CHANGESET_NEW:
                    name = "CHANGESET_NEW";
                    break;

                case CHANGESET_TYPES.CHANGESET_INTEGRITY_CURRENT:
                    name = "CHANGESET_INTEGRITY_CURRENT";
                    break;
            }
            return name;
        }
        public bool DoesChangeSetExist(CHANGESET_TYPES type)
        {
            return GetChangeSetCount(type) > 0;
        }

        public void AddToDeletedDirs(string dirPath)
        {
            DeletedDirsC data = new DeletedDirsC();
            data.directory = dirPath;
            DeletedDirs.Add(data);
            return;
        }

        public void AddToChangeSet(CHANGESET_TYPES type, string hash, string filename, string filetype, string filesize, string mode)
        {
            ChangeSetC data = new ChangeSetC();
            data.hash = hash;
            data.filename = filename;
            data.filetype = filetype;
            data.filesize = filesize;
            data.mode = mode;
            data.compressionType = Compressor.getCompressionForFileOrDirType(filetype);

            switch (type)
            {
                case CHANGESET_TYPES.CHANGESET_NEW:
                    ChangeSetListNew.Add(data);
                    break;
                case CHANGESET_TYPES.CHANGESET_INTEGRITY_OLD:
                    ChangeSetListIntegrityOld.Add(data);
                    break;
                case CHANGESET_TYPES.CHANGESET_INTEGRITY_CURRENT:
                    ChangeSetListIntegrity.Add(data);
                    break;
            }

            Console.WriteLine("Added {0}:{1}:{2}:{3}.", GetChangeSetName(type), GetChangeSetCount(type), filename, hash);
            return;
        }

        public class ChangeSetC
        {
            public string hash { set; get; } /* SHA CRC file hash */
            public string filename { set; get; }
            public string filetype { set; get; }
            public string filesize { set; get; }
            public string mode { set; get; }
            public string compressionType { set; get; } /* compressionType extension, gz, zip, etc */
        }

        public static List<ChangeSetC> ChangeSetListIntegrityOld = new List<ChangeSetC>();
        public static List<ChangeSetC> ChangeSetListNew = new List<ChangeSetC>();
        public static List<ChangeSetC> ChangeSetListIntegrity = new List<ChangeSetC>();


        public class DeletedDirsC
        {
            public string directory { set; get; }
        }

        public static List<DeletedDirsC> DeletedDirs = new List<DeletedDirsC>();
    }
}