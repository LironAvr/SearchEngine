using System;
using System.Collections.Generic;
using System.ComponentModel;
using Search_Engine.Model.Indexer;

namespace Search_Engine.Indexer
{
    interface IIndexer : INotifyPropertyChanged
    {
        string InvertedStatus { get; set; }

        void addTerm(string term, string fileNo, string DocNumber, int locationAtDoc, TermType type, string language);
        void merging(string postingLetter);
        void finish();
        void initPostingFolder();
        int amountOfTerms();
        void initiate(string path);
        double watchSeconds();
        double watchMinutes();
        void loadInvIndex();
        bool isInvertedIndex();
        Dictionary<string, Tuple<InvertedIndxTermData, int>> getInv();
        void clearIndex();
    }
}
