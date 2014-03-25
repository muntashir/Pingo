using System;
using System.Net.NetworkInformation;

namespace Pingo
{
    public class Host
    {
        //Stores status of the ping
        protected enum PingStatus { Online, Offline, Error }
        protected PingStatus status;

        //Used to ping
        protected Ping pingSend;
        protected PingReply pingReply;

        //Stores info about the host
        protected string hostname;
        protected DateTime timestamp;

        //Constructor
        public Host(string hostname)
        {
            //Sets hostname
            this.hostname = hostname;

            //Initializes pingSend
            pingSend = new Ping();           
        }

        public bool IsNotPinged()
        {
            if (pingReply == null)
                return true;
            else
                return false;
        }

        //Gets status of the host with a ping
        public void Ping()
        {
                //Default status offline
                status = PingStatus.Offline;

                //Set timestamp to current time
                timestamp = DateTime.Now;

                try
                {
                    //Send a ping with 3000 ms timeout
                    pingReply = pingSend.Send(hostname, 3000);

                    //Sets status to sucess if ping is successful
                    if (pingReply.Status == IPStatus.Success)
                        status = PingStatus.Online;
                }
                catch (PingException)
                {
                    //Could be error if DNS could not resolve host
                    status = PingStatus.Error;
                }
        }

        //Returns string array of hostname, status, and timestamp
        public new String[] ToString()
        {
            //Stores string representation of status
            string strStatus = null;

            //Converts PingStatus to string
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

            //Returns string array of hostname, status, and timestamp of ping
            String[] output = { hostname, strStatus, timestamp.ToShortTimeString() + " " + timestamp.ToShortDateString() };
            return output;
        }
    }
}
