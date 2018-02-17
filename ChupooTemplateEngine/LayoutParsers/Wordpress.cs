﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChupooTemplateEngine.LayoutParsers
{
    class Wordpress : LayoutParser
    {
        private Hashtable partial_files = new Hashtable();

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
                CopyResources();
                
                if (!partial_files.ContainsKey("header"))
                {
                    layout_content = PasteStyles(layout_content);
                }
                if (!partial_files.ContainsKey("footer"))
                {
                    layout_content = PasteScripts(layout_content);
                }

                layout_content = ReplaceLinkUrlText(layout_content, asset_level);
                layout_content = ReplaceAssetUrlText(layout_content, asset_level, cfg_layout_name);

                // Make <layout>.php file
                string p_file;
                p_file = Directories.Public + dest + ".php";
                File.WriteAllText(p_file, layout_content);
                Console.WriteLine("OK: " + dest + ".php");

                CreatePartialFile("header", asset_level);
                CreatePartialFile("footer", asset_level);

                partial_files.Clear();
                ClearAll();
            }
            else
            {
                Console.WriteLine("Error: Layout directory " + p_dir + " is not found");
            }
        }

        private void CreatePartialFile(string partialName, string asset_level)
        {
            if (partial_files.ContainsKey(partialName))
            {
                string part_content = File.ReadAllText(partial_files[partialName] + "");
                part_content = RenderPartialLayout(part_content);
                if (partialName == "header")
                {
                    part_content = PasteStyles(part_content);
                }
                else if (partialName == "footer")
                {
                    part_content = PasteScripts(part_content);
                }
                part_content = ReplaceLinkUrlText(part_content, asset_level);
                part_content = ReplaceAssetUrlText(part_content, asset_level, cfg_layout_name);
                string p_file = Directories.Public + partialName + ".php";
                File.WriteAllText(p_file, part_content);
                Console.WriteLine("OK: " + partialName + ".php");
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
                    string name = match.Groups[1].Value;
                    string layout_name = "_" + name;
                    string layout_file = Directories.Layout + layout_name + ".html";

                    if (name == "header" || name == "footer")
                    {
                        string code = "<?php get_" + name + "() ?>";
                        content = SubsituteString(content, match.Index + newLength, match.Length, code);
                        newLength += code.Length - match.Length;
                        partial_files[name] = layout_file;
                    }
                    else
                    {
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
            }
            return content;
        }

        private void CopyResources()
        {
            string dst = Directories.Public + "style.css";
            string src = Directories.Module + "\\launch\\wordpress\\style.css";
            File.Copy(src, dst);
        }

        private void CreateFunctionsFile()
        {
            // Make functions.php file
            string p_file = Directories.Public + "functions.php";
            Hashtable data = new Hashtable();
            data["ThemeName"] = "swad";

            List<Hashtable> JsFiles = new List<Hashtable>();
            int i = 0;
            foreach(string file in script_file_list)
            {
                Hashtable _file = new Hashtable();
                _file["Id"] = "script" + i;
                _file["Path"] = file;
                JsFiles.Add(_file);
                i++;
            }

            List<Hashtable> CssFiles = new List<Hashtable>();
            foreach (string file in style_file_list)
            {
                Hashtable _file = new Hashtable();
                _file["Id"] = "style" + i;
                _file["Path"] = file;
                CssFiles.Add(_file);
                i++;
            }
            data["Css"] = CssFiles;

            string r_path = Directories.Resources + "\\launch_templates\\wordpress\\functions.php";
            string content = ResourceParser.Parse(r_path, data);
            File.WriteAllText(p_file, content);
        }
    }
}
