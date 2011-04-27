using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono;
using System.Collections;

namespace NDESharp
{
    /**
     * A record is basically some fields. As you can see here. :P
     */
    public class NDEFileRecord
    {
        public List<NDEField> fields;

        public NDEFileRecord()
        {
            fields = new List<NDEField>();
        }
    }

    /**
     * A record, our internal representation. Nothing's here, look at
     * NDEDatabase::__construct
     */
    public class NDERecord
    {
        public Dictionary<int, object> variable;

        public NDERecord()
        {
            variable = new Dictionary<int, object>();
        }
    }
    /**
 * A NDE Field
 * Format information (from Winamp SDK):
	==================================================================================================
	Offset                      Data Type      Size                  Field
	==================================================================================================
	0                           UCHAR          1                     Column ID
	1                           UCHAR          1                     Field Type
	2                           INT            4                     Size of field data
	6                           INT            4                     Next field position in table data pool
	10                          INT            4                     Previous field position in table data pool
	14                          FIELDDATA      SizeOfFieldData       Field data
	==================================================================================================
 */
    public class NDEField
    {
        /**
         * All the different field types
         */
        public const int FIELD_UNDEFINED = 255;
        public const int FIELD_COLUMN = 0;
        public const int FIELD_INDEX = 1;
        public const int FIELD_REDIRECTOR = 2;
        public const int FIELD_STRING = 3;
        public const int FIELD_INTEGER = 4;
        public const int FIELD_BOOLEAN = 5;
        public const int FIELD_BINARY = 6;
        public const int FIELD_GUID = 7;
        public const int FIELD_FLOAT = 9;
        public const int FIELD_DATETIME = 10;
        public const int FIELD_LENGTH = 11;
        public const int FIELD_FILEPATH = 12;

        public char id;
        public char type;
        public int size;
        public int next;
        public int prev;
        public byte[] raw;
        public object data;

        /**
         * Creates a new NDEField.
         * 
         * When an instance of this class is created, we need to:
         * 1. Get all the data (using the format information as shown above)
         * 2. Set the "data" variable based on the type of field this is.
         */
        public NDEField(byte[] somedata)
        {
            // First two things are unsigned characters (UCHAR).
            IList stuff = DataConverter.Unpack("2C", somedata.Take(2).ToArray(),0);
            this.id = (char)stuff[0];
            this.type = (char)stuff[1];
            // Next three are integers.
            stuff = DataConverter.Unpack("3i", somedata.Skip(2).Take(12).ToArray(),0);
            this.size = (int)stuff[0];
            this.next = (int)stuff[1];
            // And this is the rest of the data.
            this.raw = somedata.Skip(13).Take(this.size).ToArray();


            // Actually get the data, depending on type.
            switch (Convert.ToInt32(this.type))
            {
                case FIELD_COLUMN:
                    this.data = new NDEField_Column(this.raw).name;
                    break;

                // I don't actually know what these are, so they're ignored for now.
                case FIELD_INDEX:
                    break;

                case FIELD_STRING:
                case FIELD_FILEPATH:
                    this.data = new NDEField_String(this.raw).data;
                    break;

                case FIELD_INTEGER:
                case FIELD_LENGTH:
                    this.data = new NDEField_Integer(this.raw).data;
                    break;

                case FIELD_DATETIME:
                    this.data = new NDEField_DateTime(this.raw).data;
                    break;

                // Shouldn't really happen. Yes, I know I haven't implemented all
                // the different types, but the above ones are the only ones that
                // seem to be used in the media library.
                default:
                    //failure
                    break;
            }
        }

    }

    /**
     * All data types inherit from this class
     */
    public abstract class NDEField_Data
    {
        
    }

    /**
     * NDE "Column" type
     * Format information:
        ==================================================================================================
        Offset                      Data Type      Size                  Field
        ==================================================================================================
        0                           UCHAR          1                     Column Field Type (ie, FIELD_INTEGER)
        1                           UCHAR          1                     Index unique values (0/1)
        2                           UCHAR          1                     Size of column name string
        3                           STRING         SizeOfColumnName      Public name of the column
        ==================================================================================================
    */
    public class NDEField_Column : NDEField_Data
    {
        public char type;
        public char unique;
        public char size;
        public string name;

        public NDEField_Column(byte[] data)
        {
            // Characters (UCHARs)
            IList stuff = DataConverter.Unpack("3C", data,0);
            this.type = (char)stuff[0];
            this.unique = (char)stuff[1];
            this.size = (char)stuff[2];
            // Name = rest of the data
            this.name = System.Text.Encoding.ASCII.GetString(data.Skip(3).Take(size).ToArray());
        }

        public override string ToString()
        {
            return name;
        }
    }

    /**
     * NDE "String" type
     * Format information:
        ==================================================================================================
        Offset                      Data Type      Size                  Field
        ==================================================================================================
        0                           USHORT         2                     Size of string
        2                           STRING         SizeOfString          String
        ==================================================================================================
    */
    public class NDEField_String : NDEField_Data
    {
        public ushort size;
        public string data;

        public NDEField_String(byte[] data)
        {
            // Unsigned short
            this.size = (ushort)(DataConverter.Unpack("S", data.Take(2).ToArray(), 0)[0]);

            UnicodeEncoding encoder = new UnicodeEncoding();

            this.data = encoder.GetString(data.Skip(2).Take(size).ToArray());

        }

        public override string ToString()
        {
            return this.data;
        }
    }

    /**
     * NDE "Integer" type. I think this is the simplest one :)
     * Format information:
        ==================================================================================================
        Offset                      Data Type      Size                  Field
        ==================================================================================================
        0                           INT            4                     Integer value
        ==================================================================================================
    */
    public class NDEField_Integer : NDEField_Data
    {
        public int data;

        public NDEField_Integer(byte[] data)
        {
            this.data = (int)(DataConverter.Unpack("i", data, 0)[0]);

        }

        public override string ToString()
        {
            return this.data.ToString();
        }
    }

    /**
     * NDE "DateTime" type. This is exact same as an integer, except it's treated
     * as a date/time format. The number is a UNIX timestamp.
     *
     * TODO: Add a field for formatted date?
    */
    public class NDEField_DateTime : NDEField_Data
    {
        public DateTime data;

        public NDEField_DateTime(byte[] data)
        {
            this.data = ConvertFromUnixTimestamp(Convert.ToDouble((int)(DataConverter.Unpack("i",data,0)[0])));
        }

        public override string ToString()
        {
            return this.data.ToString();
        }

        static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }
    }
}
