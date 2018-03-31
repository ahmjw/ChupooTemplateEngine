using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class LaunchingConfig
    {
        public Hashtable Data { get; internal set; }

        public LaunchingConfig()
        {
            Data = new Hashtable();
        }

        internal void Load(string launching_name)
        {
            string c_file = Directories.Launch + "\\" + launching_name + "\\config.ini";
            if (File.Exists(c_file))
            {
                string c_content = File.ReadAllText(c_file);
                MatchCollection mc = Regex.Matches(c_content, @"([a-zA-Z0-9_\.]+)\s=\s(.*)");
                foreach(Match m in mc)
                {
                    Data[m.Groups[1].Value] = m.Groups[2].Value;
                }
            }
        }
    }
}
