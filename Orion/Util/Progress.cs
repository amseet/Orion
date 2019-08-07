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
        private int clPos;

        public Progress(int interval, int Max)
        {
            timer = new Timer();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = interval;
            Counter = 0;
            MaxValue = Max;
        }

        public Progress(int interval, int Max, ElapsedEventHandler elapsedEventHandler) 
        {
            timer = new Timer();
            timer.Elapsed += elapsedEventHandler;
            timer.Interval = interval;
            Counter = 0;
            MaxValue = Max;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.SetCursorPosition(0, clPos);
            Console.WriteLine("                                                       ");
            Console.SetCursorPosition(0, clPos);
            Console.WriteLine("Proccesed {0}/{1} Rows - {2:P3}", Counter, MaxValue, (float)Counter / (float)MaxValue);

            if (Counter >= MaxValue && timer.Enabled)
            {
                Stop();
            } 
        }
        public void Stop()
        {
            timer.Enabled = false;
            Timer_Elapsed(null, null);

            timer.Stop();
            timer.Close();
        }

        public void Start()
        {
            clPos = Console.CursorTop;
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

        public void Restart()
        {
            Counter = 0;
            Start();
        }
    }
}
