using System;
using System.Collections.Generic;
using DiscordWebhook;
using Nightfire_Source_Update_Builder;
using Mono.Options;

namespace Nightfire_Source_Update_Builder
{
    class DiscordNotify
    {
        static public ulong discordId { set; get; }
        static public string discordToken { set; get; }
        static public bool discordSetup { set; get; } /* Specifies whether discord tokens were setup or not */
        static public void SendDiscordPost(string content, ulong id, string token)
        {
            if (!discordSetup)
                return;

            //If the params were validated but there's no content, don't send anything...
            if (content.Length < 1 || content == String.Empty)
            {
                Console.WriteLine("[DiscordNotify]: No content to send was provided, won't send any notifications to discord.");
                return;
            }

            Webhook hook = new Webhook(id, token)
            {
                Content = content
            };
            hook.Send(content);
        }

        static public bool SetupDiscordDetails(string[] args)
        {
            var options = new OptionSet {
                { "discordid=", "The discord id.",  (ulong idParam) => discordId = idParam },
                { "discordtoken=", "The discord token.",  tokenParam => discordToken = tokenParam },
            };

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