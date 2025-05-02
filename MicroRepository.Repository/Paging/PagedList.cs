using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Repository.Paging
{
    public record PagedList<T> : IPagedList<T>
    {
        private readonly IEnumerable<T> _items;

        public PagedList(IEnumerable<T> items, int page, int totalResults, int resultPerPage)
        {
            _items = items;
            Page = page;
            TotalResults = totalResults;
            ResultPerPage = resultPerPage;
        }

        public int Page { get; }
        public int TotalResults { get; }
        public int ResultPerPage { get; }
        public int TotalPages => (int)Math.Ceiling(TotalResults / (decimal)ResultPerPage);

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
