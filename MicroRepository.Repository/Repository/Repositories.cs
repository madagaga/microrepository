using MicroRepository.Repository.Interfaces;
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
        
        public Repositories(IDbConnection connection)
        {
            switch(connection.GetType().Name.ToLower())
            {
                case "sqliteconnection":
                    RepositoryDiscoveryService.DataBaseType = Enums.DatabaseType.SQLite;
                    break;
                default:
                    RepositoryDiscoveryService.DataBaseType = Enums.DatabaseType.Auto;
                    break;
            }
                

            Connection = connection;
            RepositoryDiscoveryService.Initialize(this);
#if DEBUG
            System.Diagnostics.Debug.WriteLine(RepositoryDiscoveryService.DiagnosticString());
#endif
        }
               

        public virtual IRepository<T> GetRepository<T>() where T:class
        {
            Type repositoryType = typeof(T);
            if (!_repositoryCache.ContainsKey(repositoryType))
                _repositoryCache[repositoryType] = new Repository<T>(this.Connection);

            return (Repository<T>) _repositoryCache[typeof(T)];
        }
                

        public void Dispose()
        {
            Connection.Dispose();
        }
    }
    
    
}
