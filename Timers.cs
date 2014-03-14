using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Pingo
{
    class Timers
    {
        public DispatcherTimer dispatcherTimer = new DispatcherTimer();
        public DispatcherTimer nextUpdate = new DispatcherTimer();
        public TimeSpan timeElapsed = new TimeSpan(0, 0, 0);
        public MainWindow mainWindow;

        public Timers()
        {
        }

        public Timers(MainWindow mainWindow)
        {
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 10, 0);
            dispatcherTimer.Start();

            nextUpdate.Tick += new EventHandler(nextUpdate_Tick);
            nextUpdate.Interval = new TimeSpan(0, 0, 1);
            nextUpdate.Start();

            this.mainWindow = mainWindow;
        }

        private void nextUpdate_Tick(object sender, object e)
        {
            mainWindow.lblNextUpdate.Content = "Next update in " + (dispatcherTimer.Interval - timeElapsed);
            timeElapsed = timeElapsed.Add(new TimeSpan(0, 0, 1));
        }

        private void dispatcherTimer_Tick(object sender, object e)
        {
            mainWindow.Refresh();
            timeElapsed = new TimeSpan(0, 0, 0);
        }
    }
}
