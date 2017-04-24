using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Search_Engine.Parser
{
    interface IParser : INotifyPropertyChanged
    {
        void parseDoc(string fileName, ArrayList doc);
        void resetWatch();
        void initiateStopWords(string path);
        void exportLanguages();
        void exportAvgDocLen();
        void exportCommonPairs();
        double watchSeconds();
        double watchMinutes();
        int docCounter();
        HashSet<string> getLanguages();
        List<string> parseQuery(string query);
    }
}
