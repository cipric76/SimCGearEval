using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimCGearEval
{
    class DescendingComparer : IComparer<double>
    {
        public int Compare(double x, double y)
        {
            return -Comparer<double>.Default.Compare(x, y);
        }
    }

    public class Profile
    {
        // Constants
        static string simc_params_test = " iterations=10 threads=6";
        static string simc_params_100 = " iterations=100 threads=6";
        static string simc_params_1000 = " iterations=1000 threads=6";

        List<string> otherLines = new List<string>();
        List<string> equipedSet = new List<string>();
        List<string> heads = new List<string>();
        List<string> necks = new List<string>();
        List<string> shoulders = new List<string>();
        List<string> backs = new List<string>();
        List<string> chests = new List<string>();
        List<string> wrists = new List<string>();
        List<string> hands = new List<string>();
        List<string> waists = new List<string>();
        List<string> legs = new List<string>();
        List<string> feets = new List<string>();
        List<string> fingers = new List<string>();
        List<string> trinkets = new List<string>();
        SortedList<string, Tuple<double, List<string>>> bestValues = new SortedList<string, Tuple<double, List<string>>>();
        Tuple<double, List<string>>[] bestSets;
        int set_count = int.MaxValue;
        string path = "";
        string simc_exe = "";
        string working_dir = "";
        List<string> legendaries = new List<string>();
        bool test_mode = false;
        int requested_set_count = 10;

        public void AddToList(string line, ref List<string> list, int condition = 0)
        {
            list.Add(line);
            if (list.Count <= condition + 1)
            {
                if (line.StartsWith("finger"))
                {
                    string finger = line.Substring(0, 6) + list.Count.ToString() + line.Substring(6);
                    equipedSet.Add(finger);
                }
                else if (line.StartsWith("trinket"))
                {
                    string trinket = line.Substring(0, 7) + list.Count.ToString() + line.Substring(7); ;
                    equipedSet.Add(trinket);
                }
                else
                {
                    equipedSet.Add(line);
                }
            }
        }

        public void SetWorkingDir(string wkdir)
        {
            working_dir = wkdir;
            path = working_dir + "\\";
        }

        public void InitializeFromFile(string file)
        {
            StreamReader reader = new StreamReader(file);
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (line.StartsWith("head"))
                {
                    AddToList(line, ref heads);
                    continue;
                }
                if (line.StartsWith("neck"))
                {
                    AddToList(line, ref necks);
                    continue;
                }
                if (line.StartsWith("shoulders"))
                {
                    AddToList(line, ref shoulders);
                    continue;
                }
                if (line.StartsWith("back"))
                {
                    AddToList(line, ref backs);
                    continue;
                }
                if (line.StartsWith("chest"))
                {
                    AddToList(line, ref chests);
                    continue;
                }
                if (line.StartsWith("wrists"))
                {
                    AddToList(line, ref wrists);
                    continue;
                }
                if (line.StartsWith("hands"))
                {
                    AddToList(line, ref hands);
                    continue;
                }
                if (line.StartsWith("waist"))
                {
                    AddToList(line, ref waists);
                    continue;
                }
                if (line.StartsWith("legs"))
                {
                    AddToList(line, ref legs);
                    continue;
                }
                if (line.StartsWith("feet"))
                {
                    AddToList(line, ref feets);
                    continue;
                }
                if (line.StartsWith("finger"))
                {
                    AddToList(line, ref fingers, 1);
                    continue;
                }
                if (line.StartsWith("trinket"))
                {
                    AddToList(line, ref trinkets, 1);
                    continue;
                }
                otherLines.Add(line);
            }
            reader.Close();
        }

        bool WriteTempFile(string temp_file, List<string> gear)
        {
            int legendary_count = 0;
            StreamWriter writer = new StreamWriter(path + temp_file);
            foreach (string line in otherLines)
            {
                writer.WriteLine(line);
            }
            foreach (string line in gear)
            {
                foreach (string legendary in legendaries)
                {
                    if (line.Contains(legendary))
                    {
                        legendary_count++;
                    }
                }
                writer.WriteLine(line);
            }
            writer.Flush();
            writer.Close();
            return (legendary_count < 3);
        }

        double SimulateSet(string temp_file)
        {
            try
            {
                Process simCProc = new Process();
                simCProc.StartInfo.FileName = simc_exe;
                if (test_mode)
                {
                    simCProc.StartInfo.Arguments = temp_file + simc_params_test;
                }
                else if (set_count > 2000)
                {
                    simCProc.StartInfo.Arguments = temp_file + simc_params_100;
                }
                else
                {
                    simCProc.StartInfo.Arguments = temp_file + simc_params_1000;
                }
                simCProc.StartInfo.WorkingDirectory = working_dir;
                simCProc.StartInfo.UseShellExecute = false;
                simCProc.StartInfo.RedirectStandardOutput = true;
                simCProc.EnableRaisingEvents = true;
                double dps_val = -1;

                simCProc.Start();

                while (!simCProc.HasExited)
                {
                    string line = simCProc.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        line = line.Trim();
                        if (line.StartsWith("DPS: ") && dps_val < 0)
                        {
                            int endpos = line.IndexOf(" ", 6);
                            string dps = line.Substring(5, endpos - 5);
                            Console.WriteLine(dps);
                            dps_val = Double.Parse(dps);
                        }
                    }
                }

                simCProc.StandardOutput.Close();
                return dps_val;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred!!!: " + ex.Message);
                return -1;
            }
        }

        double CalculateDPS(int set, List<string> gear, bool update_best = true)
        {
            string temp_file = "test_" + set.ToString() + ".simc";
            double dps = -1;
            if (WriteTempFile(temp_file, gear))
            {
                dps = SimulateSet(temp_file);
                if (update_best)
                {
                    UpdateBest(dps, gear);
                }
            }
            File.Delete(path + temp_file);
            return dps;
        }

        void UpdateBest(double dps, List<string> gear)
        {
            foreach (string g in gear)
            {
                string key = g;
                if (g.StartsWith("finger"))
                {
                    key = "finger" + g.Substring(7);
                }
                if (g.StartsWith("trinket"))
                {
                    key = "trinket" + g.Substring(8);
                }
                if (bestValues.ContainsKey(key))
                {
                    if (bestValues[key].Item1 < dps)
                    {
                        bestValues[key] = Tuple.Create(dps, gear);
                    }
                }
                else
                {
                    bestValues[key] = Tuple.Create(dps, gear);
                }
            }
            if (dps > bestSets[requested_set_count - 1].Item1)
            {
                for (int i = requested_set_count - 1; i >= 0; --i)
                {
                    if (i == 0 || dps < bestSets[i - 1].Item1)
                    {
                        bestSets[i] = Tuple.Create(dps, gear);
                        break;
                    }
                    else
                    {
                        bestSets[i] = bestSets[i - 1];
                    }
                }
            }
        }

        public void OutputToFile(string file)
        {
            StreamWriter writer = new StreamWriter(file);

            writer.WriteLine("Item bests:");
            double dps_bar = bestSets[requested_set_count - 1].Item1;
            foreach (var entry in bestValues)
            {
                writer.WriteLine("{0} - {1}({2})", entry.Key, entry.Value.Item1, entry.Value.Item1 >= dps_bar ? "in best sets" : "in other sets");
            }
            writer.WriteLine("==================");
            writer.WriteLine("");
            double equiped_dps = CalculateDPS(0, equipedSet, false);
            writer.WriteLine("Equiped Set DPS: {0}", equiped_dps);
            foreach (string gear in equipedSet)
            {
                writer.WriteLine(gear);
            }
            writer.WriteLine("==================");
            writer.WriteLine("");
            writer.WriteLine("Best sets:");
            for (int i = 0; i < requested_set_count; ++i)
            {
                writer.WriteLine("Set: {0} DPS: {1}", i + 1, bestSets[i].Item1);
                foreach (string gear in bestSets[i].Item2)
                {
                    writer.WriteLine(gear);
                }
                writer.WriteLine("-----------------------");
            }
            writer.WriteLine("==================");
            SortedList<double, List<string>> otherSets = new SortedList<double, List<string>>(new DescendingComparer());
            foreach (var entry in bestValues)
            {
                if (entry.Value.Item1 >= dps_bar)
                {
                    continue;
                }
                if (otherSets.ContainsKey(entry.Value.Item1))
                {
                    continue;
                }
                otherSets[entry.Value.Item1] = new List<string>();
                foreach (string gear in entry.Value.Item2)
                {
                    otherSets[entry.Value.Item1].Add(gear);
                }
            }
            writer.WriteLine("");
            writer.WriteLine("Other sets:");
            foreach (var entry in otherSets)
            {
                writer.WriteLine("DPS: {0}", entry.Key);
                foreach (string gear in entry.Value)
                {
                    writer.WriteLine(gear);
                }
                writer.WriteLine("-----------------------");
            }
            writer.WriteLine("==================");
            writer.WriteLine("");
            writer.Flush();
            writer.Close();
        }

        public void SimulateChar(bool no_simulation)
        {
            set_count = heads.Count * necks.Count * shoulders.Count * backs.Count * chests.Count * wrists.Count * hands.Count * waists.Count * legs.Count * feets.Count * (fingers.Count * (fingers.Count - 1) / 2) * (trinkets.Count * (trinkets.Count - 1) / 2);
            Console.WriteLine("Sets to compute: {0}", set_count);
            if (no_simulation)
            {
                return;
            }
            int set = 1;
            foreach (string head in heads)
            {
                foreach (string neck in necks)
                {
                    foreach (string shoulder in shoulders)
                    {
                        foreach (string back in backs)
                        {
                            foreach (string chest in chests)
                            {
                                foreach (string wrist in wrists)
                                {
                                    foreach (string hand in hands)
                                    {
                                        foreach (string waist in waists)
                                        {
                                            foreach (string leg in legs)
                                            {
                                                foreach (string feet in feets)
                                                {
                                                    for (int ring1 = 0; ring1 < fingers.Count; ++ring1)
                                                    {
                                                        string finger1 = fingers[ring1].Substring(0, 6) + "1" + fingers[ring1].Substring(6);
                                                        for (int ring2 = ring1 + 1; ring2 < fingers.Count; ++ring2)
                                                        {
                                                            string finger2 = fingers[ring2].Substring(0, 6) + "2" + fingers[ring2].Substring(6);
                                                            for (int trinket1 = 0; trinket1 < trinkets.Count; ++trinket1)
                                                            {
                                                                string trink1 = trinkets[trinket1].Substring(0, 7) + "1" + trinkets[trinket1].Substring(7); ;
                                                                for (int trinket2 = trinket1 + 1; trinket2 < trinkets.Count; ++trinket2)
                                                                {
                                                                    string trink2 = trinkets[trinket2].Substring(0, 7) + "2" + trinkets[trinket2].Substring(7); ;
                                                                    Console.WriteLine("{0} / {1}", set, set_count);
                                                                    List<string> gear = new List<string>();
                                                                    gear.Add(head);
                                                                    gear.Add(neck);
                                                                    gear.Add(shoulder);
                                                                    gear.Add(back);
                                                                    gear.Add(chest);
                                                                    gear.Add(wrist);
                                                                    gear.Add(hand);
                                                                    gear.Add(waist);
                                                                    gear.Add(leg);
                                                                    gear.Add(feet);
                                                                    gear.Add(finger1);
                                                                    gear.Add(finger2);
                                                                    gear.Add(trink1);
                                                                    gear.Add(trink2);
                                                                    CalculateDPS(set, gear);
                                                                    set++;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public Profile(string wkdir = "", string simc = "", List<string> lego_list = null, bool testmode = false)
        {
            working_dir = wkdir;
            simc_exe = simc;
            legendaries = lego_list;
            test_mode = testmode;
            requested_set_count = test_mode ? 3 : 10;
            bestSets = new Tuple<double, List<string>>[requested_set_count];
            for (int i = 0; i < requested_set_count; ++i)
            {
                bestSets[i] = Tuple.Create(-1.0, new List<string>());
            }
        }

    }
}
