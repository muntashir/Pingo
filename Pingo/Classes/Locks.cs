using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingo.Classes
{
    public static class Locks
    {
        public static object globalLock = new object();
        public static object threadLock = new object();
        public static object updateLock = new object();
    }
}
