using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Pingo
{
    public class Host
    {
        protected string hostname;
        protected Ping pingo;
        protected PingReply pingoReply;
        protected bool online;
        protected DateTime timestamp;

        public Host(string hostname)
        {
            this.hostname = hostname;
            pingo = new Ping();           

            Ping();
        }

        public void Ping()
        {
            online = false;

            try
            {
                pingoReply = pingo.Send(hostname);
                online = pingoReply.Status == IPStatus.Success;
                timestamp = DateTime.Now;
            }
            catch (PingException)
            {
            }
        }

        public bool IsOnline()
        {
            return online;
        }

        public new String[] ToString()
        {
            String[] array = { hostname, online ? "Online" : "Offline", timestamp.ToShortTimeString() + " " + timestamp.ToLongDateString() };

            return array;
        }
    }
}
