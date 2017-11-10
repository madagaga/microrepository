using MicroRepository.Sql;
using System;
using System.Collections;

namespace MicroRepository.Interfaces
{
    public interface IQueryableResult<TEntity> : IEnumerable
    {
        System.Data.IDbConnection Connection { get; }
        bool Enumerated { get; }
        SqlBuilder InternalBuilder { get; }
    }
}
