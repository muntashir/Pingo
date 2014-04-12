using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shell;

namespace Pingo.Classes
{
    public class ProgressBarUpdater
    {
        ProgressBar progressBar;
        MainWindow mainWindow;

        public ProgressBarUpdater(ProgressBar progressBar, MainWindow mainWindow)
        {
            this.progressBar = progressBar;
            this.mainWindow = mainWindow;
        }

        public void UpdateProgressBar(double numerator, double denominator)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));
            if (numerator != 0 && denominator != 0)
            {
                DoubleAnimation doubleanimation = new DoubleAnimation((numerator / denominator) * 100.0, duration);
                progressBar.BeginAnimation(ProgressBar.ValueProperty, doubleanimation);
            }

            mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            mainWindow.TaskbarItemInfo.ProgressValue = numerator / denominator;
        }

        public void ResetProgressBar()
        {
            progressBar.BeginAnimation(ProgressBar.ValueProperty, null);
            progressBar.Value = 0;

            mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None; 
            mainWindow.TaskbarItemInfo.ProgressValue = 0;
        }
    }
}
