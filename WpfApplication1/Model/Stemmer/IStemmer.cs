using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search_Engine.Stemmer
{
    interface IStemmer
    {
        string stemTerm(string s);
    }
}
