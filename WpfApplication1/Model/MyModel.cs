using Newtonsoft.Json;
using Search_Engine.Utilities;
using Search_Engine.Parser;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Search_Engine.Indexer;
using Search_Engine.ReadFile;
using System.ComponentModel;
using System;
using Search_Engine.Model.Indexer;
using Search_Engine.Model.Searcher;
using Search_Engine.Stemmer;

namespace Search_Engine
{
    class MyModel
    {
        private IFileReader reader;
        private IParser parser;
        private IIndexer indexer;
        private ISearcher _searcher;
        private string status;
        public Stopwatch watch;

        public delegate void ParsingDoneEventHandler();
        public delegate void PostingDoneEventHandler(int numOfDocs, int numOfTerms);
        public delegate void EngineEventHandler(string msg);

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        public IFileReader Reader
        {
            get { return reader; }
            set { reader = value; }
        }

        public IParser Parser
        {
            get { return parser; }
            set { parser = value; }
        }

        public IIndexer Indexer
        {
            get { return indexer; }
            set { indexer = value; }
        }
        
        public string Status
        {
            get { return status; }
            set { status = value; }
        }

        public Stopwatch Timer
        {
            get { return watch; }
            set { watch = value; }
        }

        public ISearcher Searcher { get { return _searcher; } set { _searcher = value; } }

        /// <summary>
        /// Constractor, to init a new instance of Model_Engine.
        /// </summary>
        public MyModel()
        {
            indexer = new MyIndexer();
            parser = new MyParser(indexer, new PorterStemmer(), ConValues.USE_STEMMER);
            reader = new MyReader(parser);
            _searcher = new MySearcher(indexer.getInv());
            watch = new Stopwatch();
            status = "Ready";
            NotifyPropertyChanged("Status");
        }

        public void start()
        {
            parser.initiateStopWords(ConValues.Corpus + @"\stop_words.txt");
            reader.initiate(ConValues.Corpus);
            indexer.initiate(ConValues.Posting);
            watch.Start();
            status = "Parsing...";
            NotifyPropertyChanged("Status");
            reader.ReadFile();
            status = "Merging...";
            NotifyPropertyChanged("Status");
            indexer.finish();
            status = "Engine is Ready";
            NotifyPropertyChanged("Status");
            watch.Stop();
            NotifyPropertyChanged("totalClock");
        }

        /// <summary>
        /// initialization function.
        /// </summary>
        public void initialization_Model_Engine()
        {
            Indexer.initPostingFolder();
        }

        /// <summary>
        /// this function will take as an argument a letter or a sign and will return a 
        /// sorted dictionray with all the relevant data for this specific argument.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public SortedDictionary<string,TermData> retrieveLetterData(string c)
        {
            var sortDic = new SortedDictionary<string, TermData>();
            BinaryReader br = new BinaryReader(File.Open((ConValues.Posting + c + ".bin"), FileMode.Open));

            while(br.BaseStream.Position< br.BaseStream.Length)
            {
                string fileData = br.ReadString();
                KeyValuePair<string, TermData> KVP = JsonConvert.DeserializeObject<KeyValuePair<string, TermData>>(fileData);
                if (!sortDic.ContainsKey(KVP.Key))
                    sortDic.Add(KVP.Key, KVP.Value);
            }
            br.Close();
            return sortDic;
        }

        public void clear()
        {
            _searcher.clear();
        }

        public bool isInvertedIndex()
        {
            if (indexer.amountOfTerms() != 0 | indexer.isInvertedIndex())
                return true;
            return false;
        }

        internal HashSet<string> getLanguages()
        {
            return parser.getLanguages();
        }

        public Dictionary<string, Tuple<InvertedIndxTermData, int>> getInvIndex()
        {
            return indexer.getInv();
        }

        public void search(string query, List<string> language, string queryId = null)
        {
            _searcher.search(parser.parseQuery(query), language, queryId);
        }

        public IEnumerable<string> getCommonPairs(string word)
        {
            return _searcher.getCommonPairs(word);
        }

        internal void readQueriesFromFile()
        {
            readQueries(ConValues.Queries);
        }

        public void readQueries(string path)
        {
            if (File.Exists(ConValues.Results + "\\Results.txt"))
                File.Delete(ConValues.Results + "\\Results.txt");

            using (TextReader reader = new StreamReader(File.Open(path, FileMode.Open)))
            {
                string query = reader.ReadLine();
                while (query != null)
                {
                    string[] splittedQuery = query.Split(' ');
                    string fixedQuery = "";
                    for (int i = 1; i < splittedQuery.Length - 1; i++)
                        fixedQuery += splittedQuery[i] + " ";
                    fixedQuery += splittedQuery[splittedQuery.Length - 1];
                    search(fixedQuery, null, splittedQuery[0].Remove(splittedQuery[0].Length - 1));
                    query = reader.ReadLine();
                }
            }
        }
    }
}
