using System;
using System.Collections.Generic;
using Search_Engine.Model.Indexer;
using System.IO;
using Search_Engine.Utilities;
using Search_Engine.Indexer;
using Newtonsoft.Json;

namespace Search_Engine.Model.Ranker
{
    class MyRanker
    {
        Dictionary<string, RankerDoc> rankResults;
        Dictionary<string, int> relevantDoc;

        public MyRanker()
        {
            rankResults = new Dictionary<string, RankerDoc>();
            relevantDoc = new Dictionary<string, int>();
        }

        public Dictionary<string, RankerDoc> results
        {
            get { return rankResults; }
        }

        internal void addTermDocs(KeyValuePair<string, Tuple<InvertedIndxTermData, int>> current, Dictionary<string, Dictionary<string, List<int>>> locationList)
        {
            string term = current.Key;
            int tf = current.Value.Item1.gs_tf_Corpus;
            int df = current.Value.Item1.gs_docFreq;
            TermType type = (TermType)current.Value.Item1.Type;

            if (!relevantDoc.ContainsKey(term))
                relevantDoc.Add(term, 0);
            {
                foreach(Dictionary<string, List<int>> doc in locationList.Values)
                {
                    foreach(string FileDoc in doc.Keys)
                    {
                        relevantDoc[term] += 1;
                        if (!rankResults.ContainsKey(FileDoc))
                        {
                            string[] info = FileDoc.Split('F'); //info[0] = File ID | info[1] = Doc ID
                            RankerDoc newDoc = new RankerDoc("F" + info[1], "F" + info[2]);
                            AddDoc(newDoc, FileDoc);
                        }
                            rankResults[FileDoc].addTerm(current, doc[FileDoc]);
                    }
                }
            }
        }

        private void AddDoc(RankerDoc doc, string id)
        {
            rankResults.Add(id, doc);
        }

        private double score_BM25(string term, string[] query, RankerDoc doc, int dl, KeyValuePair<string, Tuple<InvertedIndxTermData, int>> current, Dictionary<string, Dictionary<string, Dictionary<string, List<int>>>> locationList)
        {
            /*param*/
            //k1,k2,b, are param Typical TREC value for k1 is 1.2, k2  varies from 0 to 1000, b = 0.75//
            //dl - doc len - doc data drom file

            /*data from ConValues*/
            //avdl - avg dl ConValues.avgDocLen
            //N is the total # of docs in the collection 

            /*data relevant to query*/
            //qfi is the frequency of term i in the query
            //R - is the number of relevant documents for this query

            //ni - # of docs containing term i
            //fi is the frequency of term i in the doc under consideration
            // ri - # of relevant doc contatining term i , relevantDocs[term]

            int N = ConValues.totalNumberOfDoc;
            int qfi = 0;
            int R = 0;
            int ni = current.Value.Item1.gs_docFreq;
            int fi = locationList[term][doc.Language].Count;
            int ri = locationList[term].Count;

            foreach (string s in relevantDoc.Keys)
                R += relevantDoc[s];

            foreach (string t in query)
                if (term == t)
                    qfi++;

            var K = compute_K(dl);
            var first = Math.Log(((ri + 0.5) / (R - ri + 0.5)) / ((ni - ri + 0.5) / (N - ni - R + ri + 0.5)));
            var second = ((ConValues.k1 + 1) * fi) / (K + fi);
            var third = ((ConValues.k2 + 1) * qfi) / (ConValues.k2 + qfi);
            return first * second * third;
        }

        private double compute_K(int dl)
        {
            return ConValues.k1 * ((1 - ConValues.b) + ConValues.b * ((double)dl / ConValues.avdl));
        }

        public void cleanResults()
        {
            rankResults = new Dictionary<string, RankerDoc>();
            relevantDoc = new Dictionary<string, int>();
        }
    }
}
