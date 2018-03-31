using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ChupooTemplateEngine.Command;

namespace ChupooTemplateEngine.ViewParsers
{
    class HtmlTemplate : ViewParser
    {
        protected override void OnViewParsed(string route, string dest, string asset_level)
        {
            LayoutParsers.HtmlTemplate layoutParser = new LayoutParsers.HtmlTemplate();
            layoutParser.Parse(route, Extension, asset_level, dest);
        }
    }
}
