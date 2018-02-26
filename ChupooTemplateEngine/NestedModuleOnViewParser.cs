using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class NestedModuleOnViewParser : ViewParser
    {
        public string ParseText(string package_name, string lib_name, string content)
        {
            string pattern = @"<c\.module\[(.+)?\](.*?)(?:\s*\/)?>([\w\W]+)?<\/c\.module>";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string part_content = match.Groups[3].Value;

                    LibParser lp = new LibParser();
                    part_content = lp.Parse(lib_name, part_content);

                    part_content = ReplaceAssetUrlText(part_content, "./", "dev/views/" + package_name + "/" + lib_name + "/");

                    content = SubsituteString(content, match.Groups[3].Index + newLength, match.Groups[3].Length, part_content);
                    newLength += part_content.Length - match.Groups[3].Length;
                }
            }
            return content;
        }

        public override void Parse(string route, string dest)
        {
        }

        public override void LoopViews(string path)
        {
        }
    }
}
