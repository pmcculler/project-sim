using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SWProject;

namespace SWProject
{
    class Program
    {
        static string STEPS_NAME = "Day";
        string logFileName = @"c:\users\Patrick\Desktop\logfile.csv";
        string projectConfigFileName = "ProjectConfig.txt";

        static void Main(string[] args)
        {
            Program p = new Program();
            p.go();
        }
        public void go()
        {
            Config config = new Config(projectConfigFileName);
            List<Project> statistics = new List<Project>();
            List<Event> events = EventInitializer.getEvents(config);
            Int32 targetRuns = config.getValue<Int32>("Runs");

            for (int i = 0; i < targetRuns; i++)
            {
                Project p = new Project(config);
                p.possibleEvents = events;

                Simulate(p);
                statistics.Add(p);
            }
            Analyze(statistics); 
            
        }

        public void Analyze(List<Project> projects)
        {
            double mean = 0; 
            double minimum = double.MaxValue;
            double maximum = 0; 
            double median  = 0;
            int totalSteps = 0;
            int numberOfProjects = projects.Count;
            int medianPlace = (numberOfProjects + 1) / 2; 
            int i = 0;
            Dictionary<int, int> histogram = new Dictionary<int, int>();

            foreach (Project p in projects)
            {
                int projectSteps = p.stepsTaken;

                totalSteps += projectSteps;
                maximum = projectSteps < maximum ? maximum : projectSteps;
                minimum = projectSteps > minimum ? minimum : projectSteps;
                if (i++ == medianPlace)
                    median = projectSteps;

                if (!histogram.ContainsKey(projectSteps))
                    histogram[projectSteps] = 1;
                else
                    histogram[projectSteps] = histogram[projectSteps] + 1;

                //if (p.log.Count > 0)
                //    System.Console.Out.WriteLine("Project log: ");
                //foreach (string s in p.log)
                //{
                //    System.Console.Out.WriteLine(s);
                //}
            }

            Project exampleProject = projects[0];
            if (exampleProject.log.Count > 0)
                System.Console.Out.WriteLine("Example project log: ");
            foreach (string s in exampleProject.log)
            {
                System.Console.Out.WriteLine(s);
            }

            //PrintHistogram(histogram);
            

            mean = (double)totalSteps / (double)projects.Count;

            System.Console.Out.WriteLine("Ran " + projects.Count + " simulations.");
            System.Console.Out.WriteLine("Minimum number of " + STEPS_NAME + "s was " + minimum + ".");
            System.Console.Out.WriteLine("Mean    number of " + STEPS_NAME + "s was " + mean + ".");
            System.Console.Out.WriteLine("Median  number of " + STEPS_NAME + "s was " + median + ".");
            System.Console.Out.WriteLine("Maximum number of " + STEPS_NAME + "s was " + maximum + ".");
        }
        public int Simulate(Project p)
        {
            int steps = 0;
            while (!p.Finished)
            {
                p.Step();
                steps++;
            }
            //System.Console.Out.WriteLine(STEPS_NAME + "s taken to complete project: " + steps);
            return steps;
        }

        private void PrintHistogram(Dictionary<int, int> histogram)
        {
            StringBuilder sb = new StringBuilder();

            foreach (int key in histogram.Keys)
            {
                sb.AppendLine("" + key + "," + histogram[key]);
            }

            using (StreamWriter outfile = new StreamWriter(logFileName))
            {
                outfile.Write(sb.ToString());
            }
        }
    }
}
