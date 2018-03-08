using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Cameronism.Csv
{
    internal class DataReaderHandler<T>
        where T : IDataReader
    {
        private char _separator;
        private char[] _charsToEscape;

        public DataReaderHandler(char separator)
        {
            _separator = separator;
            _charsToEscape = new[] { separator, '"', '\r', '\n' };
        }

        public Action<TextWriter, T, int>[] GetWriters(T reader)
        {
            var writers = new Action<TextWriter, T, int>[reader.FieldCount];
            for (int i = 0; i < writers.Length; i++)
            {
                //DateTime GetDateTime(int i);
                //char GetChar(int i);

                //Guid GetGuid(int i);
                //bool GetBoolean(int i);
                //decimal GetDecimal(int i);
                //double GetDouble(int i);
                //float GetFloat(int i);
                //byte GetByte(int i);
                //int GetInt32(int i);
                //long GetInt64(int i);
                //short GetInt16(int i);
                //string GetString(int i);

                //long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length);
                //long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length);

                Action<TextWriter, T, int> writer;
                var type = reader.GetFieldType(i);
                switch (type.FullName)
                {
                    case "System.String":
                        writer = WriteString;
                        break;
                    case "System.Boolean":
                        writer = WriteBoolean;
                        break;
                    case "System.Byte":
                        writer = WriteByte;
                        break;
                    case "System.Int16":
                        writer = WriteInt16;
                        break;
                    case "System.Int32":
                        writer = WriteInt32;
                        break;
                    case "System.Int64":
                        writer = WriteInt64;
                        break;
                    case "System.Float":
                        writer = WriteFloat;
                        break;
                    case "System.Double":
                        writer = WriteDouble;
                        break;
                    case "System.Decimal":
                        writer = WriteDecimal;
                        break;
                    case "System.Guid":
                        writer = WriteGuid;
                        break;
                    default:
                        throw new NotImplementedException();

                }
                writers[i] = writer;
            }

            return writers;
        }

        private void WriteGuid(TextWriter destination, T reader, int index)
        {
            var value = reader.GetGuid(index);
            var s = value.ToString();
            destination.Write(s);
        }

        private void WriteBoolean(TextWriter destination, T reader, int index)
        {
            var value = reader.GetBoolean(index);
            destination.Write(value);
        }

        private void WriteByte(TextWriter destination, T reader, int index)
        {
            var value = reader.GetByte(index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteInt16(TextWriter destination, T reader, int index)
        {
            var value = reader.GetInt16(index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteInt32(TextWriter destination, T reader, int index)
        {
            var value = reader.GetInt32(index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteInt64(TextWriter destination, T reader, int index)
        {
            var value = reader.GetInt64(index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteFloat(TextWriter destination, T reader, int index)
        {
            var value = reader.GetFloat(index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteDouble(TextWriter destination, T reader, int index)
        {
            var value = reader.GetDouble(index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteDecimal(TextWriter destination, T reader, int index)
        {
            var value = reader.GetDecimal(index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteString(TextWriter destination, T reader, int index)
        {
            WriteString(destination, reader.GetString(index));
        }

        /// <summary>
        /// MUST NOT be called with null
        /// </summary>
        private void WriteString(TextWriter destination, string value)
        {
            var index = value.IndexOfAny(_charsToEscape);
            if (index == -1)
            {
                destination.Write(value);
                return;
            }

            destination.Write('"');
            destination.Write(value.Replace("\"", "\"\""));
            destination.Write('"');
        }


        public void Serialize(T reader, Action<TextWriter, T, int>[] writers, TextWriter destination)
        {
            // headers
            WriteString(destination, reader.GetName(0));
            for (int i = 1; i < writers.Length; i++)
            {
                destination.Write(_separator);
                WriteString(destination, reader.GetName(i));
            }
            destination.WriteLine();

            var firstWriter = writers[0];

            // values
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
    }
}
