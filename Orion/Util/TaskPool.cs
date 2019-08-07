using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orion.Util
{
    public class TaskPool
    {
        private List<Task> Tasks;
        private readonly int _PoolSize;
        private readonly int _BatchSize;
        private readonly int _MaxCount;

        public TaskPool(int BatchSize, int MaxCount)
        {
            _BatchSize = BatchSize;
            _MaxCount = MaxCount;
            ThreadPool.GetMinThreads(out _PoolSize, out int minPorts);
            Tasks = new List<Task>();
        }

        public void Run(Action<int> action)
        {
            int totalBatches = (int)((double)_MaxCount / (double)_BatchSize + 0.5f);
            Console.WriteLine("No. of Batchs: {0}", totalBatches);
            Progress progress = new Progress(1000, totalBatches);
            //progress.Start();
            int i = 0;
            while(i < totalBatches)
            {
                if (Tasks.Count < _PoolSize)
                {
                    Tasks.Add(Task.Run(() =>
                    {
                        action.Invoke(i);
                        progress.inc();
                    }));
                    i += _BatchSize;
                    Thread.Sleep(100);
                }
                else
                    Tasks.RemoveAt(Task.WaitAny(Tasks.ToArray()));
            }
            progress.Stop();
        }
        public void Wait()
        {
            Task.WaitAll(Tasks.ToArray());
        }
    }
}
