using System;
using System.Linq;
using System.Numerics;
using NpgsqlTypes;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Collections;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common
{
    /// <summary>
    /// This class is used to test compatability with converting various data types to their respective
    /// PostgreSQL types.
    /// </summary>
    public class ProductColumnTypes
    {
        /// <summary>
        /// The ProductId is the primary key for the Products table.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// The Bigint column type is a 64-bit signed integer.
        /// </summary>
        public long Bigint { get; set; }

        /// <summary>
        /// The Bigserial column type is an auto-incrementing 64-bit signed integer.
        /// </summary>
        public BigInteger Bigserial { get; set; }

        /// <summary>
        /// The Bit column type represents a binary value.
        /// </summary>
        public bool Bit { get; set; }

        /// <summary>
        /// The BitVarying column type represents a binary value with a variable length.
        /// </summary>
        public BitArray BitVarying { get; set; }

        /// <summary>
        /// The Boolean column type represents a true/false value.
        /// </summary>
        public bool Boolean { get; set; }

        /// <summary>
        /// The Bytea column type represents binary data.
        /// </summary>
        public byte[] Bytea { get; set; }

        /// <summary>
        /// The Character column type represents a fixed-length string.
        /// </summary>
        public string Character { get; set; }

        /// <summary>
        /// The CharacterVarying column type represents a variable-length string.
        /// </summary>
        public string CharacterVarying { get; set; }

        /// <summary>
        /// The Date column type represents a calendar date.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The DoublePrecision column type represents a double precision floating-point number.
        /// </summary>
        public double DoublePrecision { get; set; }

        /// <summary>
        /// The Integer column type represents a 32-bit signed integer.
        /// </summary>
        public int Integer { get; set; }

        /// <summary>
        /// The Interval column type represents a time span.
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// The Json column type represents a JSON data structure.
        /// </summary>
        public JObject Json { get; set; }

        /// <summary>
        /// The Jsonb column type represents a JSON data structure stored in a binary format.
        /// </summary>
        public JObject Jsonb { get; set; }

        /// <summary>
        /// The Numeric column type represents a exact numeric number with a user-specified precision.
        /// </summary>
        public decimal Numeric { get; set; }

        /// <summary>
        /// The Real column type represents a single precision floating-point number.
        /// </summary>
        public float Real { get; set; }

        /// <summary>
        /// The Smallint column type represents a 16-bit signed integer.
        /// </summary>
        public short Smallint { get; set; }

        /// <summary>
        /// The Smallserial column type represents an auto-incrementing 16-bit signed integer.
        /// </summary>
        public short Smallserial { get; set; }

        /// <summary>
        /// The Serial column type represents an auto-incrementing 32-bit signed integer.
        /// </summary>
        public int Serial { get; set; }

        /// <summary>
        /// The Text column type represents a variable-length string.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The Time column type represents a time of day.
        /// </summary>
        public TimeSpan Time { get; set; }

        /// <summary>
        /// The TimeWithTimeZone column type represents a time of day including time zone.
        /// </summary>
        public DateTimeOffset TimeWithTimeZone { get; set; }

        /// <summary>
        /// The Timestamp column type represents a date and time.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The TimestampWithTimeZone column type represents a date and time, including time zone.
        /// </summary>
        public DateTimeOffset TimestampWithTimeZone { get; set; }

        /// <summary>
        /// The Uuid column type represents a universally unique identifier.
        /// </summary>
        public Guid Uuid { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is ProductColumnTypes)
            {
                var that = obj as ProductColumnTypes;
                return this.ProductId == that.ProductId && this.Bigint == that.Bigint && this.Bit == that.Bit &&
                    this.Bytea.SequenceEqual(that.Bytea) && this.Character == that.Character &&
                    this.CharacterVarying == that.CharacterVarying && this.Date == that.Date &&
                    this.DoublePrecision == that.DoublePrecision && this.Integer == that.Integer &&
                    this.Interval == that.Interval && this.Json.ToString() == that.Json.ToString() &&
                    this.Jsonb.ToString() == that.Jsonb.ToString() && this.Numeric == that.Numeric &&
                    this.Real == that.Real && this.Smallint == that.Smallint && this.Smallserial == that.Smallserial &&
                    this.Serial == that.Serial && this.Text == that.Text && this.Time == that.Time &&
                    this.TimeWithTimeZone == that.TimeWithTimeZone && this.Timestamp == that.Timestamp &&
                    this.TimestampWithTimeZone == that.TimestampWithTimeZone && this.Uuid == that.Uuid;
            }
            return false;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{this.ProductId}, {this.Bigint}, {this.Bit}, {this.Bytea}, {this.Character}, {this.CharacterVarying}, {this.Date}, {this.DoublePrecision}, {this.Integer}, {this.Interval}, {this.Json}, {this.Jsonb}, {this.Numeric}, {this.Real}, {this.Smallint}, {this.Smallserial}, {this.Serial}, {this.Text}, {this.Time}, {this.TimeWithTimeZone}, {this.Timestamp}, {this.TimestampWithTimeZone}, {this.Uuid}]";
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}