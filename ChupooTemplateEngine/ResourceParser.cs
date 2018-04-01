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
    class ResourceParser
    {
        public static string Parse(string maskPath, Hashtable data)
        {
            if (!File.Exists(maskPath))
            {
                return "";
            }
            string content = FileIo.GetHtmlContent(maskPath);
            if (data != null)
            {
                MatchCollection mc = Regex.Matches(content, @"\{\:([^\}]+)\}");
                if (mc.Count > 0)
                {
                    int newLength = 0;
                    foreach (Match m in mc)
                    {
                        string newContent = data[m.Groups[1].Value] + "";
                        content = Parser.SubsituteString(content, m.Index + newLength, m.Length, newContent);
                        newLength += newContent.Length - m.Length;
                    }
                }

                mc = Regex.Matches(content, @"\{Loop\:([^\}]+)\}([\w\W]+?)\{\/Loop\:[^\}]+\}");
                if (mc.Count > 0)
                {
                    int newLength = 0;
                    foreach (Match m in mc)
                    {
                        string varname = m.Groups[1].Value;
                        string loopContent = m.Groups[2].Value;

                        List<Hashtable> subdata = (List<Hashtable>)data[varname];
                        string loopReplacedContent = "";

                        if (subdata != null)
                        {
                            foreach (Hashtable items in subdata)
                            {
                                string _content = loopContent;
                                foreach (DictionaryEntry item in items)
                                {
                                    _content = Regex.Replace(_content, @"\{Value:" + item.Key + @"\}", item.Value + "");
                                }
                                loopReplacedContent += _content;
                            }
                        }

                        content = Parser.SubsituteString(content, m.Index + newLength, m.Length, loopReplacedContent);
                        newLength += loopReplacedContent.Length - m.Length;
                    }
                }
            }
            return content;
        }
    }
}
