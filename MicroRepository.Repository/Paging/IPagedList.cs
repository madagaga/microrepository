using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Repository.Paging
{

    public interface IPagedList : IEnumerable
    {
        int Page { get; }
        int TotalResults { get; }
        int ResultPerPage { get; }
        int TotalPages { get; }
    }

    public interface IPagedList<out T> : IEnumerable<T>, IPagedList
    {
       
    }
}
