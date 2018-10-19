using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;

namespace Digitizer
{
    class Binarizer
    {
        /// <summary>
        /// Source: https://stackoverflow.com/questions/2871/reading-a-c-c-data-structure-in-c-sharp-from-a-byte-array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns>T</returns>

        public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var t = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
            return t;
        }

        public static byte[] StructureToByteArray<T>(T t) where T : struct
        {
            byte[] bytes = new byte[Marshal.SizeOf<T>()];
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Marshal.StructureToPtr<T>(t, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return bytes;
        }

        public static void Binarize<T>(string source, string destination, char delimiter, Func<string[], T> TokenParser) where T : struct
        {
            using (FileStream fstream = new FileStream(source, FileMode.Open, FileAccess.Read),
                    dstream = new FileStream(destination, FileMode.Create, FileAccess.Write))
            {
                BinaryWriter bwriter = new BinaryWriter(dstream);
                StreamReader sreader = new StreamReader(fstream);

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
                    // Read entitre row as string and tokenize
                    string[] tokens = sreader.ReadLine().Split(delimiter);

                    //Parse tokens
                    att = TokenParser.Invoke(tokens);

                    // Convert attributes into bytes
                    byte[] bytes = Binarizer.StructureToByteArray<T>(att);

                    // Write bytes to file
                    bwriter.Write(bytes, 0, bytes.Length);

                    currentrow++;
                }
                action.Invoke(null, null);
                timer.Stop();
                timer.Close();
            }
        }

        public static T[] ImportBinarizedFile<T>(string file) where T : struct
        {
            using (FileStream fstream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes;
                System.IO.BinaryReader breader = new System.IO.BinaryReader(fstream);
                long rowcount = fstream.Length / Marshal.SizeOf<T>();

                T[] Rows = new T[rowcount];

                for (var i = 0; i < rowcount; i++)
                {
                    bytes = breader.ReadBytes(Marshal.SizeOf<T>());
                    Rows[i] = Binarizer.ByteArrayToStructure<T>(bytes);
                }

                return Rows;
            }
            return null;
        }

        public static T Get<T>(string file, long row) where T : struct
        {
            BinaryReader reader = new BinaryReader(new FileStream(file, FileMode.Open, FileAccess.Read));
            long pos = row * Marshal.SizeOf<T>();
            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf<T>());
            return Binarizer.ByteArrayToStructure<T>(bytes);
        }
    }
}
