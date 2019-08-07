using System;
using System.Collections.Generic;
using System.Text;
using Orion.Models;
using System.IO;
using System.Linq;
using System.Timers;
using Orion.IO;
using Orion.Util;
using static Orion.Models.TripRecordModel;
using System.Diagnostics;

namespace Orion.Core
{
    public class TripRecordFactory : TripRecordContext
    {
        public struct LookupRow
        {
            public DateTime Key;
            public long Value;

            public LookupRow(DateTime key, long value)
            {
                Key = key;
                Value = value;
            }
        }
       
        protected TripRecordFactory(string DataFile) : base(DataFile)
        {
            this.LookupTable = new Dictionary<DateTime, List<long>>();
        }

        private static TripRecordFactory ImportLookupTable(string DataFile, string LookupFile)
        {
            TripRecordFactory context = new TripRecordFactory(DataFile);
            if (Path.GetExtension(LookupFile) != '.' + Constants.LookupTableExtension)
                throw new Exception(string.Format("Looking for Index Map '.{0}' file", Constants.LookupTableExtension));

            BinaryReader<LookupRow> reader = new BinaryReader<LookupRow>(LookupFile);
            
            //Build Lookup Table
            foreach (var row in reader.ReadAll())
            {
                List<long> ret;
                if (!context.LookupTable.TryGetValue(row.Key, out ret))
                {
                    ret = new List<long>();
                    context.LookupTable[row.Key] = ret;
                }
                ret.Add(row.Value);
            }
            reader.Close();
            return context;
        }

        private static void CsvToBinaryFile<T>(string[] sources, string destination,  char delimiter, bool isFirstLineHeader)
            where T : TripRecordModel, new()
        {
            BinaryWriter<TripRecord> writer = new BinaryWriter<TripRecord>(destination);
            T model = new T();
            foreach (var source in sources)
            {
                Console.WriteLine(">Processing {0}<", source);
                using (StreamReader sreader = new StreamReader(source))
                {
                    TripRecord record = new TripRecord();
                    int rowcount = 0;
                    int currentrow = 0;

                    // Remove first line
                    if (isFirstLineHeader)
                        sreader.ReadLine();
                    while (!sreader.EndOfStream)
                    {
                        sreader.ReadLine();
                        rowcount++;
                    }
                    Console.WriteLine("Row Count = {0}", rowcount);

                    Timer timer = new Timer(1000);
                    var action = new ElapsedEventHandler((s, e) =>
                    {
                        Console.CursorLeft = 0;
                        Console.Write("Proccesed {0}/{1} Rows - {2:P2}\t", currentrow, rowcount, (float)currentrow / (float)rowcount);
                    });
                    timer.Elapsed += action;
                    timer.Start();

                    Dictionary<string, string> row = new Dictionary<string, string>();
                    sreader.BaseStream.Position = 0;  // Reset stream
                    string[] header = sreader.ReadLine().Split(delimiter);

                    while (!sreader.EndOfStream)
                    {
                        // Read entire row as string and tokenize
                        string[] tokens = sreader.ReadLine().Split(delimiter);
                        Debug.Assert(row.Count != tokens.Length);

                        for (int i = 0; i < tokens.Length; i++)
                            row.Add(header[i], tokens[i]);

                        //Parse tokens
                        record = model.ParseTokens(row);

                        writer.Write(record);

                        currentrow++;
                    }
                    action.Invoke(null, null);
                    timer.Stop();
                    timer.Close();
                }
                writer.Flush();
            }
            writer.Close();
        }

        private static TripRecordFactory CsvToBinaryFileWithLookupTable<T>(string[] sources, string destination, char delimiter)
            where T : TripRecordModel, new()
        {
            BinaryWriter<TripRecord> writer = new BinaryWriter<TripRecord>(destination);
            TripRecordFactory context = new TripRecordFactory(destination);
            T model = new T();
            foreach (var source in sources)
            {
                Console.WriteLine(">Processing {0}<", source);
                using (StreamReader sreader = new StreamReader(source))
                {
                    TripRecord record = new TripRecord();
                    int rowcount = 0;
                    int currentrow = 0;

                    //Count Rows
                    while (!sreader.EndOfStream)
                    {
                        sreader.ReadLine();
                        rowcount++;
                    }
                    Console.WriteLine("Row Count = {0}", rowcount);

                    Timer timer = new Timer(1000);
                    var action =  new ElapsedEventHandler((s, e) =>
                    {
                        Console.CursorLeft = 0;
                        Console.Write("Proccesed {0}/{1} Rows - {2:P2}\t", currentrow, rowcount, (float)currentrow / (float)rowcount);
                    });
                    timer.Elapsed += action;
                    timer.Start();

                    Dictionary<string, string> row = new Dictionary<string, string>();
                    sreader.BaseStream.Position = 0;  // Reset stream
                    string[] header = sreader.ReadLine().ToLower().Split(delimiter);

                    while (!sreader.EndOfStream)
                    {
                        row.Clear();
                        // Read entire row as string and tokenize
                        string[] tokens = sreader.ReadLine().Split(delimiter).Take(header.Length).ToArray();

                        for (int i = 0; i < tokens.Length; i++)
                            row.Add(header[i], tokens[i]);

                        //Parse tokens
                        record = model.ParseTokens(row);

                        //Write data structure to file
                        writer.Write(record);

                        //Index the data structure
                        DateTime key = new DateTime(record.TimeStamp.Year, record.TimeStamp.Month, record.TimeStamp.Day);
                        List<long> ret;
                        if (!context.LookupTable.TryGetValue(key, out ret))
                        {
                            ret = new List<long>();
                            context.LookupTable[key] = ret;
                        }
                        ret.Add(record.ID);

                        currentrow++;
                    }
                    action.Invoke(null, null);
                    Console.WriteLine();
                    timer.Stop();
                    timer.Close();
                }
                writer.Flush();
            }
            writer.Close();

            //Save Lookup table to file
            string path = Path.GetDirectoryName(destination);
            string name = Path.GetFileNameWithoutExtension(destination);
            string filename = Path.Combine(path, string.Format("{0}.{1}", name, Constants.LookupTableExtension));
            Console.WriteLine(">Storing Lookup Table<");

            BinaryWriter<LookupRow> stream = new BinaryWriter<LookupRow>(filename);
            foreach (var entry in context.LookupTable)
            {
                foreach (var value in entry.Value)
                {
                    LookupRow row = new LookupRow(entry.Key, value);
                    stream.Write(row);
                }
                stream.Flush();
            }
            stream.Close();
            Console.WriteLine("Lookup Table Successfully stored at {0}", filename);

            return context;
        }

        public static TripRecordContext Load(string DataFile, string LookupFile)
        {
            return ImportLookupTable(DataFile, LookupFile);
        }

        public static TripRecordContext GenerateBinaryFile<T>(string[] sources, string destination)
            where T : TripRecordModel, new()
        {
            return CsvToBinaryFileWithLookupTable<T>(sources, destination, ','); ;
        }


    }
}
