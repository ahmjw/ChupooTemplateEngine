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
        public override void Parse(string dest, string asset_level, string write_as = null)
        {
            Match matched = Regex.Match(dest, @"^_([^\\]+)\\.*?$");
            if (matched.Success)
            {
                dest = matched.Groups[1].Value;
                asset_level = "./";
            }
            if (dest[0] == '_')
            {
                MessageController.Show("Rendering route " + dest + " was skipped");
                return;
            }

            string layout_content = "";
            string path = Directories.Layout + cfg_layout_name + ".html";
            if (!File.Exists(path))
            {
                MessageController.Show("Warning: " + dest + " >> Layout file is not found: " + cfg_layout_name + ".html");
                cfg_layout_name = "page";
                path = Directories.Layout + cfg_layout_name + ".html";
                if (File.Exists(path))
                {
                    MessageController.Show("\tChange layout to " + cfg_layout_name + ".html");
                }
                else
                {
                    MessageController.Show("Warning: " + dest + " >> Layout file is not found: " + cfg_layout_name + ".html");
                    return;
                }
            }

            Directories.Current = Directories.Layout;
            layout_content = File.ReadAllText(path);

            LibParser lp = new LibParser();
            layout_content = lp.Parse(dest, layout_content);

            ModuleParser mp = new ModuleParser();
            layout_content = mp.Parse(layout_content);

            layout_content = RenderPartialLayout(layout_content);
            layout_content = RenderLayoutComponent(cfg_layout_name, layout_content);

            layout_content = PasteScripts(layout_content);
            layout_content = PasteStyles(layout_content);
            string pattern = @"<c\.content(?:\s*\/)?>(?:<\/c\.content>)?";
            layout_content = ReplaceText(pattern, layout_content, view_content);

            string p_dir = "";
            matched = Regex.Match(dest, "^(.*?)\\?[a-zA-Z0-9-_]+$");
            if (matched.Success)
            {
                p_dir = matched.Groups[1].Value;
            }
            if (Directory.Exists(Directories.Public + p_dir))
            {
                string ext = ".html";
                string p_file;
                if (write_as == null)
                {
                    p_file = Directories.Public + dest + ext;
                }
                else
                {
                    p_file = Directories.Public + write_as + ext;
                    dest = write_as;
                }
                File.WriteAllText(p_file, layout_content);
                MessageController.Show("OK: " + dest + ext);
            }
            else
            {
                MessageController.Show("Error: Layout directory " + p_dir + " is not found");
            }
        }

        protected override string RenderPartialLayout(string content)
        {
            string pattern = @"<c\.import\sname=""(.+)?""(?:\s*\/)?>(?:<\/c\.import>)?";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    string layout_name = "_" + match.Groups[1].Value;
                    string layout_file = Directories.Layout + layout_name + ".html";

                    if (File.Exists(layout_file))
                    {
                        string part_content = File.ReadAllText(layout_file);
                        LibParser lp = new LibParser();
                        part_content = lp.Parse(layout_name, part_content);

                        ModuleParser mp = new ModuleParser();
                        part_content = mp.Parse(part_content);
                        part_content = RenderLayoutComponent(layout_name, part_content);
                        content = SubsituteString(content, match.Index + newLength, match.Length, part_content);
                        newLength += part_content.Length - match.Length;
                    }
                    else
                    {
                        layout_file = Directories.Layout + layout_name.Substring(1) + @"\main.html";
                        if (File.Exists(layout_file))
                        {
                            string part_content = File.ReadAllText(layout_file);
                            part_content = RenderLayoutComponent(layout_name.Substring(1), part_content);
                            content = SubsituteString(content, match.Index + newLength, match.Length, part_content);
                            newLength += part_content.Length - match.Length;
                        }
                        else
                        {
                            MessageController.Show("Warning: Partial layout " + layout_name.Substring(1) + ".html is not found");
                        }
                    }
                }
            }
            return content;
        }
    }
}
