using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class CloningPage
    {
        public string Name { set; get; }
        public string Content { set; get; }
        public List<JToken> Data { set; get; }
    }
}
