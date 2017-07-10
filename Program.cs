using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace SimCGearEval
{
    class Program
    {
        static List<string> names = new List<string>();
        static List<string> legendaries = new List<string>();
        static bool no_simulation = false;
        static bool test_mode = false;
        static string simc_exe = "";
        static string working_dir = "";
        static string path = "";

        static void ReadConfigurationFile(string name)
        {
            StreamReader reader = new StreamReader(name);
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (line.StartsWith("name"))
                {
                    names.Add(line.Substring(5));
                    continue;
                }
                if (line.StartsWith("nosim"))
                {
                    no_simulation = Boolean.Parse(line.Substring(6));
                    continue;
                }
                if (line.StartsWith("testmode"))
                {
                    test_mode = Boolean.Parse(line.Substring(9));
                    continue;
                }
                if (line.StartsWith("simc"))
                {
                    simc_exe = line.Substring(5);
                    continue;
                }
                if (line.StartsWith("wkdir"))
                {
                    working_dir = line.Substring(6);
                    path = working_dir + "\\";
                    continue;
                }
                if (line.StartsWith("legendary"))
                {
                    legendaries.Add(line.Substring(10));
                    continue;
                }
            }
            reader.Close();
        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Please specify configuration file!");
                return;
            }
            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Configuration file not found!");
                return;
            }
            Console.WriteLine("Reading config {0}", args[0]);
            ReadConfigurationFile(args[0]);
            Console.WriteLine("Starting sim with {0} names", names.Count);
            string input = "";
            string output = "";
            foreach (string name in names)
            {
                input = name + ".simc";
                for (int i = 1; ; ++i)
                {
                    output = name + "_" + i.ToString() + ".txt";
                    if (File.Exists(path + output))
                    {
                        continue;
                    }
                    break;
                }
                Profile profile = new Profile(working_dir, simc_exe, legendaries, test_mode);
                profile.InitializeFromFile(path + input);
                profile.SimulateChar(no_simulation);
                profile.OutputToFile(path + output);
            }
        }
    }
}
