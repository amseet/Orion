using Orion.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Orion.Models.TripRecordModel;

namespace Orion.Core
{
    public class TripRecordContext : ICollection<DateTime>
    {      
        protected BinaryReader<TripRecord> reader;
        protected Dictionary<DateTime, List<long>> LookupTable;     //Indexed by DateTime
        protected string DataFile;

        protected TripRecordContext(string dataFile)
        {
            DataFile = dataFile;
        }

        public int Count => LookupTable.Count;

        public bool IsReadOnly => true;

        public void Add(DateTime item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(DateTime item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(DateTime[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public TripRecord[] Get(DateTime dateTime)
        { 
            if(reader == null)
                reader = new BinaryReader<TripRecord>(DataFile);
            DateTime Key = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
            List<long> values = LookupTable[Key];
            List<TripRecord> list = new List<TripRecord>();
            foreach (var value in values)
                list.Add(reader.Read(value));
            return list.ToArray();
        }

        public IEnumerator<DateTime> GetEnumerator()
        {
            return LookupTable.Keys.GetEnumerator();
        }

        public bool Remove(DateTime item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return LookupTable.Keys.GetEnumerator();
        }
    }

}
