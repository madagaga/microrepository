namespace MicroRepository.Enums
{
    public enum DatabaseType : int
    {
        None = 0,
        /// <summary>
        /// Microsoft SQLServer
        /// </summary>
        SQLServer = 1,
        /// <summary>
        /// MySQL
        /// </summary>
        MySql,
        /// <summary>
        /// SQLite
        /// </summary>
        SQLite,
        /// <summary>
        /// Postgres 
        /// </summary>
        Postgres,
    }
}
