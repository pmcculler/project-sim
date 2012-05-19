using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SWProject
{
    /// <summary>
    /// This class reads in a configuration file.
    /// Others consume it.
    /// </summary>
    public class Config
    {
        // Example line:
        // Project: foo
        protected List<string> lines = new List<string>();
        public Config() { }
        public Config(string fileName)
        {
            using (TextReader reader = new StreamReader(fileName))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    lines.Add(line);
                    line = reader.ReadLine();
                }
            } 
        }

        public int getInteger(string name)
        {  
            return Int32.Parse(getSection(name));
        }

        Dictionary<string, object> cache = new Dictionary<string, object>();

        public T getValue<T>(string name) //where T : class
        {
            if (!cache.ContainsKey(name))
            {
                Type type = typeof(T);
                if (typeof(String) == type)
                    return (T)((object)(getSection(name)));
                T instance = Activator.CreateInstance<T>();
                object answer = type.InvokeMember("Parse", System.Reflection.BindingFlags.Static |
                                                        System.Reflection.BindingFlags.InvokeMethod
                                                        | System.Reflection.BindingFlags.Public
                                                        , null, instance, new object[] { getSection(name) });
                if (answer is T)
                    cache[name] = answer;
                else
                    throw new ConfigurationParameterTypeUnsupportedException(type.ToString());
            }
            return (T)cache[name];
        }
        protected string getSection(string name)
        {
            foreach (string line in lines)
                if (line.StartsWith(name))
                    return line.Substring(name.Length + 2); // +2 accounts for ": "
            throw new ConfigurationParameterNotFoundException(name);
        }
        public class ConfigurationParameterNotFoundException : Exception
        {
            public ConfigurationParameterNotFoundException(string message)
                : base(message)
            { }
        }
        public class ConfigurationParameterTypeUnsupportedException : Exception
        {
            public ConfigurationParameterTypeUnsupportedException(string message)
                : base(message)
            { }
        }
    }
}
