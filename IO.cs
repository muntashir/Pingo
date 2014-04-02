﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Pingo
{
    public class IO
    {
        protected HostList hostList;

        public IO(HostList hostList)
        {
            this.hostList = hostList;
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
                foreach (Host host in hostList.hosts)
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
    }
}
