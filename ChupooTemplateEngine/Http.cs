using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class Http
    {
        public string GetResponse(string url)
        {
            System.IO.Stream str = null;
            System.IO.StreamReader srRead = null;
            try
            {
                System.Net.WebRequest req = System.Net.WebRequest.Create(url);
                System.Net.WebResponse resp = req.GetResponse();
                str = resp.GetResponseStream();
                srRead = new System.IO.StreamReader(str);
                return srRead.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                srRead.Close();
                str.Close();
            }
        }
    }
}
