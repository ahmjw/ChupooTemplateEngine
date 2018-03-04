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
        private Hashtable attributes = new Hashtable();

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

        private string RenderNestedContent(string lib_name, string content)
        {
            string pattern = @"<c.lib\[([^\]]+)\]([\w\W]+?)>([\w\W]+?)<\/c\.lib>";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string part_content = match.Groups[3].Value;

                    part_content = ReplaceAttributes(part_content);
                    part_content = Parse(lib_name, part_content);

                    LibParser lp = new LibParser();
                    part_content = lp.Parse(lib_name, part_content);

                    AssetParser ap = new AssetParser("modules", Directories.Module);
                    part_content = ap.Parse(lib_name, part_content);

                    content = Parser.SubsituteString(content, match.Groups[3].Index + newLength, match.Groups[3].Length, part_content);
                    newLength += part_content.Length - match.Groups[3].Length;
                }
            }
            return content;
        }

        public string Parse(string fileName, string content)
        {
            string pattern = @"<c.lib\[([^\]]+)\]([^\/>]+?)>([\w\W]+?)<\/c\.lib>";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count == 0)
            {
                pattern = @"<c\.lib\[(.+)?\]([\w\W]+?)(?:\s*\/)?>(?:<\/c\.lib>)?";
                matches = Regex.Matches(content, pattern);
            }
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string lib_name = match.Groups[1].Value;
                    Console.WriteLine(lib_name);

                    if (!Parser.IsLibExists(lib_name))
                    {
                        Parser.RegisterLib(lib_name);
                    }
                    else
                    {
                        content = Parser.SubsituteString(content, match.Index + newLength, match.Length, "");
                        newLength += -match.Length;
                        continue;
                    }

                    ReadAttributes(match.Groups[2].Value);
                    string lib_dir = Directories.Lib + lib_name.Replace("/", "\\");
                    string lib_file = lib_dir + "\\main.html";
                    if (File.Exists(lib_file))
                    {
                        string part_content = File.ReadAllText(lib_file);
                        part_content = RenderNestedContent(lib_name, part_content);

                        if (match.Groups[3].Value != "")
                        {
                            part_content = Parser.ReplaceText(@"<c\.content(?:\s*\/)?>(?:<\/c\.content>)?", part_content, match.Groups[3].Value);
                        }

                        part_content = ReplaceAttributes(part_content);
                        part_content = Parse(lib_name, part_content);

                        AssetParser ap = new AssetParser("libs", Directories.Lib);
                        part_content = ap.Parse(lib_name, part_content);

                        content = Parser.SubsituteString(content, match.Index + newLength, match.Length, part_content);
                        newLength += part_content.Length - match.Length;
                    }
                    else
                        MessageController.Show("Warning: Library is not found > " + lib_name + " in " + fileName);
                }
            }
            return content;
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
