using System;
using System.Collections.Generic;
using System.Data;

namespace Pingo
{
    public class HostList
    {
        private Object updateLock = new Object();

        //DataTable source for ListView
        public DataTable data = new DataTable();

        //List to store all hosts
        public List<Host> hosts;

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

        public void AddHost(String hostname)
        {
            hosts.Add(new Host(hostname));
        }

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
