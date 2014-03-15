using System;
using System.Windows.Threading;

namespace Pingo
{
    class Timers
    {
        public DispatcherTimer updateTimer = new DispatcherTimer();
        public DispatcherTimer timeToNextUpdateTimer = new DispatcherTimer();
        public TimeSpan timeElapsed = new TimeSpan(0, 0, 0);
        public MainWindow mainWindow;

        public Timers()
        {
        }

        public Timers(MainWindow mainWindow)
        {
            updateTimer.Tick += new EventHandler(updateTimer_Tick);
            updateTimer.Interval = new TimeSpan(0, 10, 0);
            updateTimer.Start();

            timeToNextUpdateTimer.Tick += new EventHandler(nextUpdate_Tick);
            timeToNextUpdateTimer.Interval = new TimeSpan(0, 0, 1);
            timeToNextUpdateTimer.Start();

            this.mainWindow = mainWindow;
        }

        private void nextUpdate_Tick(object sender, object e)
        {
            mainWindow.lblNextUpdate.Content = "Next update in " + (updateTimer.Interval - timeElapsed);
            timeElapsed = timeElapsed.Add(new TimeSpan(0, 0, 1));
        }

        private void updateTimer_Tick(object sender, object e)
        {
            mainWindow.RefreshAll();
            timeElapsed = new TimeSpan(0, 0, 0);
        }
    }
}
