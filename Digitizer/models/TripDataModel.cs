using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Digitizer.models
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class CSVAttribute : Attribute
    {
        private int columnPosition;
        // Zero index position of the column.
        public int ColumnPosition { get { return columnPosition; } }

        public CSVAttribute(int ColumnPosition)
        {
            this.columnPosition = ColumnPosition;
        }

        public static int GetColumnPosition<T>(string MemberName) where T : class, new()
        {
            MemberInfo[] members = typeof(T).GetMember(MemberName);
            return members[0].GetCustomAttribute<CSVAttribute>().ColumnPosition;
        }
    }

    public class TripDataModel
    {

        [CSV(ColumnPosition: 1)]
        public DateTime Pickup_datetime;
        
        public TripDataModel()
        {
            //CSVAttribute.GetColumnPosition<TripDataModel>(nameof(nyc.Pickup_datetime));
        } 
    }
}
