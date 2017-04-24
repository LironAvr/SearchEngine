using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search_Engine.Model.Searcher
{
    interface ISearcher
    {
        void importCommonPairs();
        void importDocData();
        void updateDocData(List<string> query);
        void search(List<string> query, List<string> language = null, string queryId = null);
        IEnumerable<string> getCommonPairs(string word);
        void clear();
    }
}
