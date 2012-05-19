using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SWProject
{
    public class ProjectMath
    {
        public static Random rand = new Random();
        // 
        /// <summary>
        /// permutes the base by Poisson distribution.
        /// NOTE this doesn't work for large numbers.
        /// If you need to distribute around a larger number,
        /// first 
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Poisson(double b)
        {
            double L = Math.Pow(Math.E, -9.0);// 1.0*-1.0);
            double k = 0;
            double p = 1;
            do
            {
                k += 1.0;
                double u = rand.NextDouble();
                p *= u;
            }
            while (p > L);
            double coefficient = k-1;
            return (b * coefficient) / 9.0;
        }

        /// <summary>
        /// Applies a fraction of the Poisson ditribution to the input
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double EstimationError(double i)
        {
            double poisson = Poisson(i);
            double diff = (poisson > i ? poisson - i : i - poisson);
            diff /= 5.0;
            return (poisson > i ? i - diff : i + diff);
        }

    }
    public class Project
    {
        public int totalWorkUnits = 0;
        public int completedWorkUnits = 0;
        public List<Worker> workers = new List<Worker>();
        public int stepsTaken = 0;
        public List<string> log = new List<string>();
        public string name = string.Empty;
        protected Config config = null;
        public Project(Config c)
        {
            config = c;
            name = config.getValue<string>("Project");
            
            Int32 numWorkers = config.getValue<Int32>("Workers");
            for (int j = 0; j < numWorkers; j++)
                workers.Add(new Worker());
            SetupCommunications();
            SetupWork();
        }

        protected void SetupWork()
        {
            Int32 units = config.getValue<Int32>("Units");
            totalWorkUnits = units;

            string errorModel = config.getValue<string>("Effort Estimation Error Model");
            switch (errorModel)
            {
                case "Poisson":
                    {
                        totalWorkUnits = (int)(ProjectMath.EstimationError((double)totalWorkUnits));
                        break;
                    }
                case "None":
                    {
                        break;
                    }
                default: throw new UnknownEstimationErrorModelException(errorModel);
            }
        }
        protected void SetupCommunications()
        {
            string modelName = config.getValue<string>("Communications Overhead Model");
            switch (modelName)
            {
                case "Linear": 
                    {
                        Int32 numWorkers = config.getValue<Int32>("Workers");
                        Double baseValue = config.getValue<Double>("Communications Overhead Base");
                        foreach (Worker w in workers)
                            w.baseCapabilityPerDay = w.initialCapabilityPerDay - (int)(baseValue * workers.Count - 1);
                        break;
                    }
                default: throw new UnknownCommunicationsModelException(modelName);
            }
        }

        /// <summary>
        /// The core, repeating routing for Project!
        /// </summary>
        public void Step()
        {
            foreach (Worker worker in workers)
            {
                foreach(Event e in possibleEvents)
                {
                    bool stop = e.Apply(this, worker);
                    // Stop signal means no other events should apply today - she's already not working, for example.
                    // side effect: events can prevent other events from resolving if they could resolve in parallel. todo.
                    if (stop)
                        break;
                }
                worker.Work(this);
            }
            stepsTaken++;
        }
        public bool Finished
        {
            get
            {
                return totalWorkUnits <= completedWorkUnits;
            }
        }

        public List<Event> possibleEvents = new List<Event>();
    }

    public class Worker
    {
        public int initialCapabilityPerDay = 100;
        public int baseCapabilityPerDay;
        public double currentCapability;
        public string name = randomName();
        public double efficiency = 0.80;

        public Worker()
        {
            baseCapabilityPerDay = initialCapabilityPerDay;
            currentCapability = baseCapabilityPerDay;
        }
        private static string randomName()
        {
            Random r = ProjectMath.rand;
            byte[] buffer = new byte[12];
            r.NextBytes(buffer);
            return System.Convert.ToBase64String(buffer).Substring(0,4);
        }

        /// <summary>
        /// Applies effort to reduce total remaining work on the project.
        /// May choose to do nothing, either once, or permanently.
        /// </summary>
        /// <param name="project"></param>
        public void Work(Project project)
        {
            // currentCapability has been altered by events, use it as it is
            currentCapability *= efficiency;
            project.completedWorkUnits += (int)currentCapability;

            // now reset capability
            currentCapability = baseCapabilityPerDay;
        }
    }

    public class Event
    {
        public static Random random = new Random();

        /// <summary>
        /// This method decides whether to apply the event to this worker,
        /// based on worker, environment, and external factors.
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        public virtual bool Apply(Project p, Worker w)
        {
            return false;
        }
    }

    /// <summary>
    /// Loads and prepares all events.
    /// </summary>
    public class EventInitializer
    {
        protected static double total_absence = 0.0;        
        protected static int daysPerYear = 1;

        public static Config config = new Config();
        public static List<Event> getEvents(Config config)
        {
            EventInitializer.config = config;
            List<Event> events = new List<Event>();
            daysPerYear = config.getValue<int>("Work Days Per Year");
            double baseChanceOfBereavement = config.getValue<double>("Bereavement Chance Per Year");
            double chanceOfBereavement = baseChanceOfBereavement / (double)daysPerYear;
            double baseChanceOfFamilyLeave = config.getValue<double>("Family Chance Per Year");
            double chanceOfFamilyLeave = baseChanceOfFamilyLeave / (double)daysPerYear;
            double baseChanceOfQuit = config.getValue<double>("Quit Chance Per Year");
            double chanceOfQuitLeave = baseChanceOfQuit / (double)daysPerYear;

            events.Add(new AbsenceEvent(eventChance("Sick Days Per Year"), total_absence, "out sick"));
            events.Add(new AbsenceEvent(eventChance("Vacation Days Per Year"), total_absence, "on vacation"));
            events.Add(new AbsenceEvent(eventChance("Extra Holiday Days Per Year"), total_absence, "on holiday"));

            events.Add(new LingeringEvent(chanceOfBereavement, total_absence, "grieving", config.getValue<int>("Bereavement Leave")));
            events.Add(new LingeringEvent(chanceOfFamilyLeave, total_absence, "babysitting", config.getValue<int>("Family Leave")));
            events.Add(new LingeringEvent(chanceOfQuitLeave, total_absence, "terminated", config.getValue<int>("Quit Leave")));

            // other event types:

            // Stress
            // transfers/quits 
            return events;
        }

        protected static double eventChance(string name)
        {
            double dailyChance = 1.0 / ((double)daysPerYear / (double)config.getValue<int>(name));
            return dailyChance;
        }
    }

    public abstract class ChanceEvent : Event
    {
        protected double chance = 0.00001;
        protected double effect = 1.0;

        public ChanceEvent(double chanceIn, double effectIn)
        {
            chance = chanceIn;
            effect = effectIn;
        }
        public override bool Apply(Project p, Worker w)
        {
            if (Event.random.NextDouble() <= chance)
            {
                Triggered(p, w);
                return true;
            }
            return false;
        }

        public abstract void Triggered(Project p, Worker w);
    }
    public class AbsenceEvent : ChanceEvent
    {
        protected string logline = string.Empty;
        public AbsenceEvent(double chanceIn, double effectIn, string loglineIn) : base(chanceIn, effectIn) { logline = loglineIn; }
        public override void Triggered(Project p, Worker w)
        {
            w.currentCapability *= effect;
            p.log.Add("Worker " + w.name + " was " + logline + " on day " + p.stepsTaken + ".");
        }
    }
    public class LingeringEvent : ChanceEvent
    {
        private class Memo { public int daysLeft = 0; public Memo(int d) { daysLeft = d; } }
        private Dictionary<Worker, Memo> affected = new Dictionary<Worker, Memo>();

        protected string logline = string.Empty;
        protected int length = 1;
        public LingeringEvent(double chanceIn, double effectIn, string loglineIn, int lengthIn)
            : base(chanceIn, effectIn)
        {
            logline = loglineIn;
            length = lengthIn;
        }
        public override bool Apply(Project p, Worker w)
        {
            // we check first whether this worker is already affected
            if (affected.ContainsKey(w))
            {
                Memo memo = affected[w];
                memo.daysLeft--;
                if (memo.daysLeft <= 0)
                    affected.Remove(w);
                Triggered(p, w);
            }
            else if (Event.random.NextDouble() <= chance)
            {
                int leave = length - 1;
                affected.Add(w, new Memo(leave));
                Triggered(p, w);
                return true;
            }
            return false;
        }
        public override void Triggered(Project p, Worker w)
        {
            w.currentCapability *= effect;
            int left = 0;
            if (affected.ContainsKey(w))
                left = affected[w].daysLeft;
            left = length - left;
            p.log.Add("Worker " + w.name + " was " + logline + " (" + left + "/" + length + ") on day " + p.stepsTaken + ".");
        }
    }
    public class UnknownCommunicationsModelException : Exception
    {
        public UnknownCommunicationsModelException(string message)
            : base(message)
        { }
    }
    public class UnknownEstimationErrorModelException : Exception
    {
        public UnknownEstimationErrorModelException(string message)
            : base(message)
        { }
    }
}
