using ChupooTemplateEngine.LayoutParsers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ChupooTemplateEngine.Command;

namespace ChupooTemplateEngine
{
    abstract class LayoutParser : Parser
    {
        public void Parse(string dest, string ext, string asset_level, string write_as = null)
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

        protected string RenderPartialLayout(string content)
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

        protected string SeparateLayoutStyle(string content)
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

        protected string SeparateLayoutScript(string content)
        {
            string pattern;
            MatchCollection matches;
            pattern = @"<script.*?src=\""(.*?)\"".*?></script>";
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

        protected string ReplaceAssetUrlText(string content, string asset_level, string component_name = null)
        {
            string pattern = @"<(?:link|script|img|source).*?(?:href|src|poster)=""(\...*?)"".*?>";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                if (CurrentCommand == CommandType.LAUNCH)
                {
                    asset_level = asset_level.Substring(2);
                }
                else if (CurrentCommand == CommandType.RENDER_BACKUP)
                {
                    asset_level = asset_level.Substring(2) + "..";
                }
                else
                {
                    asset_level = asset_level.Substring(2) + "../dev";
                }

                foreach (Match match in matches)
                {
                    string new_value = "";
                    if (match.Groups[1].Value.Substring(0, 2) == "./")
                    {
                        if (CurrentCommand != CommandType.LAUNCH)
                        {
                            new_value = asset_level + match.Groups[1].Value.Substring(1);
                        }
                        else
                        {
                            if (match.Groups[1].Length >= 6 && match.Groups[1].Value.Substring(2, 6) == "assets")
                            {
                                new_value += asset_level + match.Groups[1].Value.Substring(2);
                            }
                            else
                            {
                                string view_asset = asset_level + match.Groups[1].Value.Substring(2);
                                new_value += LaunchViewAssets(view_asset);
                            }
                        }
                    }
                    else if (Regex.Match(match.Groups[1].Value, @"^\.[a-zA-Z0-9-_]+\/").Success)
                    {
                        if (CurrentCommand != CommandType.LAUNCH)
                        {
                            new_value = asset_level + "/layouts/" + component_name + "/" + match.Groups[1].Value.Substring(1);
                        }
                        else
                        {
                            string view_asset = asset_level + component_name + "/" + match.Groups[1].Value.Substring(1);
                            new_value += LaunchViewAssets(view_asset);
                        }
                    }
                    else
                    {
                        new_value = match.Groups[1].Value;
                    }

                    FileInfo finfo = new FileInfo(new_value);
                    if (finfo.Extension == ".js")
                        RegisterUniversalJsFile(new_value);
                    else if (finfo.Extension == ".css")
                        RegisterUniversalCssFile(new_value);
                    else if (!LaunchEngine.IsCodeOnly && CurrentCommand == CommandType.LAUNCH && LaunchEngine.LaunchType == LaunchEngine.LaunchTypeEnum.WORDPRESS)
                        new_value = "<?= get_template_directory_uri() ?>/" + new_value;

                    content = SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                    newLength += new_value.Length - match.Groups[1].Length;
                }
            }
            return content;
        }

        protected string RenderLayoutComponent(string name, string content, string parent_route = null)
        {
            LibParser lp = new LibParser();
            content = lp.Parse(name, content);
            content = RenderPartialLayout(content);
            content = ReplaceAssetUrlText(content, "./", name);
            RenderPartialAssets(name, Directories.Layout, content, true, parent_route);
            content = SeparateLayoutStyle(content);
            content = SeparateLayoutScript(content);
            return content;
        }

        protected string PasteStyles(string content)
        {
            Match matched = Regex.Match(content, @"</head>[\w\W]*?<body.*?>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string item in style_file_list)
                {
                    string _item = item;
                    if (CurrentCommand == CommandType.LAUNCH && LaunchEngine.LaunchType == LaunchEngine.LaunchTypeEnum.WORDPRESS)
                        _item = "<?= get_template_directory_uri() ?>/" + item;
                    appended += "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + _item + "\">\n";
                }
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }

            matched = Regex.Match(content, @"</head>[\w\W]*?<body.*?>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string style in v_style_code_list)
                    appended += style;
                v_style_code_list.Clear();

                foreach (string style in l_style_code_list)
                    appended += style;
                l_style_code_list.Clear();

                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            return content;
        }

        protected string PasteScripts(string content)
        {
            Match matched = Regex.Match(content, @"</body>[\w\W]*?</html>");
            if (matched.Success)
            {
                string appended = "";
                foreach(string item in script_file_list)
                {
                    string _item = item;
                    if (CurrentCommand == CommandType.LAUNCH && LaunchEngine.LaunchType == LaunchEngine.LaunchTypeEnum.WORDPRESS)
                        _item = "<?= get_template_directory_uri() ?>/" + item;
                    appended += "<script type=\"text/javascript\" src=\"" + _item + "\"></script>\n";
                }
                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            matched = Regex.Match(content, @"</body>[\w\W]*?</html>");
            if (matched.Success)
            {
                string appended = "";
                foreach (string script in l_script_code_list)
                    appended += script;
                l_script_code_list.Clear();

                foreach (string script in v_script_code_list)
                    appended += script;
                v_script_code_list.Clear();

                string new_content = appended + matched.Value;
                content = SubsituteString(content, matched.Index, matched.Length, new_content);
            }
            return content;
        }
    }
}
