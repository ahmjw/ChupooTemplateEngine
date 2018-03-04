using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    class Debugger
    {
        public static void Enumerator(Hashtable data)
        {
            foreach (DictionaryEntry de in data)
            {
                Console.WriteLine(de.Key + "=" + de.Value);
            }
            Console.WriteLine();
        }
    }
}
