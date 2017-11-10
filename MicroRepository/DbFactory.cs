

using MicroRepository.Enums;
using MicroRepository.Interfaces;
using MicroRepository.Repository;
using MicroRepository.Templates;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MicroRepository
{
    public static class DbFactory
    {

        #region configuration purpose
        public static bool Buffered { get; set; } = true;
        public static bool UpdateChangeOnly { get; set; } = true;
        static SqlTemplate _template;
        internal static SqlTemplate Template
        {
            get
            {
                if (_dataBaseType == DatabaseType.Auto)
                    throw new ArgumentException("Databasetype can not be auto when accessing to template.");
                return _template;
            }
        }

        static DatabaseType _dataBaseType = DatabaseType.Auto;
        internal static DatabaseType DataBaseType
        {
            get { return _dataBaseType; }
            set
            {
                if (value == DatabaseType.Auto)
                    throw new ArgumentException("Database type can not be set to Auto");

                _dataBaseType = value;
                if (_template == null)
                    _template = SqlTemplate.Load(_dataBaseType.ToString().ToLower());
            }
        }
        #endregion        

        internal static object DiagnosticString()
        {
            return string.Format("** RepositoryConfiguration **\r\nUpdateChangeOnly : {0}\r\nDatabase Type : {1}", UpdateChangeOnly, DataBaseType);
        }

        public static IDbConnection GetConnection(string connectionStringName)
        {
            ConnectionStringSettings connectionStringSetting = ConfigurationManager.ConnectionStrings[connectionStringName];

            return GetConnection(connectionStringSetting);
        }

        public static IDbConnection GetConnection(ConnectionStringSettings connectionStringSettings)
        {
            return GetConnection(connectionStringSettings.ProviderName, connectionStringSettings.ConnectionString);
        }
        

        /// <summary>
        /// return connection from provider and connection string.
        /// </summary>
        /// <param name="providerName"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static IDbConnection GetConnection(string providerName, string connectionString)
        {
            DbConnection connection = null;
            if (connectionString != null)
            {
                try
                {
                    DbProviderFactory factory = DbProviderCache.GetFactory(providerName);
                    Console.WriteLine(connectionString);
                    connection = factory.CreateConnection();
                    connection.ConnectionString = connectionString;


                    // detect database type
                    // resolve provider name and set template if auto
                    if (string.IsNullOrEmpty(providerName))
                        providerName = "System.Data.SqlClient";

                    providerName = providerName.ToLower();
                    // detect 
                    if (providerName.IndexOf("mysql") >= 0)
                        DataBaseType = Enums.DatabaseType.MySql;
                    else if (providerName.IndexOf("sqlite") >= 0)
                       DataBaseType = Enums.DatabaseType.SQLite;
                    else if (providerName.IndexOf("sqlserverce") >= 0 ||
                        providerName.IndexOf("sqlceconnection") >= 0 ||
                        providerName.IndexOf("sqlserver") >= 0 ||
                        providerName.IndexOf("system.data.sqlclient") >= 0 ||
                        providerName.IndexOf("oledb") >= 0)
                        DataBaseType = Enums.DatabaseType.MSSql;
                    
                    if (DataBaseType == Enums.DatabaseType.Auto)
                        throw new ArgumentException("Provider `" + providerName + "` has no template.", "providerName");

                }
                catch (Exception ex)
                {
                    // Set the connection to null if it was created.
                    if (connection != null)
                        connection = null;
                    throw ex;
                }
            }
            return connection;

        }






    }
}
