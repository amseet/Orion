using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Orion.IO
{
    /// <summary>
    /// Source: https://stackoverflow.com/questions/2871/reading-a-c-c-data-structure-in-c-sharp-from-a-byte-array
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <returns>T</returns>
    public abstract class BinaryStream
    {
        public static byte[] StructureToByteArray<T>(T t) where T : struct
        {
            byte[] bytes = new byte[Marshal.SizeOf<T>()];
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            Marshal.StructureToPtr<T>(t, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return bytes;
        }

        public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var t = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
            return t;
        }

        public static T[] ImportBinaryFile<T>(Stream stream) where T : struct
        {
            byte[] bytes;
            System.IO.BinaryReader breader = new System.IO.BinaryReader(stream);
            long rowcount = stream.Length / Marshal.SizeOf<T>();

            T[] Rows = new T[rowcount];

            for (var i = 0; i < rowcount; i++)
            {
                bytes = breader.ReadBytes(Marshal.SizeOf<T>());
                Rows[i] = ByteArrayToStructure<T>(bytes);
            }

            return Rows;
        }

        public static T Get<T>(Stream stream, long row) where T : struct
        {
            System.IO.BinaryReader reader = new System.IO.BinaryReader(stream);
            long pos = row * Marshal.SizeOf<T>();
            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf<T>());
            return ByteArrayToStructure<T>(bytes);
        }

        public static void Write<T>(T item, Stream stream) where T : struct
        {
            byte[] bytes = StructureToByteArray<T>(item);
            // Write bytes to file
            stream.Write(bytes, 0, bytes.Length);
        }
    }

    public class BinaryWriter<T> : BinaryStream
        where T : struct
    {
        System.IO.BinaryWriter writer;
        public BinaryWriter(string file)
        {
            writer = new System.IO.BinaryWriter(new FileStream(file, FileMode.Create, FileAccess.Write));
        }

        public void Write(T item)
        {
            byte[] bytes = StructureToByteArray(item);
            // Write bytes to file
            writer.Write(bytes, 0, bytes.Length);
        }

        public void Write(List<T> list)
        {
            foreach (T item in list)
                Write(item);
        }

        public void Flush()
        {
            writer.Flush();
        }

        public void Close()
        {
            writer.Close();
        }
    }

    public class BinaryReader<T> : BinaryStream
        where T : struct
    {
        System.IO.BinaryReader reader;
        public BinaryReader(string file)
        {
            reader = new System.IO.BinaryReader(new FileStream(file, FileMode.Open, FileAccess.Read));
        }


        public long Count()
        {
            long counter = 0;
            StreamReader s = new StreamReader(reader.BaseStream);
            while (!s.EndOfStream)
            {
                reader.ReadBytes(Marshal.SizeOf<T>());
                counter++;
            }
            return counter;
        }

        public T Read(long row)
        {
            return Get<T>(reader.BaseStream, row);
        }

        public T[] ReadAll()
        {
            return ImportBinaryFile<T>(reader.BaseStream);
        }
        public void Close()
        {
            reader.Dispose();
            reader.Close();
        }
    }
}


