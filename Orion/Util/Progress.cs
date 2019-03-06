using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Timers;

namespace Orion.Util
{
    public class Progress
    {
        private Timer timer;
        private int Counter;
        public int MaxValue;

        public Progress(int interval)
        {
            timer = new Timer();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = interval;
            Counter = 0;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        { 
            Console.WriteLine("Proccesed {0}/{1} Rows - {2:P3}", Counter, MaxValue, (float)Counter / (float)MaxValue);

            if (Counter >= MaxValue)
            {
                Stop();
            } 
        }
        public void Stop()
        {
            timer.Enabled = false;
            timer.Stop();
            timer.Close();
        }

        public void Start()
        {
            if (!timer.Enabled)
            {
                timer.Enabled = true;
                timer.Start();
            }
        }
        public void inc()
        {
            Counter++;
        }
        public void Update(int counter, int max)
        {
            if (!timer.Enabled)
            {
                timer.Enabled = true;
                Counter = counter;
                MaxValue = max;
                timer.Start();
            }
            else
            {
                Counter = counter;
                MaxValue = max;
            }
        }
    }
}
