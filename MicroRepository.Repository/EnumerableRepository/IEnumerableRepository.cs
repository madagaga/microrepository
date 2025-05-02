using MicroRepository.Repository.Paging;
using MicroRepository.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Repository.EnumerableEntity
{
    public interface IEnumerableRepository<TEntity> 
    {
        IDbConnection Connection { get; }
        SqlBuilder InternalBuilder { get; }

        bool Enumerated { get; }

        IPagedList<TEntity> ToPagedList(int page, int resultPerPage);
        List<TEntity> ToList();

    }
}
