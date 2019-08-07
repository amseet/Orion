using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;

namespace Orion.IO
{
    

    public class DateMap
    {
        public interface IDateStructure
        {
            DateTime Date { get; }
        }

        private readonly Dictionary<DateTime, List<uint>> addressMap;
        private uint _counter = 0;
        private string _dataPath;
        private string _source, _destDir;
        private string name;

        public DateMap(string source, string destDir)
        {
            _source = source;
            _destDir = destDir;
            name = Path.GetFileNameWithoutExtension(source);

            addressMap = new Dictionary<DateTime, List<uint>>();
        }

        public void Create<T>(StreamReader sreader, Orion.IO.BinaryWriter<T> bwriter, char delimiter, Func<string[], T> TokenParser) where T : struct, IDateStructure
        {
            T att = new T();
            int rowcount = 0;
            int currentrow = 0;

            // Remove first line
            sreader.ReadLine();
            while (!sreader.EndOfStream)
            {
                sreader.ReadLine();
                rowcount++;
            }
            Console.WriteLine("Row Count = {0}", rowcount);

            // Reset stream
            sreader.BaseStream.Position = 0;
            // Remove first line
            sreader.ReadLine();

            var action = new ElapsedEventHandler((s, e) => {
                Console.WriteLine("Proccesed {0}/{1} Rows - {2:P2}", currentrow, rowcount, (float)currentrow / (float)rowcount);
            });

            Timer timer = new Timer();
            timer.Elapsed += action;
            timer.Interval = 1000;
            timer.Start();
            while (!sreader.EndOfStream)
            {
                // Read entire row as string and tokenize
                string[] tokens = sreader.ReadLine().Split(delimiter);

                //Parse tokens
                att = TokenParser.Invoke(tokens);

                bwriter.Write(att);

                currentrow++;
            }
            action.Invoke(null, null);
            timer.Stop();
            timer.Close();
        }

        //public static void Create<T>(string source, string destination, char delimiter, Func<string[], T> TokenParser) where T : struct
        //{
        //    using (StreamReader sreader = new StreamReader(
        //        new FileStream(source, FileMode.Open, FileAccess.Read)))
        //    {
        //        Create(sreader, new BinaryStream.Writer(destination), delimiter, TokenParser);
        //    }
        //}

        //public static void Convert<T>(string[] sources, string destination, char delimiter, Func<string[], T> TokenParser) where T : struct
        //{
        //    BinaryStream.Writer bwriter = new BinaryStream.Writer(destination);
        //    foreach (var source in sources)
        //    {
        //        Console.WriteLine(">Processing {0}<", source);
        //        using (StreamReader sreader = new StreamReader(
        //            new FileStream(source, FileMode.Open, FileAccess.Read)))
        //        {
        //            Create(sreader, bwriter, delimiter, TokenParser);
        //        }
        //    }
        //}

        //public void ToBinary(string [] csvFiles, string destPath)
        //{
        //    _dataPath = Path.Combine(destPath, "data.dat");
        //    Create<Record>(csvFiles, _dataPath, ',', this.ParseTokens);

        //    using (StreamWriter writer = new StreamWriter(Path.Combine(destPath, "record.map")))
        //        foreach (var entry in addressMap)
        //            writer.WriteLine(string.Join(',', entry.Key, string.Join(';', entry.Value)));
        //}
        
        //public void ConvertAll(string path, string destPath)
        //{
        //    string[] files = Directory.GetFiles(path);
        //    ToBinary(files, destPath);
        //}

        //public static TaxiTrips Import(string directory)
        //{
        //    TaxiTrips tt = new TaxiTrips();
        //    tt._dataPath = Path.Combine(directory, "data.dat");
        //    using (StreamReader reader = new StreamReader(Path.Combine(directory, "record.map")))
        //    {
        //        while (!reader.EndOfStream)
        //        {
        //            string line = reader.ReadLine();
        //            string[] tokens = line.Split(',');

        //            if (tokens.Length == 2)
        //            {
        //                DateTime dt = DateTime.Parse(tokens[0]);
        //                string[] sids = tokens[1].Split(';');
        //                var ids = sids.Select(s => uint.Parse(s)).ToList();
        //                tt.addressMap.Add(dt, ids);
        //                tt._counter += (uint)ids.Count();
        //            }
        //        }
        //    }
        //    return tt;
        //}
    
    }
}
