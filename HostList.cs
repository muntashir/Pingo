using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Pingo
{
    class HostList
    {
        public DataTable data = new DataTable();
        public List<Host> hosts;

        public HostList()
        {
            hosts = new List<Host>();

            data.Columns.Add("Hostname", typeof(string));
            data.Columns.Add("Status", typeof(string));
            data.Columns.Add("Timestamp", typeof(string));
        }

        public void Export()
        {
            try
            {
                StreamWriter writer = new StreamWriter("export.csv");

                writer.WriteLine("Hostname,Status,Last updated");

                foreach (Host host in hosts)
                {
                    writer.WriteLine(host.ToString()[0] + "," + host.ToString()[1] + "," + host.ToString()[2]);
                }

                writer.Close();

                Process.Start("export.csv");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void UpdateData()
        {
            data.Rows.Clear();

            foreach (Host host in hosts)
            {
                data.Rows.Add(host.ToString()[0], host.ToString()[1], host.ToString()[2]);
            }
        }
    }
}
