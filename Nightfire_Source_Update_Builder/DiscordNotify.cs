using System;
using System.Collections.Generic;
using DiscordWebhook;
using Nightfire_Source_Update_Builder;
using Mono.Options;
using System.Threading.Tasks;

namespace Nightfire_Source_Update_Builder
{
    class DiscordNotify
    {
        //Discord has a 2000 character limit, 256 is used for overhead.
        public const int DISCORD_LIMIT_LENGTH = 2000-256;
        static public ulong discordId { get; set; }
        static public string discordToken { get; set; }
        static public bool discordSetup { get; set; } /* Specifies whether discord tokens were setup or not */
        static public string discordContent { get; set; }
        static public string discordMoreURL { get; set; }
        public static OptionSet options = new OptionSet {
                { "discordid=", "The discord id.",  (ulong idParam) => discordId = idParam },
                { "discordtoken=", "The discord token.",  tokenParam => discordToken = tokenParam },
                { "discordmoreurl=", "The url to read more about said post.",  urlParam => discordMoreURL = urlParam },
        };
        static public string GetDiscordStringTruncated(string str, int length, out bool truncated)
        {
            truncated = false;
            if (str.Length >= length)
            {
                str = str.Substring(0, length);
                truncated = true;
            }
            return str;
        }

        static public void FormatDiscordPost(string content, string additionalString = "")
        {
            discordContent = GetDiscordStringTruncated(content, DISCORD_LIMIT_LENGTH, out bool truncated);
            discordContent = truncated ? $"{discordContent}...\n{additionalString}" : discordContent;
        }
        static public async Task SendDiscordPost(ulong id, string token)
        {
            if (!discordSetup)
                return;

            //If the params were validated but there's no content, don't send anything...
            if (discordContent.Length < 1 || discordContent == String.Empty)
            {
                Console.WriteLine("[DiscordNotify]: No content to send was provided, won't send any notifications to discord.");
                return;
            }

            Webhook hook = new Webhook(id, token)
            {
                Content = discordContent
            };
            await hook.Send(discordContent);
        }

        static public bool SetupDiscordDetails(string[] args)
        {
            List<string> extra;
            try
            {
                discordSetup = true; //Initialize as true
                // parse the command line
                extra = options.Parse(args);

                if (discordId == 0 || discordToken.Length < 1)
                {
                    Console.WriteLine("WARNING: Couldn't setup discord id or token, won't send any notifications to discord. Try `--help' for more information.");
                    discordSetup = false;
                }
            }
            catch (OptionException e)
            {
                // output some error message
                Console.WriteLine("[DiscordNotify]: Didn't receive any valid parameters.");
                Console.WriteLine("[DiscordNotify]: Try `--help' for more information.");
            }
            return discordSetup;
        }
    }
}

public static partial class Hooks
{
    public static bool CouldSetupDiscordDetails(string[] args)
    {
        return DiscordNotify.SetupDiscordDetails(args) != false;
    }
}