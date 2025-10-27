using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Transactions;
using Npgsql;
using NpgsqlTypes;

namespace Core.Database
{
    public class PosgreSQLData : Data, IData
    {
        private string _strConnect;

        private NpgsqlConnection objConnection = null;

        private NpgsqlCommand objCommand = null;

        private NpgsqlTransaction objTransaction = null;

        private string strTableName = "TableNameDefault";

        private const string DEFAULT_REF = "@ref";

        IDbConnection IData.IConnection
        {
            get
            {
                return objConnection;
            }
            set
            {
                objConnection = (NpgsqlConnection)value;
            }
        }

        IDbTransaction IData.ITransaction
        {
            get
            {
                return objTransaction;
            }
            set
            {
                objTransaction = (NpgsqlTransaction)value;
            }
        }

        IDbCommand IData.ICommand
        {
            get
            {
                return objCommand;
            }
            set
            {
                objCommand = (NpgsqlCommand)value;
            }
        }

        public PosgreSQLData()
        {
        }

        public PosgreSQLData(string strConnect)
        {
            _strConnect = strConnect;
        }

        ~PosgreSQLData()
        {
            if (IsConnected())
            {
                Disconnect();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public bool Connect()
        {
            if (IsConnected())
            {
                return true;
            }

            if (objConnection == null)
            {
                string connectionString = _strConnect.Replace(";Unicode=True", string.Empty);
                objConnection = new NpgsqlConnection(connectionString);
            }

            try
            {
                objConnection.Open();
                objConnection.EnlistTransaction(Transaction.Current);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("ORA-02396"))
                {
                    objConnection.Open();
                    return true;
                }

                throw ex;
            }

            return true;
        }

        public bool Disconnect()
        {
            try
            {
                if (objCommand != null)
                {
                    objCommand.Dispose();
                }

                objConnection.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IsConnected()
        {
            if (objConnection == null || objConnection.State != ConnectionState.Open)
            {
                return false;
            }

            return true;
        }

        private NpgsqlCommand SetCommand(string strSQL)
        {
            objCommand = new NpgsqlCommand(strSQL, objConnection);
            if (objTransaction != null)
            {
                objCommand.Transaction = objTransaction;
            }

            return objCommand;
        }

        private NpgsqlDataAdapter SetDataAdapter(string strSQL)
        {
            return new NpgsqlDataAdapter(strSQL, objConnection);
        }

        private NpgsqlDataAdapter SetDataAdapter(NpgsqlCommand objCommand)
        {
            return new NpgsqlDataAdapter(objCommand);
        }

        private NpgsqlDbType GetOleDBDataType(Globals.DATATYPE enDataType)
        {
            NpgsqlDbType result = NpgsqlDbType.Integer;
            switch (enDataType)
            {
                case Globals.DATATYPE.INTEGER:
                    result = NpgsqlDbType.Integer;
                    break;
                case Globals.DATATYPE.CHAR:
                    result = NpgsqlDbType.Char;
                    break;
                case Globals.DATATYPE.VARCHAR:
                    result = NpgsqlDbType.Varchar;
                    break;
                case Globals.DATATYPE.TEXT:
                    result = NpgsqlDbType.Text;
                    break;
                case Globals.DATATYPE.BINARY:
                    result = NpgsqlDbType.Bytea;
                    break;
                case Globals.DATATYPE.BLOB:
                    result = NpgsqlDbType.Bytea;
                    break;
                case Globals.DATATYPE.CLOB:
                    result = NpgsqlDbType.Text;
                    break;
                case Globals.DATATYPE.NCLOB:
                    result = NpgsqlDbType.Text;
                    break;
                case Globals.DATATYPE.SMALLINT:
                    result = NpgsqlDbType.Smallint;
                    break;
                case Globals.DATATYPE.TIMESTAMP:
                    result = NpgsqlDbType.Timestamp;
                    break;
                case Globals.DATATYPE.BOOLEAN:
                    result = NpgsqlDbType.Boolean;
                    break;
                case Globals.DATATYPE.BIGINT:
                    result = NpgsqlDbType.Bigint;
                    break;
                case Globals.DATATYPE.NUMERIC:
                    result = NpgsqlDbType.Numeric;
                    break;
                case Globals.DATATYPE.DATE:
                    result = NpgsqlDbType.Date;
                    break;
                case Globals.DATATYPE.REFCURSOR:
                    result = NpgsqlDbType.Refcursor;
                    break;
            }

            return result;
        }

        private NpgsqlDbType GetPGSQLDataType(Globals.DATATYPE enDataType)
        {
            NpgsqlDbType result = NpgsqlDbType.Integer;
            switch (enDataType)
            {
                case Globals.DATATYPE.SMALLINT:
                    result = NpgsqlDbType.Smallint;
                    break;
                case Globals.DATATYPE.NUMBER:
                case Globals.DATATYPE.INTEGER:
                case Globals.DATATYPE.NUMERIC:
                    result = NpgsqlDbType.Integer;
                    break;
                case Globals.DATATYPE.BIGINT:
                    result = NpgsqlDbType.Bigint;
                    break;
                case Globals.DATATYPE.CHAR:
                    result = NpgsqlDbType.Char;
                    break;
                case Globals.DATATYPE.VARCHAR:
                    result = NpgsqlDbType.Varchar;
                    break;
                case Globals.DATATYPE.NVARCHAR:
                    result = NpgsqlDbType.Varchar;
                    break;
                case Globals.DATATYPE.NTEXT:
                    result = NpgsqlDbType.Text;
                    break;
                case Globals.DATATYPE.BINARY:
                    result = NpgsqlDbType.Bytea;
                    break;
                case Globals.DATATYPE.BLOB:
                    result = NpgsqlDbType.Bytea;
                    break;
                case Globals.DATATYPE.CLOB:
                    result = NpgsqlDbType.Text;
                    break;
                case Globals.DATATYPE.NCLOB:
                    result = NpgsqlDbType.Text;
                    break;
                case Globals.DATATYPE.TIMESTAMP:
                    result = NpgsqlDbType.Timestamp;
                    break;
                case Globals.DATATYPE.BOOLEAN:
                    result = NpgsqlDbType.Boolean;
                    break;
                case Globals.DATATYPE.BIT:
                    result = NpgsqlDbType.Bit;
                    break;
            }

            return result;
        }

        public void BeginTransaction()
        {
            if (!IsConnected())
            {
                Connect();
            }

            objTransaction = objConnection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (objTransaction != null)
            {
                objTransaction.Commit();
            }
        }

        public void RollBackTransaction()
        {
            if (objTransaction != null)
            {
                objTransaction.Rollback();
                objTransaction = null;
            }
        }

        public IDataReader ExecQueryToDataReader(string strSQL)
        {
            return SetCommand(strSQL).ExecuteReader();
        }

        public string ExecQueryToString(string strSQL)
        {
            object obj = SetCommand(strSQL).ExecuteScalar();
            if (obj == null)
            {
                return string.Empty;
            }

            return obj.ToString().Trim();
        }

        public byte[] ExecQueryToBinary(string strSQL)
        {
            return (byte[])SetCommand(strSQL).ExecuteScalar();
        }

        public void ExecUpdate(string strSQL)
        {
            SetCommand(strSQL).ExecuteNonQuery();
        }

        public void ExecUpdate(string strSQL, params IDataParameter[] objParameters)
        {
            SetCommand(strSQL);
            foreach (IDataParameter value in objParameters)
            {
                objCommand.Parameters.Add(value);
            }

            objCommand.ExecuteNonQuery();
        }

        public void ExecUpdate(string strSQL, ArrayList arrParameters)
        {
            SetCommand(strSQL);
            foreach (IDataParameter arrParameter in arrParameters)
            {
                objCommand.Parameters.Add(arrParameter);
            }

            objCommand.ExecuteNonQuery();
        }

        public IDataAdapter ExecQueryToDataAdapter(string strSQL)
        {
            return SetDataAdapter(strSQL);
        }

        public DataTable ExecQueryToDataTable(string strSQL)
        {
            DataSet dataSet = new DataSet();
            SetDataAdapter(strSQL).Fill(dataSet);
            return dataSet.Tables[0];
        }

        public DataSet ExecQueryToDataSet(string strSQL)
        {
            DataSet dataSet = new DataSet();
            SetDataAdapter(strSQL).Fill(dataSet);
            return dataSet;
        }

        public void CreateNewSqlText(string strSQL)
        {
            objCommand = SetCommand(strSQL);
            objCommand.CommandType = CommandType.Text;
        }

        public void CreateNewStoredProcedure(string strStoreProName)
        {
            strTableName = strStoreProName;
            objCommand = SetCommand(strStoreProName);
            objCommand.CommandType = CommandType.StoredProcedure;
        }

        public void CreateNewStoredProcedure(string storeProName, object objectParameters, bool hasParameter_VOut = false)
        {
            strTableName = storeProName;
            string value = "'v_out'::refcursor";
            if (objectParameters != null)
            {
                IDictionary<string, object> dictionary = ObjectExtensions.AsDictionary(objectParameters);
                string value2 = string.Join(", ", dictionary.Keys.Select((string p) => "@" + p).ToArray());
                if (hasParameter_VOut)
                {
                    objCommand = SetCommand($"SELECT * FROM {storeProName}({value},{value2})");
                }
                else
                {
                    objCommand = SetCommand($"SELECT * FROM {storeProName}({value2})");
                }

                foreach (KeyValuePair<string, object> item in dictionary)
                {
                    AddParameter(item.Key, item.Value);
                }
            }
            else if (hasParameter_VOut)
            {
                objCommand = SetCommand($"SELECT * FROM {storeProName}({value})");
            }
            else
            {
                objCommand = SetCommand("SELECT * FROM " + storeProName + "()");
            }

            objCommand.CommandType = CommandType.Text;
        }

        public void CreateNewStoredProcedure(string strStoreProName, int intTimeOut)
        {
            strTableName = strStoreProName;
            objCommand = SetCommand(strStoreProName);
            objCommand.CommandTimeout = intTimeOut;
            objCommand.CommandType = CommandType.StoredProcedure;
        }

        public void AddParameter(string strParameterName, object objValue)
        {
            if (objValue != null)
            {
                switch (objValue.GetType().Name)
                {
                    case "Boolean":
                        AddParameter(strParameterName, objValue, Globals.DATATYPE.BOOLEAN);
                        break;
                    case "Int64":
                        AddParameter(strParameterName, objValue, Globals.DATATYPE.BIGINT);
                        break;
                    case "Int16":
                        AddParameter(strParameterName, objValue, Globals.DATATYPE.SMALLINT);
                        break;
                    case "Double":
                        AddParameter(strParameterName, objValue, Globals.DATATYPE.NUMERIC);
                        break;
                    case "Decimal":
                        AddParameter(strParameterName, objValue, Globals.DATATYPE.NUMERIC);
                        break;
                    case "Int32":
                        AddParameter(strParameterName, objValue, Globals.DATATYPE.INTEGER);
                        break;
                    default:
                        objCommand.Parameters.AddWithValue(strParameterName, objValue);
                        break;
                }
            }
            else
            {
                objCommand.Parameters.AddWithValue(strParameterName, objValue);
            }
        }

        public void AddParameter(string strParameterName, object objValue, Globals.DATATYPE enDataType)
        {
            NpgsqlParameter npgsqlParameter = new NpgsqlParameter(strParameterName.Replace("@", "v_"), GetOleDBDataType(enDataType));
            npgsqlParameter.Value = objValue;
            objCommand.Parameters.Add(npgsqlParameter);
        }

        public void AddParameter(params object[] objArrParam)
        {
            bool flag = false;
            if (objArrParam.Length > 3 && objArrParam[2].GetType().Name == "DATATYPE")
            {
                flag = true;
            }

            if (flag)
            {
                for (int i = 0; i < objArrParam.Length; i += 3)
                {
                    AddParameter(objArrParam[i].ToString().Trim(), objArrParam[i + 1].ToString().Trim(), (Globals.DATATYPE)objArrParam[i + 2]);
                }
            }
            else
            {
                for (int j = 0; j < objArrParam.Length; j += 2)
                {
                    AddParameter(objArrParam[j].ToString().Trim(), objArrParam[j + 1]);
                }
            }
        }

        public void AddParameter(Hashtable hstParameter)
        {
            IDictionaryEnumerator enumerator = hstParameter.GetEnumerator();
            while (enumerator.MoveNext())
            {
                AddParameter(enumerator.Key.ToString(), enumerator.Value);
            }
        }

        public IDataReader ExecStoreToDataReader()
        {
            return ExecStoreToDataReader("");
        }

        public IDataReader ExecStoreToDataReader(string strOutParameter)
        {
            return objCommand.ExecuteReader();
        }

        public Hashtable ExecStoreToHashtable()
        {
            return ExecStoreToHashtable("");
        }

        public Hashtable ExecStoreToHashtable(string strOutParameter)
        {
            Hashtable hashtable = new Hashtable();
            NpgsqlDataReader npgsqlDataReader = objCommand.ExecuteReader();
            while (npgsqlDataReader.Read())
            {
                for (int i = 0; i < npgsqlDataReader.FieldCount; i++)
                {
                    hashtable.Add(npgsqlDataReader.GetName(i), npgsqlDataReader[i]);
                }
            }

            return hashtable;
        }

        public string ExecStoreToString()
        {
            return ExecStoreToString("");
        }

        public string ExecStoreToString(string strOutParameter)
        {
            object obj = objCommand.ExecuteScalar();
            if (obj == null)
            {
                return "";
            }

            return obj.ToString().Trim();
        }

        public byte[] ExecStoreToBinary()
        {
            return ExecStoreToBinary("");
        }

        public byte[] ExecStoreToBinary(string strOutParameter)
        {
            throw new Exception("Vui lòng không sử dụng hàm này!");
        }

        public int ExecNonQuery()
        {
            return objCommand.ExecuteNonQuery();
        }

        public IDataAdapter ExecStoreToDataAdapter()
        {
            return ExecStoreToDataAdapter("");
        }

        public IDataAdapter ExecStoreToDataAdapter(string strOutParameter)
        {
            return SetDataAdapter(objCommand);
        }

        public DataTable ExecStoreToDataTable(string strOutParameter)
        {
            if (!string.IsNullOrEmpty(strOutParameter))
            {
                NpgsqlParameter npgsqlParameter = new NpgsqlParameter();
                npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Refcursor;
                npgsqlParameter.Direction = ParameterDirection.Output;
                npgsqlParameter.Value = strOutParameter;
                objCommand.Parameters.Insert(0, npgsqlParameter);
            }

            DataTable dataTable = new DataTable(strTableName);
            StringBuilder stringBuilder = new StringBuilder();
            using (NpgsqlDataReader npgsqlDataReader = objCommand.ExecuteReader())
            {
                if (npgsqlDataReader.Read())
                {
                    StringBuilder stringBuilder2 = stringBuilder;
                    StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
                    handler.AppendLiteral("FETCH ALL IN \"");
                    handler.AppendFormatted<object>(npgsqlDataReader[0]);
                    handler.AppendLiteral("\";");
                    stringBuilder2.AppendLine(ref handler);
                }
            }

            if (stringBuilder.Length > 0)
            {
                using NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
                npgsqlCommand.Connection = objCommand.Connection;
                npgsqlCommand.Transaction = objCommand.Transaction;
                npgsqlCommand.CommandTimeout = objCommand.CommandTimeout;
                npgsqlCommand.CommandText = stringBuilder.ToString();
                npgsqlCommand.CommandType = CommandType.Text;
                using NpgsqlDataReader reader = npgsqlCommand.ExecuteReader();
                dataTable.Load(reader);
            }

            return dataTable;
        }

        public DataTable ExecStoreToDataTable()
        {
            UpChangeExecuteStoreToDataTable();
            DataTable dataTable = new DataTable(strTableName);
            StringBuilder stringBuilder = new StringBuilder();
            using (NpgsqlDataReader npgsqlDataReader = objCommand.ExecuteReader())
            {
                if (npgsqlDataReader.Read())
                {
                    StringBuilder stringBuilder2 = stringBuilder;
                    StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
                    handler.AppendLiteral("FETCH ALL IN \"");
                    handler.AppendFormatted<object>(npgsqlDataReader[0]);
                    handler.AppendLiteral("\";");
                    stringBuilder2.AppendLine(ref handler);
                }
            }

            string text = stringBuilder.ToString();
            if (!string.IsNullOrEmpty(text))
            {
                using NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
                npgsqlCommand.Connection = objCommand.Connection;
                npgsqlCommand.Transaction = objCommand.Transaction;
                npgsqlCommand.CommandTimeout = objCommand.CommandTimeout;
                npgsqlCommand.CommandText = text;
                npgsqlCommand.CommandType = CommandType.Text;
                using NpgsqlDataReader reader = npgsqlCommand.ExecuteReader();
                dataTable.Load(reader);
            }

            return dataTable;
        }

        public async Task<DataTable> ExecStoreToDataTableAsync()
        {
            DataTable dataTable = new DataTable(strTableName);
            StringBuilder sql = new StringBuilder();
            using (NpgsqlDataReader npgsqlDataReader = await objCommand.ExecuteReaderAsync())
            {
                if (npgsqlDataReader.Read())
                {
                    StringBuilder stringBuilder = sql;
                    StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder);
                    handler.AppendLiteral("FETCH ALL IN \"");
                    handler.AppendFormatted<object>(npgsqlDataReader[0]);
                    handler.AppendLiteral("\";");
                    stringBuilder.AppendLine(ref handler);
                }
            }

            string sqlQuery = sql.ToString();
            if (!string.IsNullOrEmpty(sqlQuery))
            {
                using NpgsqlCommand cmd2 = new NpgsqlCommand();
                cmd2.Connection = objCommand.Connection;
                cmd2.Transaction = objCommand.Transaction;
                cmd2.CommandTimeout = objCommand.CommandTimeout;
                cmd2.CommandText = sqlQuery;
                cmd2.CommandType = CommandType.Text;
                using NpgsqlDataReader reader = await cmd2.ExecuteReaderAsync();
                dataTable.Load(reader);
            }

            return dataTable;
        }

        public DataSet ExecStoreToDataSet()
        {
            return ExecStoreToDataSet("");
        }

        public DataSet ExecStoreToDataSet(params string[] strOutParameter)
        {
            DataSet dataSet = new DataSet();
            List<string> list = new List<string>();
            int num = 0;
            using (NpgsqlDataReader npgsqlDataReader = objCommand.ExecuteReader(CommandBehavior.SequentialAccess))
            {
                while (npgsqlDataReader.Read())
                {
                    list.Add($"FETCH ALL IN \"{npgsqlDataReader[0]}\";");
                }
            }

            foreach (string item in list)
            {
                using NpgsqlCommand npgsqlCommand = new NpgsqlCommand();
                npgsqlCommand.Connection = objCommand.Connection;
                npgsqlCommand.Transaction = objCommand.Transaction;
                npgsqlCommand.CommandTimeout = objCommand.CommandTimeout;
                npgsqlCommand.CommandText = item.ToString();
                npgsqlCommand.CommandType = CommandType.Text;
                using (NpgsqlDataReader reader = npgsqlCommand.ExecuteReader())
                {
                    dataSet.Tables.Add(new DataTable());
                    dataSet.Tables[num].Load(reader);
                }

                num++;
            }

            return dataSet;
        }

        public void CreateNewBuckCopy(string strTableName, DataTable table)
        {
            throw new NotImplementedException();
        }

        public List<object> ExecStoreToListObject()
        {
            return new List<object>();
        }

        public List<object> ExecStoreToListObject(string strOutParameter)
        {
            return new List<object>();
        }

        private void ProcessException(Exception ex)
        {
            if (objTransaction != null || !ex.Message.ToString().Contains("16000") || !ex.Message.ToString().Contains("ORA"))
            {
                return;
            }

            //IData data = Data.CreateData(_strConnect.Replace("RO", "RW"));
            //try
            //{
            //    data.Connect();
            //    data.ExecUpdate("ALTER PROCEDURE " + objCommand.CommandText + " COMPILE");
            //}
            //catch
            //{
            //}
            //finally
            //{
            //    data.Disconnect();
            //}
        }

        private void UpChangeExecuteStoreToDataTable()
        {
            string commandText = objCommand.CommandText;
            if (commandText.ToLower().Contains("select"))
            {
                return;
            }

            NpgsqlParameterCollection parameters = objCommand.Parameters;
            if (parameters != null && parameters.Count > 0)
            {
                string value = string.Join(", ", (from p in objCommand.Parameters
                                                  select p.ParameterName into p
                                                  select "@" + p).ToArray());
                commandText = $"select * from {strTableName}({value})";
                objCommand.CommandText = commandText;
                objCommand = SetCommand(commandText);
            }
            else
            {
                commandText = "select * from " + strTableName + "()";
                objCommand.CommandText = commandText;
                objCommand = SetCommand(commandText);
            }

            objCommand.CommandType = CommandType.Text;
        }

        private void UpChangeExecNonQuery()
        {
            string commandText = objCommand.CommandText;
            if (commandText.ToLower().Contains("select"))
            {
                return;
            }

            NpgsqlParameterCollection parameters = objCommand.Parameters;
            if (parameters != null && parameters.Count > 0)
            {
                string value = string.Join(", ", (from p in objCommand.Parameters
                                                  select p.ParameterName into p
                                                  select "@" + p).ToArray());
                commandText = $"select {strTableName}({value})";
                objCommand.CommandText = commandText;
                objCommand = SetCommand(commandText);
            }
            else
            {
                commandText = "select " + strTableName + "()";
                objCommand.CommandText = commandText;
                objCommand = SetCommand(commandText);
            }

            objCommand.CommandType = CommandType.Text;
        }

        private void UpChangeExecStoreToString()
        {
            string commandText = objCommand.CommandText;
            if (commandText.ToLower().Contains("select"))
            {
                return;
            }

            NpgsqlParameterCollection parameters = objCommand.Parameters;
            if (parameters != null && parameters.Count > 0)
            {
                string value = string.Join(", ", (from p in objCommand.Parameters
                                                  select p.ParameterName into p
                                                  select "@" + p).ToArray());
                commandText = $"select {strTableName}({value})";
                objCommand.CommandText = commandText;
                objCommand = SetCommand(commandText);
            }
            else
            {
                commandText = "select " + strTableName + "()";
                objCommand.CommandText = commandText;
                objCommand = SetCommand(commandText);
            }

            objCommand.CommandType = CommandType.Text;
        }

        public void AddArrayBindingParam<T>(string parameterName, List<T> paramList, Globals.DATATYPE enDataType, int valueSize = 0)
        {
            throw new NotImplementedException();
        }

        void IData.Connect()
        {
            throw new NotImplementedException();
        }

        void IData.Disconnect()
        {
            throw new NotImplementedException();
        }
    }
}
