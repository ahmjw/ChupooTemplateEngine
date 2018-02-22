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
    class AssetParser
    {
        private string[] pic_exts = { ".ico", ".png", ".jpeg", ".jpg", ".jpeg", ".bmp", ".svg" };
        private string[] vid_exts = { ".mp4", ".avi", ".mov", ".flv", ".wmv", ".3gp", ".mpg" };
        private string[] aud_exts = { ".mp3", ".amr", ".ogg", ".wav", ".wma" };

        private void ParseCss(string dir, string src_path, string dst_path)
        {
            string content = File.ReadAllText(src_path);
            MatchCollection matches = Regex.Matches(content, @"url\s*\(\s*(.*?)\s*\)");
            int newLength = 0;
            foreach (Match match in matches)
            {
                string c_name = match.Groups[1].Value.Replace("/", "\\");
                src_path = dir + "\\" + c_name;
                string i_dst_file_name = c_name;
                if (File.Exists(src_path))
                {
                    FileInfo finfo2 = new FileInfo(src_path);
                    string d_name = finfo2.DirectoryName.Replace(Directories.Module, "") + "\\" + finfo2.Name;
                    d_name = d_name.Replace("\\", "/");
                    i_dst_file_name = RenameAsset(d_name);
                    string i_dst_path = Directories.PublicAsset + "local\\" + i_dst_file_name;
                    if (!File.Exists(i_dst_path))
                    {
                        Console.WriteLine("  CSS> " + d_name);
                        File.Copy(src_path, i_dst_path);
                    }
                }
                string new_value = "../" + i_dst_file_name;
                content = Parser.SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                newLength += new_value.Length - match.Groups[1].Length;
            }
            File.WriteAllText(dst_path, content);
        }

        private string RenameAsset(string asset)
        {
            string d_root = Directories.PublicAsset + "local\\";
            Match match = Regex.Match(asset, @"(_.*)?/([a-zA-Z0-9-_\.]+)(\.[a-zA-Z0-9-_]+)$");
            if (match.Success)
            {
                string d_name = "";
                string extension = match.Groups[3].Value;
                string f_name = match.Groups[2].Value;
                f_name = match.Groups[1].Value.Replace("/", "-") + f_name;
                f_name = f_name.Replace("-main", "");

                if (pic_exts.Any(extension.ToLower().Equals))
                {
                    d_name = "images";
                    if (!Directory.Exists(d_root + d_name))
                    {
                        Directory.CreateDirectory(d_root + d_name);
                    }
                }
                else if (aud_exts.Any(extension.ToLower().Equals))
                {
                    d_name = "audios";
                    if (!Directory.Exists(d_root + d_name))
                    {
                        Directory.CreateDirectory(d_root + d_name);
                    }
                }
                else if (vid_exts.Any(extension.ToLower().Equals))
                {
                    d_name = "videos";
                    if (!Directory.Exists(d_root + d_name))
                    {
                        Directory.CreateDirectory(d_root + d_name);
                    }
                }
                else if (extension == ".js")
                {
                    d_name = "scripts";
                    if (!Directory.Exists(d_root + d_name))
                    {
                        Directory.CreateDirectory(d_root + d_name);
                    }
                }
                else if (extension == ".css")
                {
                    d_name = "styles";
                    if (!Directory.Exists(d_root + d_name))
                    {
                        Directory.CreateDirectory(d_root + d_name);
                    }
                }
                return d_name + "/" + f_name + extension;
            }
            return asset;
        }

        private string ReplaceAssetUrlText(string asset_level, string name, string content)
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
                asset_level += "/libs";

                foreach (Match match in matches)
                {
                    string new_value = "";
                    if (match.Groups[1].Value.Substring(0, 2) == "./")
                    {
                        if (CurrentCommand != CommandType.LAUNCH)
                            new_value += asset_level + match.Groups[1].Value.Substring(1);
                        else
                        {
                            // LAUNCH
                        }
                    }
                    else
                    {
                        if (CurrentCommand != CommandType.LAUNCH)
                        {
                            new_value += asset_level + "/" + name + "/" + match.Groups[1].Value.Substring(1);
                        }
                        else
                        {
                            // LAUNCH
                        }
                    }

                    content = Parser.SubsituteString(content, match.Groups[1].Index + newLength, match.Groups[1].Length, new_value);
                    newLength += new_value.Length - match.Groups[1].Length;
                }
            }
            return content;
        }

        public string Parse(string name, string content)
        {
            string v_dir = Directories.Lib;
            string _route = name;
            content = ReplaceAssetUrlText("./", name, content);

            string path = v_dir + name + "\\main.css";
            if (File.Exists(path))
            {
                string _m_name = path.Replace(Directories.Module, "").Replace('\\', '/');
                string asset_url;
                if (CurrentCommand != CommandType.LAUNCH)
                {
                    asset_url = "../modules/" + _m_name;
                }
                else
                {
                    string m_name = _m_name.Replace("/main.css", ".css");
                    m_name = m_name.Replace("/", "-");
                    m_name = m_name.Replace("-@", "-");
                    string a_dir = Directories.PublicAsset + "local\\styles\\";
                    if (!Directory.Exists(a_dir))
                    {
                        Directory.CreateDirectory(a_dir);
                    }
                    if (!File.Exists(a_dir + m_name))
                    {
                        Console.WriteLine("  File> " + _m_name);
                        ParseCss(v_dir + name, path, a_dir + m_name);
                    }
                    asset_url = "assets/local/styles/" + m_name;
                }
                Parser.RegisterCssFile(asset_url);
            }

            path = v_dir + name + "\\main.js";
            if (File.Exists(path))
            {
                string asset_url;
                string _m_name = path.Replace(Directories.Module, "").Replace('\\', '/');
                if (CurrentCommand != CommandType.LAUNCH)
                {
                    asset_url = "../modules/" + _m_name;
                }
                else
                {
                    string m_name = _m_name.Replace("/main.js", ".js");
                    m_name = m_name.Replace("/", "-");
                    m_name = m_name.Replace("-@", "-");
                    string a_dir = Directories.PublicAsset + "local\\scripts\\";
                    if (!Directory.Exists(a_dir))
                    {
                        Directory.CreateDirectory(a_dir);
                    }
                    if (!File.Exists(a_dir + m_name))
                    {
                        Console.WriteLine("  File> " + _m_name);
                        File.Copy(path, a_dir + m_name);
                    }
                    asset_url = "assets/local/scripts/" + m_name;
                }
                Parser.RegisterJsFile(asset_url);
            }
            return content;
        }
    }
}