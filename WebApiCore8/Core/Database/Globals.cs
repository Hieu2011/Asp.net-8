using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Database
{
    public class Globals
    {
        public enum DATATYPE
        {
            NUMBER,
            CHAR,
            VARCHAR,
            NVARCHAR,
            NTEXT,
            BINARY,
            BLOB,
            CLOB,
            NCLOB,
            SMALLINT,
            TIMESTAMP,
            BOOLEAN,
            BIGINT,
            INTEGER,
            TEXT,
            NUMERIC,
            DATE,
            DATETIME,
            REFCURSOR,
            BIT,
            TIME,
            DOUBLE,
            SINGLE,
            REAL
        }

        public static Hashtable ConvertHashTable(IDataReader drReader)
        {
            Hashtable hashtable = new Hashtable();
            if (drReader.Read())
            {
                for (int i = 0; i < drReader.FieldCount; i++)
                {
                    if (!hashtable.Contains(drReader.GetName(i)))
                    {
                        if (drReader[i] == null || drReader.IsDBNull(i))
                        {
                            hashtable.Add(drReader.GetName(i).ToUpper(), string.Empty);
                        }
                        else
                        {
                            hashtable.Add(drReader.GetName(i), drReader[i]);
                        }
                    }
                }
            }

            return hashtable;
        }

        public static ArrayList ConvertArrayList(IDataReader drReader)
        {
            ArrayList arrayList = new ArrayList();
            while (drReader?.Read() ?? false)
            {
                Hashtable hashtable = new Hashtable();
                for (int i = 0; i < drReader.FieldCount; i++)
                {
                    if (!hashtable.Contains(drReader.GetName(i)))
                    {
                        if (drReader.IsDBNull(i) || drReader[i] == null || drReader[i].ToString() == string.Empty)
                        {
                            hashtable.Add(drReader.GetName(i).ToUpper(), string.Empty);
                        }
                        else
                        {
                            hashtable.Add(drReader.GetName(i), drReader[i]);
                        }
                    }
                }

                arrayList.Add(hashtable);
            }

            return arrayList;
        }
    }
}
