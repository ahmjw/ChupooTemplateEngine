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
    class CloningParser
    {
        private Hashtable attributes = new Hashtable();
        private Hashtable parent_attributes;

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

        public static JObject HashTable2JObject(Hashtable data)
        {
            JObject obj = new JObject();
            if (data != null)
            {
                foreach (DictionaryEntry datum in data)
                {
                    obj.Add(datum.Key + "", datum.Value + "");
                }
            }
            return obj;
        }

        internal string Parse(string content)
        {
            string pattern = @"<c.clone([\w\W]+?)>([\w\W]+?)</c.clone>";
            Match matched = Regex.Match(content, pattern);
            if (matched.Success)
            {
                // Combine data from attributes
                ReadAttributes(matched.Groups[1].Value);
                string file_name = attributes["json"] + ".json";
                //if (file_name == "_talks-div.json")
                //    Console.WriteLine(content);
                attributes.Clear();
                content = ViewParser.ReplaceFormattedDataText(content, HashTable2JObject(parent_attributes), false);

                // Reload content
                pattern = @"<c.clone([\w\W]+?)>([\w\W]+?)</c.clone>";
                matched = Regex.Match(content, pattern);

                ReadAttributes(matched.Groups[1].Value);
                file_name = attributes["json"] + ".json";
                string file_path = Directories.ViewDataJson + file_name;

                if (File.Exists(file_path))
                {
                    MessageController.Show("Loading JSON file " + file_name + " ...");
                    string json_text = File.ReadAllText(file_path);
                    JArray data = (JArray)JsonConvert.DeserializeObject(json_text);
                    string new_content = "";
                    foreach (JObject datum in (JToken)data)
                    {
                        //if (file_name == "_talks-div.json")
                        //    MessageController.Show(matched.Groups[2].Value);
                        new_content += ViewParser.ReplaceFormattedDataText(matched.Groups[2].Value, datum);
                    }
                    content = Parser.SubsituteString(content, matched.Index, matched.Length, new_content);
                }
                else
                {
                    MessageController.Show("Error: JSON file is not found > " + file_name);
                }
            }
            return content;
        }

        internal void SetParentAttributes(Hashtable attributes)
        {
            parent_attributes = attributes;
        }
    }
}
