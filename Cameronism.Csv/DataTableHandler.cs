using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Cameronism.Csv
{
    sealed internal class DataTableHandler : ReaderBase<DataRow, DataTable>
    {
        public DataTableHandler(char separator) : base(separator) { }

        public override void Serialize(DataTable table, Action<TextWriter, DataRow, int>[] writers, TextWriter destination)
        {
            WriteHeaders(table, writers, destination);

            var firstWriter = writers[0];
            foreach (DataRow row in table.Rows)
            {
                if (!row.IsNull(0))
                {
                    firstWriter.Invoke(destination, row, 0);
                }

                // remaining columns
                for (int i = 1; i < writers.Length; i++)
                {
                    destination.Write(_separator);
                    if (!row.IsNull(i))
                    {
                        writers[i].Invoke(destination, row, i);
                    }
                }
                destination.WriteLine();
            }
        }

        protected override int GetFieldCount(DataTable table) => table.Columns.Count;
        protected override Type GetFieldType(DataTable table, int i) => table.Columns[i].DataType;
        protected override string GetFieldName(DataTable table, int i) => table.Columns[i].ColumnName;

        // would love a more strongly typed way to handle the following (that doesn't do the same under the hood)
        protected override bool GetBoolean(DataRow row, int i) => (bool)row[i];
        protected override byte GetByte(DataRow row, int i) => (byte)row[i];
        protected override decimal GetDecimal(DataRow row, int i) => (decimal)row[i];
        protected override double GetDouble(DataRow row, int i) => (double)row[i];
        protected override float GetFloat(DataRow row, int i) => (float)row[i];
        protected override Guid GetGuid(DataRow row, int i) => (Guid)row[i];
        protected override DateTime GetDateTime(DataRow row, int i) => (DateTime)row[i];
        protected override short GetInt16(DataRow row, int i) => (short)row[i];
        protected override int GetInt32(DataRow row, int i) => (int)row[i];
        protected override long GetInt64(DataRow row, int i) => (long)row[i];
        protected override string GetString(DataRow row, int i) => (string)row[i];
    }
}
