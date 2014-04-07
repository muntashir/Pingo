using System;
using System.Collections.Generic;
using System.Data;

namespace Pingo
{
    //Stores a List of all hosts and a ToString of each host as a DataTable
    public class HostList
    {
        //Lock so data is not updated by multiple threads at once
        protected Object updateLock = new Object();

        //DataTable source for ListView
        protected DataTable data = new DataTable();

        //List to store all hosts
        protected List<Host> hosts;

        //Constructor
        public HostList()
        {
            //Initialize hosts
            hosts = new List<Host>();

            //Initialize DataTable with columns
            data.Columns.Add("Hostname", typeof(string));
            data.Columns.Add("Status", typeof(string));
            data.Columns.Add("Timestamp", typeof(string));
        }

        //Updates DataTable with contents of hosts
        public void UpdateData()
        {
            lock (updateLock)
            {
                //Clear old rows of DataTable
                data.Rows.Clear();

                //Create new rows with contents of hosts
                foreach (Host host in hosts)
                {
                    data.Rows.Add(host.ToString()[0], host.ToString()[1], host.ToString()[2]);
                }
            }
        }

        public List<Host> GetHostsAsList()
        {
            return hosts;
        }

        public DataTable GetHostsAsDataTable()
        {
            return data;
        }

        //Adds host
        public void AddHost(String hostname)
        {
            hosts.Add(new Host(hostname));
        }

        //Checks for duplicate host
        public bool IsDuplicate(string line)
        {
            foreach (Host host in hosts)
            {
                if (host.ToString()[0] == line)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
