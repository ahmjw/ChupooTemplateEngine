using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static ChupooTemplateEngine.Command;
using static ChupooTemplateEngine.Route;

namespace ChupooTemplateEngine
{
    class Parser
    {
        protected static string view_content = "";
        protected string cfg_layout_name = "page";
        protected static List<string> script_file_list = new List<string>();
        protected static List<string> style_file_list = new List<string>();
        protected static List<string> v_script_file_list = new List<string>();
        protected static List<string> v_style_file_list = new List<string>();
        protected static List<string> v_script_code_list = new List<string>();
        protected static List<string> v_style_code_list = new List<string>();
        protected static List<string> l_script_file_list = new List<string>();
        protected static List<string> l_style_file_list = new List<string>();
        protected static List<string> l_script_code_list = new List<string>();
        protected static List<string> l_style_code_list = new List<string>();

        protected string ReplaceText(string pattern, string content, string replacement)
        {
            Match matched = Regex.Match(content, pattern);
            if (matched.Success)
            {
                content = SubsituteString(content, matched.Index, matched.Length, replacement);
            }
            return content;
        }

        public static string SubsituteString(string OriginalStr, int index, int length, string subsituteStr)
        {
            return new StringBuilder(OriginalStr).Remove(index, length).Insert(index, subsituteStr).ToString();
        }

        public static void ClearStyles()
        {
            style_file_list.Clear();
            l_style_file_list.Clear();
            l_style_code_list.Clear();
            v_style_file_list.Clear();
            v_style_code_list.Clear();
        }

        public static void ClearScripts()
        {
            script_file_list.Clear();
            v_script_file_list.Clear();
            v_script_code_list.Clear();
            l_script_file_list.Clear();
            l_script_code_list.Clear();
        }

        public static void ClearAll()
        {
            ClearStyles();
            ClearScripts();
        }

        protected void RenderPartialAssets(string route, string dir, string view_content, bool is_component = false, string parent_route = null)
        {
            string v_dir = dir;
            if (parent_route != null)
            {
                v_dir = dir + parent_route + @"\";
            }
            else if (route[0] != '_' && Directory.Exists(dir + parent_route))
            {
                route = "_" + route + @"\";
                is_component = true;
            }

            string path;
            if (is_component)
            {
                if (route[route.Length - 1] == '\\')
                    path = v_dir + route + @"main.css";
                else
                    path = v_dir + route + @"\main.css";
            }
            else
                path = v_dir + route + ".css";
            if (File.Exists(path))
            {
                string asset_url = "./" + path.Replace(Directories.Module, "").Replace('\\', '/');
                string content = @"<link rel=""stylesheet"" type=""text/css"" href=""" + asset_url + @""" />" + "\n";
                v_style_file_list.Add(content);
            }

            if (is_component)
                path = v_dir + route + @"\main.js";
            else
                path = v_dir + route + ".js";
            if (File.Exists(path))
            {
                string asset_url = "./" + path.Replace(Directories.Module, "").Replace('\\', '/');
                string content = "<script language=\"javascript\" src=\"" + asset_url + "\"></script>" + "\n";
                v_script_code_list.Add(content);
            }
        }

        protected string ReplaceLinkUrlText(string content, string asset_level)
        {
            string pattern = @"<a.*?href=""\./(.*?)"".*?>";
            MatchCollection matches = Regex.Matches(content, pattern);
            if (matches.Count > 0)
            {
                int newLength = 0;
                asset_level = asset_level.Substring(2);
                foreach (Match match in matches)
                {
                    string url_target = public_routes != null && public_routes.Count > 0 && public_routes[match.Groups[1].Value] != null ? public_routes[match.Groups[1].Value] + "" : "index";
                    string new_value = asset_level + url_target + ".html";
                    content = SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                    newLength += new_value.Length - match.Groups[1].Length;
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
                    string new_value = CurrentCommand != CommandType.LAUNCH ? "./" : "";
                    if (match.Groups[1].Value.Substring(0, 2) == "./")
                    {
                        if (CurrentCommand != CommandType.LAUNCH)
                            new_value += asset_level + match.Groups[1].Value.Substring(1);
                        else
                        {
                            if (match.Groups[1].Length >= 6 && match.Groups[1].Value.Substring(1, 6) == "assets")
                            {
                                new_value += asset_level + match.Groups[1].Value;
                            }
                            else
                            {
                                string view_asset = asset_level + match.Groups[1].Value;
                                new_value += LaunchViewAssets(view_asset);
                            }
                        }
                    }
                    else
                    {
                        if (CurrentCommand != CommandType.LAUNCH)
                        {
                            new_value += asset_level + "/" + component_name + match.Groups[1].Value.Substring(1);
                        }
                        else
                        {
                            string view_asset = asset_level + component_name + match.Groups[1].Value.Substring(1);
                            new_value += LaunchViewAssets(view_asset);
                        }
                    }
                    content = SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                    newLength += new_value.Length - match.Groups[1].Length;
                }
            }
            return content;
        }

        private string CalculateMD5Hash(string input)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        protected string LaunchViewAssets(string asset)
        {
            asset = asset.Replace("//", "/");
            string src_path = Directories.Module + asset.Replace("/", "\\");
            if (File.Exists(src_path))
            {
                FileInfo finfo = new FileInfo(src_path);
                Console.WriteLine("Copying from HTML " + asset);

                string dst_file_name = CalculateMD5Hash(asset) + finfo.Extension;
                string dst_path = Directories.PublicAsset + "local\\" + dst_file_name;

                if (!File.Exists(dst_path))
                {
                    if (finfo.Extension == ".css")
                    {
                        string content = File.ReadAllText(src_path);
                        MatchCollection matches = Regex.Matches(content, @"url\s*\(\s*(.*?)\s*\)");
                        int newLength = 0;
                        foreach (Match match in matches)
                        {
                            string c_name = match.Groups[1].Value.Replace("/", "\\");
                            src_path = finfo.DirectoryName + "\\" + c_name;
                            string i_dst_file_name = c_name;
                            if (File.Exists(src_path))
                            {
                                FileInfo finfo2 = new FileInfo(c_name);
                                string d_name = finfo2.DirectoryName.Replace(Directories.Module, "") + "\\" + finfo2.Name;
                                i_dst_file_name = CalculateMD5Hash(d_name) + finfo2.Extension;
                                string i_dst_path = Directories.PublicAsset + "local\\" + i_dst_file_name;
                                if (!File.Exists(i_dst_path))
                                {
                                    Console.WriteLine("Copying from CSS " + asset);
                                    File.Copy(src_path, i_dst_path);
                                }
                            }
                            string new_value = i_dst_file_name;
                            content = SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                            newLength += new_value.Length - match.Groups[1].Length;
                        }
                        File.WriteAllText(dst_path, content);
                    }
                    else
                    {
                        File.Copy(src_path, dst_path);
                    }
                }

                dst_file_name = "assets/local/" + dst_file_name;
                if (finfo.Extension == ".css")
                {
                    style_file_list.Add(dst_file_name);
                }
                if (finfo.Extension == ".js")
                {
                    script_file_list.Add(dst_file_name);
                }
                
                if (this is LayoutParsers.Wordpress || this is ViewParsers.Wordpress)
                    return "<?= get_template_directory_uri() ?>/" + dst_file_name;
                return "./" + dst_file_name;
            }
            return asset;
        }
    }
}
