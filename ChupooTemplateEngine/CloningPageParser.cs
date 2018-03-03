using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    class CloningPageParser
    {
        private Hashtable attributes = new Hashtable();
        private CloningPage cloningPage;
        private static int index;

        public CloningPageParser()
        {
            index = 0;
        }

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

        public CloningPage Parse(string route, string content)
        {
            cloningPage = new CloningPage();
            cloningPage.Data = new List<JToken>();
            string pattern = @"<c.clone-page([\w\W]+?)>([\w\W]+?)</c.clone-page>";
            Match matched = Regex.Match(content, pattern);
            if (matched.Success)
            {
                JArray data = (JArray)JsonConvert.DeserializeObject(matched.Groups[2].Value);
                foreach (JToken datum in (JToken)data)
                {
                    cloningPage.Data.Add(datum);
                }
                ReadAttributes(matched.Groups[1].Value);
                content = Parser.SubsituteString(content, matched.Index, matched.Length, "");
            }
            else
            {
                pattern = @"<c\.clone-page([\w\W]+?)(?:\s*\/)?>(?:<\/c\.clone-page>)?";
                matched = Regex.Match(content, pattern);
                if (matched.Success)
                {
                    ReadAttributes(matched.Groups[1].Value);
                    string file_name = attributes["json"] + ".json";
                    string file_path = Directories.ViewDataJson + file_name;

                    if (File.Exists(file_path))
                    {
                        Console.WriteLine("Loading JSON file " + file_name + " ...");
                        string json_text = File.ReadAllText(file_path);
                        JArray data = (JArray)JsonConvert.DeserializeObject(json_text);
                        foreach (JToken datum in (JToken)data)
                        {
                            cloningPage.Data.Add(datum);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error: JSON file is not found > " + file_name);
                    }
                    content = Parser.SubsituteString(content, matched.Index, matched.Length, "");
                }
            }
            cloningPage.Name = route;
            cloningPage.Content = content;
            return cloningPage;
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

        internal CloningPage ApplyData(CloningPage _cloningPage, JObject data)
        {
            string name = attributes["as"] + "";
            name = ViewParser.ReplaceFormattedDataText(name, data);

            CloningPage __cloningPage = new CloningPage();
            __cloningPage.Index = ++index;
            __cloningPage.Name = _cloningPage.Name;
            __cloningPage.Content = _cloningPage.Content;

            JObject info = new JObject();
            info.Add("index", __cloningPage.Index);
            info.Add("name", __cloningPage.Name);

            if (data["index"] == null)
                data.Add("index", __cloningPage.Index);
            if (data["name"] == null)
                data.Add("name", __cloningPage.Name);

            __cloningPage.NewName = ReplaceFormattedDataText(name, info);
            __cloningPage.Content = ReplaceFormattedDataText(__cloningPage.Content, info);
            return __cloningPage;
        }

        private string ReplaceFormattedDataText(string content, JObject data)
        {
            string pattern = @"\{\{\$page\.([a-zA-Z0-9_-]+)\}\}";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string new_value = data[match.Groups[1].Value] + "";
                    content = Parser.SubsituteString(content, match.Index + newLength, match.Length, new_value);
                    newLength += new_value.Length - match.Length;
                }
            }
            return content;
        }
    }
}
