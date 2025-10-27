using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Core.Database
{
    public class Data
    {
        public enum DATABASETYPE
        {
            NONE,
            SQLSERVER,
            ORACLE,
            MySQL,
            MsAccess,
            PosgreSQL,
            SQLite
        }

        protected const string DEFAULT_OUT_PARAMETER = "v_Out";

        protected const int DEFAULT_OUT_PARAMETER_LENGTH = 4000;

        protected static string strPassword = "1900@tgdd1292.com.vn";

        public static DATABASETYPE DataBaseType = DATABASETYPE.NONE;

        protected string strConnect = "";

        public static DATABASETYPE RegconizeStringConnect(string strConnect)
        {
            string[] array = new string[8] { "Data Source", "User ID", "Password", "Unicode", "(Description", "LOAD_BALANCE", "ADDRESS_LIST", "SERVICE_NAME" };
            string[] array2 = new string[8] { "Server", "DataBase", "UID", "Pwd", "Data Source", "User ID", "Password", "Initial Catalog" };
            string[] array3 = new string[4] { "Server", "User ID", "Password", "DataBase" };
            string[] array4 = new string[5] { "Provider", "Microsoft", "Jet", "OLEDB", "Data Source" };
            string[] array5 = new string[6] { "Server", "Port", "User ID", "Password", "Database", "Host" };
            string[] array6 = new string[3] { "Data Source", "Version", "Password" };
            int num = 0;
            num += strConnect.ToUpper().Split(new string[1] { "ORA" }, StringSplitOptions.None).Length;
            for (int i = 0; i < array.Length; i++)
            {
                if (strConnect.ToUpper().Contains(array[i].ToUpper()))
                {
                    num++;
                }
            }

            int num2 = 0;
            num2 += strConnect.ToUpper().Split(new string[1] { "SQL" }, StringSplitOptions.None).Length;
            for (int j = 0; j < array2.Length; j++)
            {
                if (strConnect.ToUpper().Contains(array2[j].ToUpper()))
                {
                    num2++;
                }
            }

            int num3 = 0;
            for (int k = 0; k < array3.Length; k++)
            {
                if (strConnect.ToUpper().Contains(array3[k].ToUpper()))
                {
                    num3++;
                }
            }

            int num4 = 0;
            for (int l = 0; l < array5.Length; l++)
            {
                if (strConnect.ToUpper().Contains(array5[l].ToUpper()))
                {
                    num4++;
                }
            }

            int num5 = 0;
            for (int m = 0; m < array4.Length; m++)
            {
                if (strConnect.ToUpper().Contains(array4[m].ToUpper()))
                {
                    num5++;
                }
            }

            int num6 = 0;
            for (int n = 0; n < array6.Length; n++)
            {
                if (strConnect.ToUpper().Contains(array6[n].ToUpper()))
                {
                    num6++;
                }
            }

            if (num6 == 3)
            {
                return DATABASETYPE.SQLite;
            }

            if (num4 >= 5)
            {
                return DATABASETYPE.PosgreSQL;
            }

            if (num3 >= 4)
            {
                return DATABASETYPE.MySQL;
            }

            if (num5 >= 5)
            {
                return DATABASETYPE.MsAccess;
            }

            return (num < num2) ? DATABASETYPE.SQLSERVER : DATABASETYPE.ORACLE;
        }

        public static IData CreateData(string strConnect, bool bolIsCrypt)
        {
            DataBaseType = RegconizeStringConnect(strConnect);
            switch (DataBaseType)
            {
                //case DATABASETYPE.SQLSERVER:
                //    return new SQLData(bolIsCrypt ? strConnect : Encrypt(strConnect, strPasswordConnect));
                //case DATABASETYPE.ORACLE:
                //    return new OracleData(bolIsCrypt ? strConnect : Encrypt(strConnect, strPasswordConnect));
                //case DATABASETYPE.MySQL:
                //    return new MySQLData(bolIsCrypt ? strConnect : Encrypt(strConnect, strPasswordConnect));
                //case DATABASETYPE.MsAccess:
                //    return new AccessData(bolIsCrypt ? strConnect : Encrypt(strConnect, strPasswordConnect));
                //case DATABASETYPE.PosgreSQL:
                //    return new PostgresDbHelper(bolIsCrypt ? strConnect : Encrypt(strConnect, strPassword));
                default:
                    return null;
            }
        }

        //public static IData CreateDataByConfig(string strConfigKey)
        //{
        //    string appSettings = GetAppSettings(strConfigKey);
        //    return CreateData(appSettings);
        //}

        public static string GetAppSettings(string str)
        {
            string result = string.Empty;
            //if (ConfigurationManager.AppSettings[str] != null)
            //{
            //    result = ConfigurationManager.AppSettings[str];
            //}

            return result;
        }

        protected static string Decrypt(string strText, string strPassword)
        {
            if (strText.Trim().Length == 0)
            {
                return "";
            }

            byte[] array = Convert.FromBase64String(strText);
            byte[] rgbSalt = new byte[13]
            {
            80, 118, 97, 110, 33, 77, 101, 100, 118, 21,
            100, 101, 118
            };
            PasswordDeriveBytes passwordDeriveBytes = new PasswordDeriveBytes(strPassword, rgbSalt);
            MemoryStream memoryStream = new MemoryStream();
            Rijndael rijndael = Rijndael.Create();
            rijndael.Key = passwordDeriveBytes.GetBytes(32);
            rijndael.IV = passwordDeriveBytes.GetBytes(16);
            CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndael.CreateDecryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(array, 0, array.Length);
            cryptoStream.Close();
            return Encoding.Unicode.GetString(memoryStream.ToArray());
        }

        protected static string Encrypt(string strText, string strPassword)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(strText);
            byte[] rgbSalt = new byte[13]
            {
            80, 118, 97, 110, 33, 77, 101, 100, 118, 21,
            100, 101, 118
            };
            PasswordDeriveBytes passwordDeriveBytes = new PasswordDeriveBytes(strPassword, rgbSalt);
            MemoryStream memoryStream = new MemoryStream();
            Rijndael rijndael = Rijndael.Create();
            rijndael.Key = passwordDeriveBytes.GetBytes(32);
            rijndael.IV = passwordDeriveBytes.GetBytes(16);
            CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndael.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.Close();
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }
}
