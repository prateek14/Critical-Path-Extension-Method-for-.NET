﻿//
//	CPM - Critical Path Method C# Sample Application
//	Copyright ©2006 Leniel Braz de Oliveira Macaferi & Wellington Magalhães Leite.
//
//  UBM COMPUTER ENGINEERING - 7TH TERM [http://www.ubm.br/]
//  This program sample was developed and turned in as a term paper for Lab. of
//  Software Engineering.
//  The source code is provided "as is" without warranty.
//

using System;
using System.Linq;
using System.Text;
using System.Threading;
using CriticalPathMethod;
using System.Collections.Generic;

namespace ComputerEngineering
{
    class CPM
    {

        private static void Main(string[] args)
        {
            // Array to store the activities that'll be evaluated.
            var activities = GetActivities();
            Output(activities.Shuffle().CriticalPath(p => p.Predecessors, l => (long)l.Duration));

            // This should create an infinite loop
            activities.First().Predecessors.Add(activities.Last());
            CheckForLoops(activities);
            activities.First().Predecessors.Remove(activities.Last());
            activities.Skip(1).First().Predecessors.Add(activities.Skip(activities.Count() - 2).First());
            CheckForLoops(activities.Shuffle());
        }

        private static void CheckForLoops(IEnumerable<Activity> activities) {
            var isCaughtProperly = false;
            var thread = new System.Threading.Thread(
                () =>
                    {
                        try {
                            activities.CriticalPath(p => p.Predecessors, l => (long)l.Duration);
                        }
                        catch (System.InvalidOperationException ex) {
                            System.Console.WriteLine("Found problem: " + ex.Message);
                            isCaughtProperly = true;
                        }                        
                    }
                );
            thread.Start();
            for (var i = 0; i < 100; i++) {
                Thread.Sleep(100); // Wait for 10 seconds - our thread should finish by then
                if (thread.ThreadState != ThreadState.Running)
                    break;
            }
            if(thread.ThreadState ==ThreadState.Running)
                thread.Abort();
            System.Console.WriteLine(isCaughtProperly
                                         ? "Critical path caught the loop"
                                         : "Critical path did not find the loop properly");
        }

        private static void Output(IEnumerable<Activity> list)
        {
            var sb = new StringBuilder();
            Console.Write("\n          Critical Path: ");
            var totalDuration = 0L;
            foreach (Activity activity in list) {
                Console.Write("{0} ", activity.Id);
                sb.AppendFormat("{0} ", activity.Id);
                totalDuration += activity.Duration;
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append("\r\n" + totalDuration);
            var output = System.IO.File.ReadAllText("output.txt");
            Console.Write("\n\n         Total duration: {0}\n\n", totalDuration);
            System.Diagnostics.Debug.Assert(sb.ToString().CompareTo(output.Trim()) == 0);
        }

        /// <summary>
        /// Gets the activities that'll be evaluated by the critical path method.
        /// </summary>
        /// <param name="list">Array to store the activities that'll be evaluated.</param>
        /// <returns>list</returns>
        private static IEnumerable<Activity> GetActivities()
        {
            var list = new List<Activity>();
            var input = System.IO.File.ReadAllLines("input.txt");
            var ad = new Dictionary<string, Activity>();
            var deferredList = new Dictionary<Activity, List<string>>();
            Console.Write("\n       Number of activities: " + input.Length);

            int inx = 0;
            foreach (var line in input) {
                var activity = new Activity();
                var elements = line.Split(' ');
                Console.WriteLine("\n                Activity {0}\n", inx + 1);

                activity.Id = elements[0];
                Console.WriteLine("\n                     ID: " + activity.Id);
                ad.Add(activity.Id, activity);
                activity.Description = elements[1];
                Console.WriteLine("            Description: " + activity.Description);

                activity.Duration = int.Parse(elements[2]);
                Console.WriteLine("               Duration: " + activity.Duration);

                int np = int.Parse(elements[3]);
                Console.WriteLine(" Number of predecessors: ", np);

                if (np != 0) {
                    var allIds = new List<string>();
                    for (int j = 0; j < np; j++) {
                        allIds.Add(elements[4 + j]);
                        Console.WriteLine("    #{0} predecessor's ID: " + elements[4 + j], j + 1);
                    }

                    if (allIds.Any(i => !ad.ContainsKey(i))) {
                        // Defer processing on this one
                        deferredList.Add(activity, allIds);
                    }
                    else {
                        foreach (var id in allIds) {
                            var aux = ad[id];

                            activity.Predecessors.Add(aux);
                        }
                    }
                }
                list.Add(activity);
            }

            while (deferredList.Count > 0) {
                var processedActivities = new List<Activity>();
                foreach (var activity in deferredList) {
                    if (activity.Value.Where(ad.ContainsKey).Count() == activity.Value.Count) {
                        // All dependencies are now loaded
                        foreach (var id in activity.Value) {
                            var aux = ad[id];

                            activity.Key.Predecessors.Add(aux);
                        }
                        processedActivities.Add(activity.Key);
                    }
                }
                foreach (var activity in processedActivities) {
                    deferredList.Remove(activity);
                }                
            }

            return list;
        }
    }
}
