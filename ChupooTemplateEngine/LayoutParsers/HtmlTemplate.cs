using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChupooTemplateEngine.LayoutParsers
{
    class HtmlTemplate : LayoutParser
    {
        public override void Parse(string dest, string asset_level)
        {
            if (dest[0] == '_')
            {
                Console.WriteLine("Rendering route " + dest + " was skipped");
                return;
            }

            string layout_content = "";
            string path = Directories.Layout + cfg_layout_name + ".html";
            if (!File.Exists(path))
            {
                Console.WriteLine("Warning: " + dest + " >> Layout file is not found: " + cfg_layout_name + ".html");
                cfg_layout_name = "page";
                path = Directories.Layout + cfg_layout_name + ".html";
                if (File.Exists(path))
                {
                    Console.WriteLine("\tChange layout to " + cfg_layout_name + ".html");
                }
                else
                {
                    Console.WriteLine("Warning: " + dest + " >> Layout file is not found: " + cfg_layout_name + ".html");
                    return;
                }
            }

            Directories.Current = Directories.Layout;
            layout_content = File.ReadAllText(path);
            layout_content = RenderPartialLayout(layout_content);
            layout_content = RenderLayoutComponent(cfg_layout_name, layout_content);

            layout_content = PasteScripts(layout_content);
            layout_content = PasteStyles(layout_content);
            layout_content = ReplaceLinkUrlText(layout_content, asset_level);
            layout_content = ReplaceAssetUrlText(layout_content, asset_level, cfg_layout_name);
            string pattern = @"<c\.content(?:\s*\/)?>(?:<\/c\.content>)?";
            layout_content = ReplaceText(pattern, layout_content, view_content);

            string p_dir = "";
            Match matched = Regex.Match(dest, "^(.*?)\\?[a-zA-Z0-9-_]+$");
            if (matched.Success)
            {
                p_dir = matched.Groups[1].Value;
            }
            if (Directory.Exists(Directories.Public + p_dir))
            {
                string p_file = Directories.Public + dest + ".html";
                File.WriteAllText(p_file, layout_content);
                Console.WriteLine("OK: " + dest + ".html");
            }
            else
            {
                Console.WriteLine("Error: Layout directory " + p_dir + " is not found");
            }
        }
    }
}
