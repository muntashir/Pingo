using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingo.Classes
{
    public class Locks
    {
        public object globalLock = new object();
        public object threadLock = new object();
        public object updateLock = new object();

        public Locks()
        { }
    }
}
