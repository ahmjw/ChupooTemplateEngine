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
    class LibParser
    {
        Hashtable attributes = new Hashtable();

        private void ReadAttributes(string text)
        {
            string pattern = @"([a-zA-Z0-9_-]+)\=""([^""]+)""";
            MatchCollection matches = Regex.Matches(text, pattern);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    attributes[match.Groups[1].Value] = match.Groups[2].Value;
                }
            }
        }

        public string Parse(string content)
        {
            string pattern = @"<c\.lib\[(.+)?\](.*?)(?:\s*\/)?>(?:<\/c\.lib>)?";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string lib_name = match.Groups[1].Value;
                    ReadAttributes(match.Groups[2].Value);
                    string lib_dir = Directories.Lib + lib_name;
                    string lib_file = lib_dir + "\\main.html";
                    if (File.Exists(lib_file))
                    {
                        RegisterAssets(lib_name, lib_dir);
                        string part_content = File.ReadAllText(lib_file);
                        part_content = ReplaceAttributes(part_content);

                        content = Parser.SubsituteString(content, match.Index + newLength, match.Length, part_content);
                        newLength += part_content.Length - match.Length;
                    }
                }
            }
            return content;
        }

        private void RegisterAssets(string name, string lib_dir)
        {
            string path = lib_dir + "\\main.css";
            string url = "../modules/libs/" + name + "/main.css";
            Parser.RegisterCssFile(path, url);
        }

        private string ReplaceAttributes(string content)
        {
            string pattern = @"\{\{([a-zA-Z0-9_-]+)\}\}";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string part_content = attributes[match.Groups[1].Value] + "";

                    content = Parser.SubsituteString(content, match.Index + newLength, match.Length, part_content);
                    newLength += part_content.Length - match.Length;
                }
            }
            return content;
        }
    }
}
