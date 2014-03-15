using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Pingo
{
    class HostList
    {
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

        //Exports hosts to CSV file
        public void Export()
        {
            try
            {
                //Creates file
                StreamWriter writer = new StreamWriter("export.csv");

                //Writes the column names
                writer.WriteLine("Hostname,Status,Last updated");

                //Writes each host as a seperate line
                foreach (Host host in hosts)
                {
                    writer.WriteLine(host.ToString()[0] + "," + host.ToString()[1] + "," + host.ToString()[2]);
                }

                //Closes file
                writer.Close();

                //Opens exported file
                Process.Start("export.csv");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //Updates DataTable with contents of hosts
        public void UpdateData()
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
}
