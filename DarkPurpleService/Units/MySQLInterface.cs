using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using MySql.Data.MySqlClient;
using MySql.Data.Types;

namespace DarkPurpleService.Units
{
    public class MySQLInterface
    {
        private static MySQLInterface self = null;
        private MySqlConnection connection = null;
        private bool isDBOpened = false;

        private MySQLInterface()
        {
            connection = new MySqlConnection("server=localhost;user id=root;password=root;database=darkpurple");
        }

        public static MySQLInterface get()
        {
            if(self == null)
            {
                self = new MySQLInterface();
            }
            lock (self)
            {
                return self;
            }
        }

        /// <summary>
        /// 开启数据库连接 , 如果已经开启 , 则忽略此次操作
        /// </summary>
        public void startDataBase()
        {
            if(connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }
            isDBOpened = true;
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        public void closeDataBase()
        {
            if(connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
            isDBOpened = false;
        }

        /// <summary>
        /// 从数据库搜索对应的字段
        /// </summary>
        /// <param name="table">要搜索的表名</param>
        /// <param name="key">搜索的字段</param>
        /// <param name="value">搜索的字段值</param>
        /// <returns>对应数据的列表,列表内为字符串数组,如果无结果或搜索失败则返回NULL</returns>
        public List<string[]> searchDB(string table, string key, string value)
        {
            if (!isDBOpened)
            {
                //数据库尚未连接
                return null;
            }
            string dbCommandString = "select * from " + table + " where " + key + " = \"" + value + "\"";
            System.Diagnostics.Debug.Write("SQL Command: "+dbCommandString);

            DataTable result = doCommand(dbCommandString);

            //如果成功得到数据 , 并且数据的栏数和条目数大于0 , 否则返回NULL
            if(result != null && result.Columns.Count > 0 && result.Rows.Count > 0)
            {
                int singleArrayLength = result.Columns.Count;
                List<String[]> listSet = new List<string[]>();
                for (int rowCounter = 0; rowCounter < result.Rows.Count; rowCounter++)
                {
                    //生成对应长度的数组用于存放数据
                    String[] strings = new String[singleArrayLength];
                    for (int columnCounter = 0; columnCounter < singleArrayLength; columnCounter++)
                    {
                        //将数据存放到数组中
                        strings[columnCounter] = result.Rows[rowCounter].ItemArray[columnCounter].ToString();
                    }
                    listSet.Add(strings);
                }
                return listSet;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 往数据库内插入一条记录
        /// </summary>
        /// <param name="table">要插入的表名</param>
        /// <param name="keys">插入的字段名称集合</param>
        /// <param name="values">插入的值集合</param>
        public bool putValue(string table,string[] keys,string[] values)
        {
            if(keys.Length != values.Length || TextUnits.isTextEmpty(table))
            {
                //表名为空或者键值数量不对应
                return false;
            }

            string keysString = "";
            string valuesString = "";
            for (int i = 0; i < keys.Length; i++)
            {
                if(i != 0)
                {
                    keysString = keysString + ",";
                    valuesString = valuesString + ",";
                }
                keysString = keysString + keys[i];
                valuesString = valuesString + "'"+values[i]+"'";
            }
            string commandString = "INSERT INTO " + table + "(" + keysString + ") VALUES (" + valuesString + ")";
            //执行SQL语句
            doCommand(commandString);
            //检查是否插入成功 , 通过最后一个参数来检测
            List<string[]> result = searchDB(table, keys[keys.Length-1], values[values.Length-1]);
            if(result != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 简单删除一个条目
        /// </summary>
        /// <param name="table">表名</param>
        /// <param name="key">查询的键值名</param>
        /// <param name="value">对应的键值</param>
        public void removeRow(string table,string key,string value)
        {
            string command = "DELETE FROM " + table + " WHERE " + key + " = '" + value + "'";
            doCommand(command);
        }

        /// <summary>
        /// 执行数据库检索语句
        /// </summary>
        /// <param name="commandString">检索语句</param>
        /// <returns>DataTable数据对象 , 无法执行或失败返回NULL</returns>
        private DataTable doCommand(string commandString)
        {
            if (commandString.Length <= 0)
            {
                return null;
            }
            System.Diagnostics.Debug.WriteLine("执行数据库命令:" + commandString);
            MySqlCommand command = new MySqlCommand(commandString, connection);
            MySqlDataAdapter adapter = new MySqlDataAdapter(command);
            DataSet dataSet = new DataSet();
            try
            {
                adapter.Fill(dataSet);
                return dataSet.Tables[0];
            }
            catch (Exception)
            {
                return null;
                throw;
            }
        }

    }
}