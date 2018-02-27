using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class TextTagParser
    {
        private static Hashtable texts = new Hashtable();

        public static Hashtable GetTextData(string name)
        {
            return (Hashtable)texts[name];
        }

        public static void ClearTextData(string name)
        {
            texts.Remove(name);
        }

        public string Parse(string package_name, string attributes, string content)
        {
            ModuleParser mp = new ModuleParser();
            string output = mp.ParseModule(package_name, attributes);

            string pattern = @"<c.text\[(.*?)\]>([\w\W]+?)</c.text>";
            MatchCollection matches = Regex.Matches(content, pattern);

            if (matches.Count > 0)
            {
                Hashtable data = new Hashtable();
                foreach (Match match in matches)
                {
                    string text_name = match.Groups[1].Value;
                    string text_content = match.Groups[2].Value;
                    data[text_name] = text_content;
                }
                texts[package_name] = data;
                output = PasteData(package_name, attributes, output);
            }
            else
            {
                output = Parser.ReplaceText(@"<c\.content(?:\s*\/)?>(?:<\/c\.content>)?", output, content);
            }

            return output;
        }

        private static string PasteData(string lib_name, string attributes, string mod_content)
        {
            Hashtable textData = GetTextData(lib_name);
            if (textData == null)
            {
                return mod_content;
            }

            string pattern = @"<c\.text\[(.*?)\](?:\s*\/)?>(?:<\/c\.text>)?";
            MatchCollection matches = Regex.Matches(mod_content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string text_name = match.Groups[1].Value;
                    string part_content = textData[text_name] + "";
                    mod_content = Parser.SubsituteString(mod_content, match.Index + newLength, match.Length, part_content);
                    newLength += part_content.Length - match.Length;
                }
            }
            ClearTextData(lib_name);
            return mod_content;
        }
    }
}
