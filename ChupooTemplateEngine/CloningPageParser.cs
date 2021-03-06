﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ChupooTemplateEngine.Command;

namespace ChupooTemplateEngine
{
    class CloningPageParser
    {
        private Hashtable attributes = new Hashtable();
        private CloningPage cloningPage;
        private static int index;

        public static bool SingleLaunch { get; internal set; }

        public CloningPageParser()
        {
            index = 0;
            SingleLaunch = false;
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
            string pattern;
            Match matched;
            bool is_match = false;

            pattern = @"<c\.clone-page([\w\W]+?)>([\w\W]+?)<\/c\.clone-page>?";
            matched = Regex.Match(content, pattern);
            if (matched.Success)
            {
                is_match = true;
                ReadAttributes(matched.Groups[1].Value);
                pattern = "<part>(.*?)</part>";
                MatchCollection mc = Regex.Matches(matched.Groups[2].Value, pattern);
                int i = 0;
                foreach(Match match in mc)
                {
                    JObject datum = new JObject();
                    datum.Add("$page.part", match.Groups[1].Value);
                    cloningPage.Data.Add(datum);
                    i++;
                }

                content = Parser.SubsituteString(content, matched.Index, matched.Length, "");
            }
            
            if (!is_match)
            {
                pattern = @"<c.clone-page([\w\W]+?)>([\w\W]+?)</c.clone-page>";
                matched = Regex.Match(content, pattern);
                if (matched.Success)
                {
                    is_match = true;
                    JArray data = (JArray)JsonConvert.DeserializeObject(matched.Groups[2].Value);
                    foreach (JToken datum in (JToken)data)
                    {
                        cloningPage.Data.Add(datum);
                    }
                    ReadAttributes(matched.Groups[1].Value);

                    content = Parser.SubsituteString(content, matched.Index, matched.Length, "");
                }
            }

            if (!is_match)
            {
                pattern = @"<c\.clone-page([\w\W]+?)(?:\s*\/)?>(?:<\/c\.clone-page>)?";
                matched = Regex.Match(content, pattern);
                if (matched.Success)
                {
                    is_match = true;
                    ReadAttributes(matched.Groups[1].Value);

                    SingleLaunch = CurrentCommand == CommandType.LAUNCH && ViewParser.Extension == ".php" &&
                        attributes["single-launch"] != null && attributes["single-launch"].ToString() == "true";

                    string file_name = attributes["json"] + ".json";
                    string file_path = Directories.ViewDataJson + file_name;

                    if (File.Exists(file_path))
                    {
                        MessageController.Show("Loading JSON file " + file_name + " ...");
                        string json_text = File.ReadAllText(file_path);
                        JArray data = (JArray)JsonConvert.DeserializeObject(json_text);
                        foreach (JToken datum in (JToken)data)
                        {
                            cloningPage.Data.Add(datum);
                        }
                    }
                    else if (attributes["for"] != null)
                    {
                        string[] for_data = attributes["for"].ToString().Split(',');
                        int a = int.Parse(for_data[0]);
                        int b = int.Parse(for_data[1]);
                        int c = int.Parse(for_data[2]);
                        for (int i = a; i <= b; i += c)
                        {
                            cloningPage.Data.Add(new JObject(new JProperty("index", i)));
                        }
                    }
                    else
                    {
                        MessageController.Show("Error: JSON file is not found > " + file_name);
                    }
                    content = Parser.SubsituteString(content, matched.Index, matched.Length, "");
                }
            }

            cloningPage.Name = route;
            cloningPage.Content = content;
            cloningPage.IsCloningPage = is_match;
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

        internal CloningPage ApplyData(string route, CloningPage _cloningPage, JObject data)
        {
            string name = attributes["as"] + "";
            name = ViewParser.ReplaceFormattedDataText(route, name, data);

            CloningPage __cloningPage = new CloningPage();
            __cloningPage.Index = ++index;
            __cloningPage.Name = _cloningPage.Name;
            __cloningPage.Content = _cloningPage.Content;
            if (data["$page.part"] != null)
            {
                __cloningPage.Part = data["$page.part"] + "";
            }

            JObject info = new JObject();
            info.Add("index", __cloningPage.Index);
            info.Add("name", __cloningPage.Name);
            info.Add("part", __cloningPage.Part);

            __cloningPage.NewName = ReplaceFormattedDataText(name, info);
            __cloningPage.Content = ReplaceFormattedDataText(__cloningPage.Content, info);

            if (data["index"] == null)
                data.Add("index", __cloningPage.Index);
            if (data["name"] == null)
                data.Add("name", __cloningPage.Name);
            if (data["part"] == null)
                data.Add("part", __cloningPage.Part);

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
