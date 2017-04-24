using Newtonsoft.Json;
using Search_Engine.Indexer;
using Search_Engine.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Search_Engine.Model.Indexer;
using Search_Engine.Model.Ranker;

namespace Search_Engine.Model.Searcher
{
    class MySearcher : ISearcher
    {
        MyRanker ranker = new MyRanker();
        Dictionary<string, Tuple<InvertedIndxTermData, int>> invIndex;
        Dictionary<string, Tuple<InvertedIndxTermData, int>> relevantRecords;
        Dictionary<string, Dictionary<string, Dictionary<string, List<int>>>> locationList;
        Dictionary<string,DocumentData> _docData;
        Dictionary<string, List<string>> _commonPairs;
        Dictionary<string, double> final_Grades;

        int _queryCounter;

        public MySearcher(Dictionary<string, Tuple<InvertedIndxTermData, int>> index)
        {
            invIndex = index;
            _queryCounter = 1000;
            _commonPairs = new Dictionary<string, List<string>>();
            _docData = new Dictionary<string, DocumentData>();
        }

        public void search(List<string> query, List<string> language = null, string queryId = null)
        {
            relevantRecords = new Dictionary<string, Tuple<InvertedIndxTermData, int>>();
            locationList = new Dictionary<string, Dictionary<string, Dictionary<string, List<int>>>>();
            final_Grades = new Dictionary<string, double>();

            buildRelevantRecords(query);
            buildRecordsData(language);
            updateDocData(query);
            printResults(queryId);
        }

        private void buildRecordsData(List<string> language)
        {
            string term;
            TermType type;
            int pointer;
            string posting;
            foreach (KeyValuePair<string, Tuple<InvertedIndxTermData, int>> current in relevantRecords)
            {
                //Current metadata
                term = current.Key;
                type = (TermType)current.Value.Item1.Type;
                pointer = current.Value.Item2;
                
                if (type == TermType.Word)
                {
                    posting = term.Substring(0, 2);
                }
                else posting = type.ToString();

                string path;
                if (!ConValues.USE_STEMMER)
                {
                    path = ConValues.Posting + "\\Posting\\";
                }
                else path = ConValues.Posting + "\\Stemming_Posting\\";

                if (File.Exists(path + posting + ".bin"))
                {
                    BinaryReader reader = new BinaryReader(File.Open(path + "\\" + posting + ".bin", FileMode.Open));
                    for (int i = 1; i < pointer; i++)
                    {
                        reader.ReadString();
                    }
                    string data = reader.ReadString();
                    KeyValuePair<string, TermData> KVP = JsonConvert.DeserializeObject<KeyValuePair<string, TermData>>(data);
                    
                    Dictionary<string, Dictionary<string, List<int>>> temp = KVP.Value.gs_docTermLocations;
                    if (language != null)
                    {
                        List<string> somthing = new List<string>(temp.Keys);
                        foreach (string lang in somthing)
                        {
                            if (!language.Contains(lang))
                            {
                                temp.Remove(lang);
                            }
                        }
                    }
                    locationList.Add(term, temp); //word -> lang -> (DocFile, locations)
                    reader.Close();
                }
                ranker.addTermDocs(current, locationList[current.Key]);
            }
        }

        private void buildRelevantRecords(List<string> query)
        {
            foreach (string word in query)
            {
                List<string> similarWords = DevUtils.semantic(word);
                addRecord(word);
                foreach (string s in similarWords)
                {
                    addRecord(s);
                }
            }
        }

        public void addRecord(string word)
        {
            if (invIndex.ContainsKey(word) && !relevantRecords.ContainsKey(word))
            {
                relevantRecords.Add(word, invIndex[word]);
            }
        }

        public void importCommonPairs()
        {
            string path = ConValues.Posting;
            if (ConValues.USE_STEMMER)
                path += "\\Common_Pairs(stem).bin";
            else path += "\\Common_Pairs(unstemmed).bin";
            
            try
            {
                BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));
                {
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        string tmp = reader.ReadString();
                        KeyValuePair <string,List<string>> cp 
                            = JsonConvert.DeserializeObject<KeyValuePair<string, List<string>>>(tmp);
                        _commonPairs.Add(cp.Key, new List<string>(cp.Value));
                    }
                }
                reader.Close();
            }
            catch { Console.WriteLine("Import common pair error"); }
        }

        public void importDocData()
        {
            string path = ConValues.Documents;
            if (ConValues.USE_STEMMER)
                path += "\\docData_stem.bin";
            else path += "\\docData.bin";
            try
            {
                BinaryReader reader = new BinaryReader(File.Open(path , FileMode.Open));
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    string tmp = reader.ReadString();
                    DocumentData d = JsonConvert.DeserializeObject<DocumentData>(tmp);
                    if (!_docData.ContainsKey(d.DocId))
                    {
                        _docData.Add(d.DocId,d);
                        
                    }
                    else Console.WriteLine("doc is already in list");
                }
                reader.Close();
            }
            catch {}
        }

        public void updateDocData(List<string> query)
        {
            foreach (RankerDoc current in ranker.results.Values)
            {
                var relevantDocid = _docData[current.DocId];
                current.MostFreqTerm = relevantDocid.MostFreqTerm;
                current.maxFreq = relevantDocid.MaxFreq;
                current.DocTitle = relevantDocid.DocTitle;
                current.Language = relevantDocid.Language;
                current.rankerDocLengh = relevantDocid.DocLength;
                current.rankDoc(query);
            }

            foreach (RankerDoc current in ranker.results.Values)
            {
                final_Grades.Add(current.DocId, current.FinalGrad);
            }
            final_Grades = final_Grades.OrderByDescending(entry => entry.Value)
                               .Take(ConValues.docAmount)
                               .ToDictionary(pair => pair.Key, pair => pair.Value);


            using (System.IO.StreamWriter file1 =
                    new System.IO.StreamWriter(ConValues.Results + "\\FullRanks.txt", true))
            {
                //file1.WriteLine("Qeury #" + id + " Results:");
                foreach (RankerDoc current in ranker.results.Values)
                {
                    file1.Write(current.DocId + ", Final Score: " + current.FinalGrad + ", ");
                    foreach (string rank in current.Grades.Keys)
                    file1.Write(", " + rank + ": " + current.Grades[rank]);
                    file1.WriteLine("");
                }
                //file1.WriteLine("End of results for query #" + id);
                _queryCounter++;
            }
            ranker.cleanResults();
        }

        public IEnumerable<string> getCommonPairs(string word)
        {
            if (_commonPairs != null && _commonPairs.ContainsKey(word))
                return _commonPairs[word];
            return null; 
        }

        private void printResults(string id)
        {
            if (id == null) id = _queryCounter.ToString();

            using (System.IO.StreamWriter file1 =
                    new System.IO.StreamWriter(ConValues.Results + "\\Results.txt", true))
            {
                //file1.WriteLine("Qeury #" + id + " Results:");
                foreach (KeyValuePair<string, double> rankerdoc in final_Grades)
                    file1.WriteLine(id + ", 0,  " + rankerdoc.Key + " , " + rankerdoc.Value + ", 0, mt ");
                //file1.WriteLine("End of results for query #" + id);
                _queryCounter++;
            }
        }

        public void clear()
        {
            invIndex = new Dictionary<string, Tuple<InvertedIndxTermData, int>>();
            _queryCounter = 1000;
            _commonPairs = new Dictionary<string, List<string>>();
            _docData = new Dictionary<string, DocumentData>();
        }
    }
}
