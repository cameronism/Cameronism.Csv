using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Cameronism.Csv
{
    internal abstract class ReaderBase<TRow, TTable>
    {
        protected char _separator;
        protected char[] _charsToEscape;

        protected ReaderBase(char separator)
        {
            _separator = separator;
            _charsToEscape = new[] { separator, '"', '\r', '\n' };
        }

        protected abstract string GetFieldName(TTable reader, int i);
        protected abstract int GetFieldCount(TTable reader);
        protected abstract Type GetFieldType(TTable reader, int i);
        protected abstract Guid GetGuid(TRow reader, int i);
        protected abstract byte[] GetByteArray(TRow reader, int i);
        protected abstract DateTime GetDateTime(TRow reader, int i);
        protected abstract bool GetBoolean(TRow reader, int i);
        protected abstract decimal GetDecimal(TRow reader, int i);
        protected abstract double GetDouble(TRow reader, int i);
        protected abstract float GetFloat(TRow reader, int i);
        protected abstract byte GetByte(TRow reader, int i);
        protected abstract int GetInt32(TRow reader, int i);
        protected abstract long GetInt64(TRow reader, int i);
        protected abstract short GetInt16(TRow reader, int i);
        protected abstract string GetString(TRow reader, int i);

        public Action<TextWriter, TRow, int>[] GetWriters(TTable reader)
        {
            var writers = new Action<TextWriter, TRow, int>[GetFieldCount(reader)];
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

                Action<TextWriter, TRow, int> writer;
                var type = GetFieldType(reader, i);
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
                    case "System.DateTime":
                        writer = WriteDateTime;
                        break;
                    case "System.Byte[]":
                        writer = WriteBytes;
                        break;
                    default:
                        throw new NotImplementedException();

                }
                writers[i] = writer;
            }

            return writers;
        }

        private void WriteGuid(TextWriter destination, TRow reader, int index)
        {
            var value = GetGuid(reader, index);
            var s = value.ToString();
            destination.Write(s);
        }

        private void WriteBytes(TextWriter destination, TRow reader, int index)
        {
            var value = GetByteArray(reader, index);
            var s = Convert.ToBase64String(value);
            destination.Write(s);
        }

        private void WriteDateTime(TextWriter destination, TRow reader, int index)
        {
            var value = GetDateTime(reader, index);
            var s = value.ToString(BuildFlattener.DateTimeFormat, CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteBoolean(TextWriter destination, TRow reader, int index)
        {
            var value = GetBoolean(reader, index);
            destination.Write(value);
        }

        private void WriteByte(TextWriter destination, TRow reader, int index)
        {
            var value = GetByte(reader, index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteInt16(TextWriter destination, TRow reader, int index)
        {
            var value = GetInt16(reader, index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteInt32(TextWriter destination, TRow reader, int index)
        {
            var value = GetInt32(reader, index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteInt64(TextWriter destination, TRow reader, int index)
        {
            var value = GetInt64(reader, index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteFloat(TextWriter destination, TRow reader, int index)
        {
            var value = GetFloat(reader, index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteDouble(TextWriter destination, TRow reader, int index)
        {
            var value = GetDouble(reader, index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteDecimal(TextWriter destination, TRow reader, int index)
        {
            var value = GetDecimal(reader, index);
            var s = value.ToString(CultureInfo.InvariantCulture);
            destination.Write(s);
        }

        private void WriteString(TextWriter destination, TRow reader, int index)
        {
            WriteString(destination, GetString(reader, index));
        }

        /// <summary>
        /// MUST NOT be called with null
        /// </summary>
        protected void WriteString(TextWriter destination, string value)
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

        protected void WriteHeaders(TTable reader, Action<TextWriter, TRow, int>[] writers, TextWriter destination)
        {
            WriteString(destination, GetFieldName(reader, 0));
            for (int i = 1; i < writers.Length; i++)
            {
                destination.Write(_separator);
                WriteString(destination, GetFieldName(reader, i));
            }
            destination.WriteLine();
        }

        public abstract void Serialize(TTable reader, Action<TextWriter, TRow, int>[] writers, TextWriter destination);
    }
}
