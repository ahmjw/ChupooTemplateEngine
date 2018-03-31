using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChupooTemplateEngine
{
    public delegate void SimpleEventHandler(object sender, SimpleEventArgs e);

    public class SimpleEventArgs : EventArgs
    {
        object obj;
        public object Argument { get { return obj; } }

        public SimpleEventArgs(object obj)
        {
            this.obj = obj;
        }
    }
}
