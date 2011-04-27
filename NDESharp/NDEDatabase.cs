using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono;
using System.IO;
using System.Collections;
using System.Data;

namespace NDESharp
{
    public class NDEIndex
    {
        // This is the very first thing in the file. It's used to verify that the
        // file actually is a NDE Index.
        const string SIGNATURE = "NDEINDEX";
        public uint record_count;
        private FileStream readStream;
        private BinaryReader readBinary;

        /**
         * When we create this class, we'd better load the file, and check how long
         * it is.
         */
        public NDEIndex(string file)
        {
            byte[] temp;
            try
            {
                readStream = new FileStream(file, FileMode.Open);
                readBinary = new BinaryReader(readStream);
                temp = readBinary.ReadBytes(SIGNATURE.Length);
                // Number of records in the file
                record_count = (uint)DataConverter.Unpack("I", readBinary.ReadBytes(4), 0)[0];
                // Bytes that don't seem to do anything
                temp = readBinary.ReadBytes(4);

            }
            catch (Exception ex)
            {
                //failure 
            }
        }

        ~NDEIndex()
        {
            readStream.Close();
        }

        /**
         * Get the next index from the index file.
         */
        public IndexData get()
        {
            IList data = DataConverter.Unpack("2I", readBinary.ReadBytes(8), 0);
            return new IndexData { offset = (uint)data[0], index = (uint)data[1] };
        }
    }

    public class IndexData
    {
        public uint offset { get; set; }
        public uint index { get; set; }
    }

    /**
     * NDE Data file class
     */
    public class NDEData
    {
        // This is the very first thing in the file. It's used to verify that the
        // file actually is a NDE Table.
        const string SIGNATURE = "NDETABLE";

        // The order the columns are in. This is defined by the "Column" field,
        // which is the first one in the file.

        public List<string> columns;

        private FileStream readStream;
        private BinaryReader readBinary;


        /**
         * Just load the file and check the signature
         */
        public NDEData(string file)
        {
            columns = new List<string>();
            byte[] temp;
            try
            {
                readStream = new FileStream(file, FileMode.Open);
                readBinary = new BinaryReader(readStream);
                temp = readBinary.ReadBytes(SIGNATURE.Length);
                
            }
            catch (Exception ex)
            {
                //failure 
            }
        }

        /**
         * Get a record from the file. One record consists of many fields, in a
         * linked list. Firstly, this gets the first field (identified by the offset
         * passed to this function) and reads that. Then, it checks if it has a next
         * field to go to (the field will contain this data). If so, it goes to that
         * field, and reads it. This continues until we have no more fields in the
         * record. After that, we check what type of field it is.
         * 
         * The two "other" types are "column" and "index". I don't know what the 
         * index type actually does, but the "column" type tells us all the
         * information stored about songs. The very first record in the file is a 
         * "column" record, and the second record is a "index" record. The rest of
         * the file is all information about songs.
         */
        public NDERecord get_record(uint offset, uint index)
        {
            NDEFileRecord record = new NDEFileRecord();

            // While we have fields to get
            do
            {
                // Go to this offset
                readStream.Seek(Convert.ToInt64(offset),0);
                // Read some stuff
                byte[] data = readBinary.ReadBytes(14);
                // Find out the length we need to read from the file

                int size = (int)((DataConverter.Unpack("i", data.Skip(2).Take(4).ToArray(), 0))[0]);
                // Add this data
                data = Array.Add(data, readBinary.ReadBytes(Convert.ToInt32(size)));
                // The actual field itself
                NDEField field = new NDEField(data);
                record.fields.Add(field);
                
                // Do we have another one in this series? Better grab the offset
                offset = Convert.ToUInt32(field.next);
            }
            while (offset != 0);

            // Is this the "column" field?
            if (record.fields[0].type == NDEField.FIELD_COLUMN)
            {
                // We need to fill our columns variable!
                foreach (NDEField field in record.fields)
                    this.columns.Add(((string)field.data).Replace("\0", ""));

                    return null;
            }
            // otherwise, it could be that weird index one.
            else if (record.fields[0].type == NDEField.FIELD_INDEX)
            {
                // TODO: Find out what this field actually is.
                return null;
            }

            // Otherwise, it's a song!
            // We need to store all the data stuffs
            NDERecord song = new NDERecord();

            foreach (NDEField afield in record.fields)
            {
                //string variable = this.columns[field.id];
                if (afield.data != null)
                {
                    song.variable.Add(afield.id,afield.data);
                }
            }

            return song;
        }
    }

    /**
     * NDE Database
     * 
     * A database is basically an index file, and a data file. The index file tells 
     * us where to go to get data, and the data file tells us where the data 
     * actually is. This class coordinates the two files.
     */
    public class NDEDatabase
    {
        public DataSet SongDS;
        

        public List<NDERecord> records;

        /**
         * Creates a new instance of the NDEDatabase class.
         *
         * When an instance of this class is created, we need to:
         * 1. Load the index file (basefile + '.idx').
         * 2. Load the data file (basefile + '.dat').
         * 3. Loop through all the indices, and load the corresponding data.
         */
        public NDEDatabase(string basefile)
        {
            bool columnsAdded = false;
            SongDS = new DataSet();
            DataTable songTable = SongDS.Tables.Add();

            records = new List<NDERecord>();
            NDEIndex index = new NDEIndex(basefile + ".idx");
            NDEData data = new NDEData(basefile + ".dat");

            // Need to read in all the records
            for (int i = 0; i < index.record_count; i++)
            {
                // Get the next index from the index file
                var index_data = index.get();
                uint test = index_data.offset;
                // Now, get the data associated with this index.
                var record = data.get_record(index_data.offset, index_data.index);
                // Was it a record? If so, process it.
                if (record == null && columnsAdded == false)
                {
                    //add columns
                    foreach (string column in data.columns)
                    {
                        if (column != null)
                        {
                            if (column != "tuid2") //just ignore it, it seems to stuff everything up
                            {
                                songTable.Columns.Add(column, typeof(String));
                            }
                        }
                    }
                    columnsAdded = true;
                }
                else if (record != null)
                {
                    DataRow row = songTable.NewRow();
                    int loc;
                    foreach (var rec in record.variable)
                    {
                        loc = rec.Key;
                        if (loc > 12) loc--; //weird glitch that puts column data out.
                        row[data.columns[loc]] = rec.Value;
                    }
                    songTable.Rows.Add(row);
                }
            }
        }

    }
}
