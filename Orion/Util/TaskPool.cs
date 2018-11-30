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

        public void Run(Action<int, int, Progress> action)
        {
            int numberOfBatches = (int)((double)_MaxCount / (double)_BatchSize + 1.0f);
            Console.WriteLine("No. of Trips: {0}\t" +
                                "No. of Batchs: {1} of size {2}", _MaxCount, numberOfBatches, _BatchSize);

            int BatchCount = 0;
            while (BatchCount < numberOfBatches)
            {
                if(Tasks.Count < _PoolSize)
                {
                    Tasks.Add(Task.Run(()=>{
                        Progress progress = new Progress(1000);
                        int curpos = Console.CursorTop;

                    }));
                }
                else
                    Task.WaitAny(Tasks.ToArray());
            }
            
        }
    }
}
