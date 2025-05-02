using MicroRepository.Enums;
using MicroRepository.Templates;
using System;
using System.Linq.Expressions;

namespace MicroRepository.Repository
{
    public static class DbSettings
    {

        #region configuration purpose
        public static bool Buffered { get; set; } = true;
        static SqlTemplate? _template ;
        internal static SqlTemplate Template
        {
            get
            {
                if (_dataBaseType == DatabaseType.None)
                    throw new ArgumentException("Databasetype can not be None when accessing to template.");
                if (_template == null)
                    throw new ArgumentException("Template cannot be null", nameof(SqlTemplate));
                return _template;
            }
        }

        public static bool EnquoteTableNames { get; set; } = true;
        public static bool EnquoteColumnNames { get; set; } = true;

        static DatabaseType _dataBaseType = DatabaseType.None;

        public static void SetDbType(DatabaseType dbType)
        {
            _dataBaseType = dbType;

            switch (dbType)
            {
                
                case DatabaseType.SQLServer:
                    _template = new MsSqlTemplate();
                    break;
                case DatabaseType.MySql:
                    _template = new MySqlTemplate();
                    break;
                case DatabaseType.SQLite:
                    _template = new SQLiteTemplate();
                    break;
                case DatabaseType.Postgres:
                    _template = new PostgreSqlTemplate();
                    break;
                default:
                    throw new ArgumentException("Databasetype can not be None when accessing to template.");         
            }

        }
        
        #endregion        

       
    }
}
