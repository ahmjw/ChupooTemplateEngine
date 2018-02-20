using ChupooTemplateEngine.LayoutParsers;
using System;
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
        public abstract void Parse(string dest, string asset_level);

        protected abstract string RenderPartialLayout(string content);

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
                    asset_level = asset_level.Substring(2) + "../modules";
                }

                foreach (Match match in matches)
                {
                    string new_value = "";
                    if (match.Groups[1].Value.Substring(0, 2) == "./")
                    {
                        if (CurrentCommand != CommandType.LAUNCH)
                            new_value = asset_level + match.Groups[1].Value.Substring(1);
                        else
                        {
                            if (match.Groups[1].Length >= 6 && match.Groups[1].Value.Substring(2, 6) == "assets")
                            {
                                if (this is Wordpress)
                                    new_value += "<?= get_template_directory_uri() ?>/" + asset_level + match.Groups[1].Value.Substring(2);
                                else
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
                    content = SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                    newLength += new_value.Length - match.Groups[1].Length;
                }
            }
            return content;
        }

        protected string RenderLayoutComponent(string name, string content, string parent_route = null)
        {
            LibParser lp = new LibParser();
            content = lp.Parse(content);
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
                foreach (string style in l_style_file_list)
                    appended += style;
                foreach (string style in v_style_file_list)
                {
                    appended += style;
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
                foreach (string style in l_style_code_list)
                    appended += style;
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
            return content;
        }
    }
}
