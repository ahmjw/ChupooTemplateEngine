using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class FileIo
    {
        public static string GetHtmlContent(string path)
        {
            string content = File.ReadAllText(path);
            content = Regex.Replace(content, @"<!--[\w\W]*?-->", "");
            return content;
        }
    }
}
