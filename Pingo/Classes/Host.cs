﻿using System;
using System.Net.NetworkInformation;

namespace Pingo.Classes
{
    //Stores data about a host
    public class Host
    {
        //Stores status of the ping
        protected enum PingStatus { Online, Offline, Unreachable }
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

        //Check if host not pinged yet
        public bool IsNotPinged()
        {
            if (pingReply == null && status != PingStatus.Unreachable)
                return true;
            else
                return false;
        }

        //Gets status of the host with a ping
        public void Ping()
        {
            //Set timestamp to current time
            timestamp = DateTime.Now;

            try
            {
                //Send a ping with 3000 ms timeout
                pingReply = pingSend.Send(hostname, 3000);

                //Sets status to sucess if ping is successful
                if (pingReply.Status == IPStatus.Success)
                    status = PingStatus.Online;
                else if (pingReply.Status == IPStatus.TimedOut)
                    status = PingStatus.Offline;
                else
                    status = PingStatus.Unreachable;
            }
            catch (Exception)
            {
                status = PingStatus.Unreachable;
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
                case PingStatus.Unreachable:
                    strStatus = "Unreachable";
                    break;
            }

            //Returns string array of hostname, status, and timestamp of ping
            String[] output = { hostname, strStatus, timestamp.ToLongTimeString() + "  " + timestamp.ToShortDateString() };
            return output;
        }
    }
}
