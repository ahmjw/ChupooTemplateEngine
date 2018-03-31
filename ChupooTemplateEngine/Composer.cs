using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class Composer
    {
        public event SimpleEventHandler OnResponse;

        //private void LoadConfigFile()
        //{
        //    if (File.Exists(config_path))
        //    {
        //        content = File.ReadAllText(config_path);
        //        try
        //        {
        //            JObject composer_prop = JsonConvert.DeserializeObject<JObject>(content);
        //            string version = (string)composer_prop["require"][package_name];
        //            if (version == "")
        //            {
        //                is_stable = false;
        //            }
        //        }
        //        catch
        //        {
        //            is_stable = false;
        //        }
        //    }
        //    else
        //    {
        //        is_stable = false;
        //    }
        //}

        internal void ClearCache()
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c set PATH=%PATH%;" + AppProperty.ServerRoot + @"\php&composer clearcache";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            //* Set your output and error (asynchronous) handlers
            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
            //* Start process and handlers
            OnResponse?.Invoke(this, new SimpleEventArgs("Clearing cache ..."));
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }

        public void Install(string name)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            //process.StartInfo.Arguments = "/c set PATH=%PATH%;" + AppProperty.ServerRoot + @"\php&composer clearcache&composer require " + package_name + @" -d apache\htdocs";
            process.StartInfo.Arguments = "/c set PATH=%PATH%;" + AppProperty.ServerRoot + @"\php&composer require " + name + @" -d libs\composer";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            //* Set your output and error (asynchronous) handlers
            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
            //* Start process and handlers
            OnResponse?.Invoke(this, new SimpleEventArgs("Contacting repository server. Please wait ..."));
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }

        private void OutputHandler(object sender, DataReceivedEventArgs e)
        {
            //* Do your stuff with the output (write to console/log/StringBuilder)
            OnResponse?.Invoke(this, new SimpleEventArgs(e.Data));
        }
    }
}
