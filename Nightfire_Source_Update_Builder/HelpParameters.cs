using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;
using Nightfire_Source_Update_Builder;

namespace Nightfire_Source_Update_Builder
{
    class HelpParameters
    {
        static public bool shouldShowHelp = false;
        // these are the available options, note that they set the variables
        public static OptionSet options = new OptionSet {
                { "h|help", "show this message and exit", h => { shouldShowHelp = h != null; } },
        };
        //All available options
        public static OptionSet[] AvailableOptions = new[] {
            HelpParameters.options,
            DiscordNotify.options,
            CloudflarePurge.options,
            BuildCache.options,
            UpdateCreator.options,
        };
        /* Shows help for all available parameters */
        public static void showCommandParametersHelp()
        {
            Console.WriteLine("Help:\n");
            foreach (OptionSet opt in AvailableOptions)
            {
                opt.WriteOptionDescriptions(Console.Out);
            }
        }
        public static bool RunHelpParams(string[] args)
        {
            List<string> extra;
            try
            {
                // parse the command line
                extra = options.Parse(args);

                if (shouldShowHelp)
                {
                    showCommandParametersHelp();
                    return false;
                }
            }
            catch (OptionException e)
            {
                // output some error message
                Console.WriteLine("Didn't receive any valid parameters. ");
                Console.WriteLine("Try `--help' for more information.");
                return false;
            }
            return true;
        }
    }
}

public static partial class Hooks
{
    public static bool RunHelpParams(string[] args)
    {
        return HelpParameters.RunHelpParams(args);
    }
}