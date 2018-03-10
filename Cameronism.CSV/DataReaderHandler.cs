using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Cameronism.Csv
{
    sealed internal class DataReaderHandler<T> : ReaderBase<T, T>
        where T : IDataReader
    {
        public DataReaderHandler(char separator) : base(separator) { }

        public override void Serialize(T reader, Action<TextWriter, T, int>[] writers, TextWriter destination)
        {
            WriteHeaders(reader, writers, destination);

            var firstWriter = writers[0];
            while (reader.Read())
            {
                // first column
                if (!reader.IsDBNull(0))
                {
                    firstWriter.Invoke(destination, reader, 0);
                }

                // remaining columns
                for (int i = 1; i < writers.Length; i++)
                {
                    destination.Write(_separator);
                    if (!reader.IsDBNull(i))
                    {
                        writers[i].Invoke(destination, reader, i);
                    }
                }
                destination.WriteLine();
            }

            // could generate a method that invokes members on a class for each type in sequence
        }

        protected override byte[] GetByteArray(T reader, int i) => (byte[])reader.GetValue(i);
        protected override bool GetBoolean(T reader, int i) => reader.GetBoolean(i);
        protected override byte GetByte(T reader, int i) => reader.GetByte(i);
        protected override decimal GetDecimal(T reader, int i) => reader.GetDecimal(i);
        protected override double GetDouble(T reader, int i) => reader.GetDouble(i);
        protected override float GetFloat(T reader, int i) => reader.GetFloat(i);
        protected override Guid GetGuid(T reader, int i) => reader.GetGuid(i);
        protected override DateTime GetDateTime(T reader, int i) => reader.GetDateTime(i);
        protected override short GetInt16(T reader, int i) => reader.GetInt16(i);
        protected override int GetInt32(T reader, int i) => reader.GetInt32(i);
        protected override long GetInt64(T reader, int i) => reader.GetInt64(i);
        protected override string GetString(T reader, int i) => reader.GetString(i);

        protected override int GetFieldCount(T reader) => reader.FieldCount;
        protected override string GetFieldName(T reader, int i) => reader.GetName(i);
        protected override Type GetFieldType(T reader, int i) => reader.GetFieldType(i);
    }
}
