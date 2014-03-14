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
        public enum PingStatus { Online, Offline, Error }

        protected string hostname;
        protected Ping pingSend;
        protected PingReply pingReply;
        protected PingStatus status;
        protected DateTime timestamp;

        public Host(string hostname)
        {
            this.hostname = hostname;
            pingSend = new Ping();           

            Ping();
        }

        public void Ping()
        {
            status = PingStatus.Offline;
            timestamp = DateTime.Now;

            try
            {
                pingReply = pingSend.Send(hostname, 2500);
                if (pingReply.Status == IPStatus.Success)
                    status = PingStatus.Online;
            }
            catch (PingException)
            {
                status = PingStatus.Error;
            }
        }

        public PingStatus IsOnline()
        {
            return status;
        }

        public new String[] ToString()
        {
            string strStatus = null;

            switch (status)
            {
                case PingStatus.Online:
                    strStatus = "Online";
                    break;
                case PingStatus.Offline:
                    strStatus = "Offline";
                    break;
                case PingStatus.Error:
                    strStatus = "Error";
                    break;
            }

            String[] array = { hostname, strStatus, timestamp.ToShortTimeString() + " " + timestamp.ToShortDateString() };

            return array;
        }
    }
}
