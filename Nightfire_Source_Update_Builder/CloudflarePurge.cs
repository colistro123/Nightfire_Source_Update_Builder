using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using Mono.Options;

using Nightfire_Source_Update_Builder;
namespace Nightfire_Source_Update_Builder
{
    class CloudflarePurge
    {
        //Definitions (All static since this class only has one instance)
        private static CloudflarePurge classPtr = null; //Initialize to null by default
        private static string CLOUDFLARE_EMPTY_STRING_ERROR_MESSAGE = "Got empty Cloudflare API KEY or E-Mail, stopping... Specify --nocloudflare, try --help?";
        private static string APIEmail = String.Empty;
        private static string APIKey = String.Empty;
        private static bool APIShouldPurgeCloudflare = true; //Specifies whether we should purge cloudflare or not, used in case we want to do tests and don't want to purge the entire site...
        
        // these are the available options, note that they set the variables
        public static OptionSet options = new OptionSet {
                { "nocloudflare", "Tells the program to not purge cloudflare",  v => { APIShouldPurgeCloudflare = false; } },
                { "e|email=", "the cloudflare user email.", n => APIEmail = n },
                { "k|apikey=", "API Key", v => APIKey = v },
        };

        //Use this to access the class pointer / allocate, this is kind of like a Singleton
        public static CloudflarePurge getCloudflarePurgeClassPtr()
        {
            if (classPtr == null)
            {
                classPtr = new CloudflarePurge();
            }
            return classPtr;
        }

        public string getAPIEmail()
        {
            return APIEmail;
        }

        public string getAPIKey()
        {
            return APIKey;
        }

        public bool getAPIShouldPurgeCloudflare()
        {
            return APIShouldPurgeCloudflare;
        }

        public bool IsAPIKeyOrEmailEmpty()
        {
            return (getAPIKey().Length == 0 || getAPIEmail().Length == 0);
        }

        public bool SetupCloudflareCredentials(string[] args) {
            List<string> extra;
            try
            {
                // parse the command line
                extra = options.Parse(args);

                if (getAPIShouldPurgeCloudflare() && IsAPIKeyOrEmailEmpty())
                {
                    Console.WriteLine(CLOUDFLARE_EMPTY_STRING_ERROR_MESSAGE);
                    return false;
                }
            }
            catch (OptionException e)
            {
                // output some error message
                Console.WriteLine("Didn't receive any valid parameters. " + CLOUDFLARE_EMPTY_STRING_ERROR_MESSAGE);
                Console.WriteLine("Try `--help' for more information.");
                return false;
            }
            return true;
        }

        public void PurgeCache(string userEmail, string userAPIkey)
        {
            if (IsAPIKeyOrEmailEmpty())
            {
                Console.WriteLine(CLOUDFLARE_EMPTY_STRING_ERROR_MESSAGE);
                return;
            }

            /* 
             * Thanks to:
                https://chrisbitting.com/2017/01/17/accessing-the-cloudflare-api-in-c/ 
            */

            const string apiEndpoint = "https://api.cloudflare.com/client/v4";

            //let's get our zone ID (we'll need this for other requests
            HttpWebRequest request = WebRequest.CreateHttp(apiEndpoint + "/zones");
            request.Method = "Get";
            request.ContentType = "application/json";
            request.Headers.Add("X-Auth-Email", userEmail);
            request.Headers.Add("X-Auth-Key", userAPIkey);

            string srZoneResult = String.Empty;
            using (WebResponse response = request.GetResponse())
            using (var streamReader = new StreamReader(response.GetResponseStream()))

                srZoneResult = (streamReader.ReadToEnd());

            dynamic zoneResult = JsonConvert.DeserializeObject(srZoneResult);

            if (zoneResult.result != null)
            {
                //get our zoneID
                string zoneId = zoneResult.result[0].id;

                byte[] data = Encoding.ASCII.GetBytes("{\"purge_everything\":true}");

                request = WebRequest.CreateHttp(apiEndpoint + "/zones/" + zoneId + "/purge_cache");
                request.Method = "DELETE";
                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                request.Headers.Add("X-Auth-Email", userEmail);
                request.Headers.Add("X-Auth-Key", userAPIkey);

                using (Stream outStream = request.GetRequestStream())
                {
                    outStream.Write(data, 0, data.Length);
                    outStream.Flush();
                }

                string srPurgeResult = String.Empty;
                using (WebResponse response = request.GetResponse())
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                    srPurgeResult = (streamReader.ReadToEnd());

                dynamic purgeResult = JsonConvert.DeserializeObject(srPurgeResult);

                Console.WriteLine($"Purged cloudflare: {purgeResult.success}");
            }
        }
    }
}

public static partial class Hooks
{
    public static bool SetupCloudflareCredentials(string[] args)
    {
        CloudflarePurge cf = CloudflarePurge.getCloudflarePurgeClassPtr();
        return cf.SetupCloudflareCredentials(args);
    }
}