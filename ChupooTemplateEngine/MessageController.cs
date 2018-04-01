using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class MessageController
    {
        public static void Show(string message)
        {
            Console.WriteLine(message);
        }

        public static void Show(Hashtable data)
        {
            if (data == null) return;

            foreach (DictionaryEntry de in data)
            {
                Console.WriteLine(de.Key + "=" + de.Value);
            }
            Console.WriteLine();
        }

        public static void Show(JObject data)
        {
            if (data == null) return;

            foreach (var de in data)
            {
                Console.WriteLine(de.Key + "=" + de.Value);
            }
            Console.WriteLine();
        }
    }
}
