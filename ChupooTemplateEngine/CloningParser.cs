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

        private JObject HashTable2JObject(Hashtable data)
        {
            JObject obj = new JObject();
            foreach(DictionaryEntry datum in data)
            {
                obj.Add(datum.Key + "", datum.Value + "");
            }
            return obj;
        }

        internal string Parse(string content)
        {
            content = ViewParser.ReplaceFormattedDataText(content, HashTable2JObject(parent_attributes), false);

            string pattern = @"<c.clone([\w\W]+?)>([\w\W]+?)</c.clone>";
            Match matched = Regex.Match(content, pattern);
            if (matched.Success)
            {
                ReadAttributes(matched.Groups[1].Value);
                string file_name = attributes["json"] + ".json";
                if (attributes["inherit"] != null && attributes["inherit"].ToString() == "true")
                {
                }
                string file_path = Directories.ViewDataJson + file_name;

                if (File.Exists(file_path))
                {
                    Console.WriteLine("Loading JSON file " + file_name + " ...");
                    string json_text = File.ReadAllText(file_path);
                    JArray data = (JArray)JsonConvert.DeserializeObject(json_text);
                    string new_content = "";
                    foreach (JObject datum in (JToken)data)
                    {
                        new_content += ViewParser.ReplaceFormattedDataText(matched.Groups[2].Value, datum);
                    }
                    content = Parser.SubsituteString(content, matched.Index, matched.Length, new_content);
                }
                else
                {
                    Console.WriteLine("Error: JSON file is not found > " + file_name);
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
