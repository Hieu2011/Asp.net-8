using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore8.Application.Contracts
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
        public int StartIndex => (Page - 1) * PageSize + 1;
        public int EndIndex => Math.Min(Page * PageSize, Total);
    }
}
