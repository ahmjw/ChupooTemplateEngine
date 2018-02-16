using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class LayoutParser : Parser
    {
        private string SeparateLayoutStyle(string content)
        {
            string pattern;
            MatchCollection matches;
            pattern = @"<link.*?rel=""stylesheet"".*?>";
            matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    l_style_file_list.Add(match.Value);
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            pattern = @"<style.*?>[\w\W]*?</style>";
            matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    l_style_code_list.Add(match.Value);
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            return content;
        }

        public void ParseLayout(string dest, string asset_level)
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

        private string RenderPartialLayout(string content)
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
                        part_content = RenderLayoutComponent(layout_name, part_content);
                        content = SubsituteString(content, match.Index + newLength, match.Length, part_content);
                        newLength += part_content.Length - match.Length;
                    }
                    else
                    {
                        layout_file = Directories.Layout + layout_name + @"\main.html";
                        if (File.Exists(layout_file))
                        {
                            string part_content = File.ReadAllText(layout_file);
                            part_content = RenderLayoutComponent(layout_name, part_content);
                            content = SubsituteString(content, match.Index + newLength, match.Length, part_content);
                            newLength += part_content.Length - match.Length;
                        }
                        else
                        {
                            Console.WriteLine("Warning: Partial layout " + layout_name + ".html is not found");
                        }
                    }
                }
            }
            return content;
        }

        private string SeparateLayoutScript(string content)
        {
            string pattern;
            MatchCollection matches;
            pattern = @"<script.*?></script>";
            matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    l_script_file_list.Add(match.Value);
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            pattern = @"<script.*?>[\w\W]*?</script>";
            matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                foreach (Match match in matches)
                {
                    l_script_code_list.Add(match.Value);
                    content = SubsituteString(content, match.Index + newLength, match.Length, "");
                    newLength += -match.Length;
                }
            }
            return content;
        }

        private string RenderLayoutComponent(string name, string content, string parent_route = null)
        {
            content = RenderPartialLayout(content);
            RenderPartialAssets(name, Directories.Layout, content, true, parent_route);
            content = SeparateLayoutStyle(content);
            content = SeparateLayoutScript(content);
            return content;
        }

        private string PasteStyles(string content)
        {
            Match matched = Regex.Match(content, @"</head>[\w\W]*?<body.*?>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string style in l_style_file_list)
                    appended += style;
                foreach (string style in v_style_file_list)
                    appended += style;
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            matched = Regex.Match(content, @"</head>[\w\W]*?<body.*?>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string style in v_style_code_list)
                    appended += style;
                foreach (string style in l_style_code_list)
                    appended += style;
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            ClearStyles();
            return content;
        }

        private string PasteScripts(string content)
        {
            Match matched = Regex.Match(content, @"</body>[\w\W]*?</html>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string script in l_script_file_list)
                    appended += script;
                foreach (string script in v_script_file_list)
                    appended += script;
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            matched = Regex.Match(content, @"</body>[\w\W]*?</html>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string script in l_script_code_list)
                    appended += script;
                foreach (string script in v_script_code_list)
                    appended += script;
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            ClearScripts();
            return content;
        }
    }
}
