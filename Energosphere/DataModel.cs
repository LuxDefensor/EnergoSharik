using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using Settings;

namespace Energosphere
{
    public class DataModel
    {
        private string cs;

        public DataModel(string settingsFile)
        {
            SettingsManager settings = new SettingsManager(settingsFile);
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder();
            csb.DataSource = settings["server"];
            csb.InitialCatalog = settings["database"];
            csb.UserID = settings["user"];
            csb.Password = settings["password"];
            cs = csb.ConnectionString;
        }

        public DataModel() : this("settings.ini")
        {

        }

        public void ExecuteQuery(string sql, int timeout = 30)
        {
            using (SqlConnection cn = new SqlConnection(cs))
            {
                cn.Open();
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandTimeout = timeout;
                cmd.CommandText = sql;
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception(sql, ex);
                }
            }
        }

        public int GetIntValue(string sql, int timeout)
        {
            object returnValue;
            int result;
            using (SqlConnection cn = new SqlConnection(cs))
                returnValue = GetSingleObjectValue(sql, timeout);
            if (!int.TryParse(returnValue.ToString(), out result))
                throw new Exception("Returned value cannot be parsed as int: " + returnValue.ToString() + Environment.NewLine + sql);
            else
                return result;
        }

        public double GetDoubleValue(string sql, int timeout)
        {
            object returnValue;
            double result;
            using (SqlConnection cn = new SqlConnection(cs))
                returnValue = GetSingleObjectValue(sql, timeout);
            if (!double.TryParse(returnValue.ToString(), out result))
                throw new Exception("Returned value cannot be parsed as double: " + returnValue.ToString() + Environment.NewLine + sql);
            else
                return result;
        }

        public DateTime GetDateValue(string sql, int timeout)
        {
            object returnValue;
            DateTime result;
            using (SqlConnection cn = new SqlConnection(cs))
                returnValue = GetSingleObjectValue(sql, timeout);
            if (!DateTime.TryParse(returnValue.ToString(), out result))
                throw new Exception("Returned value cannot be parsed as date: " + returnValue.ToString() + Environment.NewLine + sql);
            else
                return result;
        }

        public string GetStringValue(string sql, int timeout)
        {
            return GetSingleObjectValue(sql, timeout).ToString();
        }


        private object GetSingleObjectValue(string sql, int timeout)
        {
            object returnValue;
            using (SqlConnection cn = new SqlConnection(cs))
            {
                cn.Open();
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandTimeout = timeout;
                cmd.CommandText = sql;
                try
                {
                    returnValue = cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    throw new Exception(sql, ex);
                }
                if (returnValue == null || Convert.IsDBNull(returnValue))
                    throw new Exception("Query returned NULL" + Environment.NewLine + sql);
                else
                    return returnValue;
            }
        }

        public DataTable GetData(string sql, int timeout)
        {
            DataTable result = new DataTable();
            using (SqlConnection cn = new SqlConnection(cs))
            {
                cn.Open();
                SqlCommand cmd = cn.CreateCommand();
                cmd.CommandTimeout = timeout;
                cmd.CommandText = sql;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                try
                {
                    da.Fill(result);
                }
                catch (Exception ex)
                {
                    throw new Exception(sql, ex);
                }
            }
            return result;
        }
    }
}
