using System;
using System.Security.Cryptography;
using System.IO;

namespace Nightfire_Source_Update_Builder
{
    class Hashing
    {
        public byte[] getfileHash(string filePath)
        {
            using (var sha1 = SHA1.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return sha1.ComputeHash(stream);
                }
            }
        }

        public string concatFileHash(byte[] fileHash)
        {
            string SHA1 = String.Empty;
            for (int i = 0; i < fileHash.Length; i++)
            {
                SHA1 += fileHash[i];
            }
            return SHA1;
        }

        public string genFileHash(string filePath)
        {
            byte[] _fileHash = getfileHash(filePath);
            return concatFileHash(_fileHash);
        }
    }
}
