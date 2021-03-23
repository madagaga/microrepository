using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Repository
{
    public abstract class Repositories: IDisposable
    {
        public IDbConnection Connection { get; private set; }
        internal Dictionary<Type, object> _repositoryCache = new Dictionary<Type,object>();


        //public Repositories() : this(DbFactory.GetConnection()) { }

        public Repositories(string connectionStringName) : this(DbFactory.GetConnection(connectionStringName), Enums.DatabaseType.Auto) { }

        Repositories(IDbConnection connection, Enums.DatabaseType dbType)
        {
            if (dbType != Enums.DatabaseType.Auto)
                DbFactory.DataBaseType = dbType;

            Connection = connection;
            RepositoryDiscoveryService.Initialize(this);
#if DEBUG
            System.Diagnostics.Debug.WriteLine(DbFactory.DiagnosticString());
#endif
        }
               

        public Repository<T> GetRepository<T>() where T:class
        {
            return (Repository<T>) _repositoryCache[typeof(T)];
        }
                

        public void Dispose()
        {
            Connection.Dispose();
        }
    }
    
    
}
