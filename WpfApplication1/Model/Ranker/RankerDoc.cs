using Search_Engine.Model.Indexer;
using Search_Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search_Engine.Model.Ranker
{
    class RankerDoc
    {
        private string _fileId;
        private string _docId;
        private string _docTitle;
        private string _docDate;
        private string _lang;
        private string _maxTermFreq;
        private int _maxFreq;
        private double _finalGrade;
        private int _docLength;
        private Dictionary<string, TermData> _terms;
        private Dictionary<string, double> _grades;
        private List<string> _query;

        public RankerDoc(string fileId, string docId)
        {
            this._fileId = fileId;
            this._docId = docId;
            this._terms = new Dictionary<string, TermData>();
            this._grades = new Dictionary<string, double>();
        }

        public void addTerm(KeyValuePair<string, Tuple<InvertedIndxTermData, int>> record, List<int> locations)
        {
            TermData t = new TermData(record.Key, record.Value.Item1.Type);
            t.gs_docTermLocations.Add("0", new Dictionary<string, List<int>>());
            t.gs_docFreq = record.Value.Item1.gs_docFreq;
            t.gs_tf_Corpus = record.Value.Item1.gs_tf_Corpus;
            t.gs_docTermLocations["0"][this._fileId + this._docId] = locations;

            if (!_terms.ContainsKey(record.Key))
            {
                _terms.Add(record.Key, t);
            }
        }

        #region SetGet

        public string DocId
        {
            get { return _docId; }
            set { _docId = value; }
        }

        public string Language
        {
            get { return _lang; }
            set { _lang = value; }
        }

        /// <summary>
        /// getter, setter to docTitle field.
        /// </summary>
        public int maxFreq
        {
            get { return _maxFreq; }
            set { _maxFreq = value; }
        }

        public string DocTitle
        {
            get { return _docTitle; }
            set { _docTitle = value; }
        }

        /// <summary>
        /// getter, setter to docDate field.
        /// </summary>
        public string DocDate
        {
            get { return _docDate; }
            set { _docDate = value; }
        }

        /// <summary>
        /// getter, setter to fileName field.
        /// </summary>
        public string FileName
        {
            get { return _fileId; }
            set { _fileId = value; }
        }

        /// <summary>
        /// getter, setter to maxTermTF field.
        /// </summary>
        public string MostFreqTerm
        {
            get { return _maxTermFreq; }
            set { _maxTermFreq = value; }
        }

        public double FinalGrad
        {
            get { return _finalGrade; }
            set { FinalGrad = value; }
        }
        public Dictionary<string, double> Grades
        {
            get { return _grades; }
            set { _grades = value; }

        }

        public int rankerDocLengh
        {
            get { return _docLength; }
            set { _docLength = value; }
        }

#endregion

        private void tf_idf()
        {
            //N document at corpus//
            int N = ConValues.totalNumberOfDoc;
            double score = 0;
            foreach (TermData term in _terms.Values)
            {
                double tf = term.gs_docTermLocations["0"][this._fileId + this._docId].Count;
                double idf = Math.Log((double)N / term.gs_docFreq, 2);
                score += tf * idf;
            }
            _grades["tf_idf"] = 5 * score; 
        }

        public void rankDoc(List<string> query)
        {
            _query = query;
            double rank = 0;
            //calc...
            /*if (!Grades.ContainsKey("tf_idf"))
                Grades.Add("tf_idf", 0);
            tf_idf();*/

            if (!Grades.ContainsKey("cossim"))
                Grades.Add("cossim", 0);
            cossim();

            if (!Grades.ContainsKey("distance"))
                Grades.Add("distance", 0);
            distanceRank();
            
            if (!Grades.ContainsKey("isTitle"))
                Grades.Add("isTitle", 0);
            isTitle();

            if (!Grades.ContainsKey("isMaxFreq"))
                Grades.Add("isMaxFreq", 0);
            isMaxFreq();

            if (!Grades.ContainsKey("queryFullyExist"))
                Grades.Add("queryFullyExist", 0);
            queryFullyExist();

            if (!Grades.ContainsKey("tfCourpusVDoc"))
                Grades.Add("tfCourpusVDoc", 0);
            tfCourpusVDoc();

            if (!Grades.ContainsKey("tfdocAvgDoc"))
                Grades.Add("tfdocAvgDoc", 0);
            tfdocAvgDoc();

            if (!Grades.ContainsKey("TopButtomLocation"))
                Grades.Add("TopButtomLocation", 0);
            TopButtomLocation();
            
            if (!Grades.ContainsKey("BM25"))
                Grades.Add("BM25", 0);
            score_BM25();
            Grades["BM25"] = Grades["BM25"]*5 ;

            foreach (string method in Grades.Keys)
                rank += Grades[method];

            _finalGrade = rank;
            return;
        }

        private void cossim()
        {
            double wi, wq, WiWq = 0, WiSqr = 0, WqSqr = 0;
            foreach (string term in _terms.Keys)
            {
                wq = 0;
                wi = _terms[term].gs_docTermLocations["0"][this._fileId + this._docId].Count / (double)this._docLength;
                foreach (string word in _query)
                {
                    if (term == word)
                        wq++;
                }
                WiWq += wq * wi;
                WiSqr += Math.Pow(wi, 2);
                WqSqr += Math.Pow(wq, 2);
            }
            _grades["cossim"] = 5 * WiWq / (Math.Sqrt(WiSqr * WqSqr));
            if (_grades["cossim"] == Double.NaN) _grades["cossim"] = 0;
        }

        private void isTitle()
        {
            int wordCounter = 0;
            foreach (string word in _query)
            {
                if (this._docTitle.Contains(word))
                    wordCounter++;
            }
            Grades["isTitle"] += 5 * (wordCounter);
        }

        private void distanceRank()
        {
            double score = 0;
            foreach (string term1 in _terms.Keys)
            {
                foreach (string term2 in _terms.Keys)
                    if (term1 != term2)
                    {
                        score += distanceBetween2Terms(_terms[term1].gs_docTermLocations["0"][this._fileId + this._docId], _terms[term2].gs_docTermLocations["0"][this._fileId + this._docId]);
                    }
            }
            _grades["distance"] = score * 15;
        }

        private double distanceBetween2Terms(List<int> pos1, List<int> pos2)
        {
            List<int> sub = new List<int>();

            foreach (int locIn1 in pos1)
            {
                foreach (int locIn2 in pos2)
                {
                    int dis = Math.Abs(locIn1 - locIn2);
                    if (dis <= 10)
                    {
                        return 1;
                    }
                    sub.Add(dis);
                }
            }
            sub.Sort();
            return (1.0 / (double)sub.First());
        }

        private double isMaxFreq()
        {
            double ans = 0;
            foreach (string term in _terms.Keys)
            {
                if (term == _maxTermFreq)
                {
                    if (_query.Contains(term))
                        Grades["isMaxFreq"] += 13;//original from query
                    else Grades["isMaxFreq"] += 7; ;//a word from the api.
                }
            }
            return ans;
        }

        private void queryFullyExist()
        {
            int wordCounter = _query.Count;
            foreach (string word in _query)
            {
                if (this._terms.ContainsKey(word))
                    wordCounter--;
            }

            Grades["queryFullyExist"] += (12 * ((_query.Count - wordCounter) / (double)_query.Count));
        }

        private void tfCourpusVDoc()
        {
            double ans = 0;
            foreach (string t in _terms.Keys)
                if (_terms.ContainsKey(t))
                    foreach (Dictionary<string, List<int>> Langlist in _terms[t].gs_docTermLocations.Values)
                        foreach (string len in Langlist.Keys)
                        {
                            var tfDoc = Langlist[len].Count;
                            ans += (tfDoc / _terms[t].gs_tf_Corpus);
                        }
            Grades["tfCourpusVDoc"] += (5 * ans);
        }

        private void tfdocAvgDoc()
        {
            double ans = 0;
            foreach (string t in _terms.Keys)
                foreach (Dictionary<string, List<int>> Langlist in _terms[t].gs_docTermLocations.Values)
                    foreach (string len in Langlist.Keys)
                    {
                        var tfDoc = Langlist[len].Count;
                        ans += (tfDoc / ConValues.avdl);
                    }

            Grades["tfdocAvgDoc"] += (10 * ans);
        }

        private void TopButtomLocation()
        {
            double ans = 0;
            foreach (string t in _terms.Keys)
            {
                double factor = 1;
                foreach (Dictionary<string, List<int>> Langlist in _terms                           [t].gs_docTermLocations.Values)
                    foreach (List<int> locations in Langlist.Values)
                    {
                        foreach (int location in locations)
                        {
                            if ((location > (3 * this.rankerDocLengh / 4)) || (location <           (this.rankerDocLengh / 4)))
                                ans++;
                        }
                        if (!_query.Contains(t)) factor = 0.4;
                        ans = factor * ans / (double)locations.Count;
                    }
            }

            Grades["TopButtomLocation"] += (10 * ans);
        }

        private void score_BM25()
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

            //for this calc use R,ri = 0 , as instructed//
            int R = 0;
            int ri = 0;

            foreach (string t in _terms.Keys)
            {
                double factor = 1;
                int ni = 0;
                int fi = 0;
                int qfi = 0;
                foreach (string term in _terms.Keys)
                {
                    if (term == t)
                        qfi++;
                }

                if (_terms.ContainsKey(t))
                {
                    ni = _terms[t].gs_docFreq;
                    fi = _terms[t].gs_docTermLocations["0"][this._fileId + this.DocId].Count;
                }

                var K = compute_K();
                var x = ((ri + 0.5) / (R - ri + 0.5)) / ((ni - ri + 0.5) / (N - ni - R + ri + 0.5));
                var first = Math.Log(x, 2);
                var second = ((ConValues.k1 + 1) * fi) / (K + fi);
                var third = ((ConValues.k2 + 1) * qfi) / (ConValues.k2 + qfi);
                if (!_query.Contains(t)) factor = 0.1;
                _grades["BM25"] += first * second * third * factor;
            }
            if (_grades["BM25"] > 35) _grades["BM25"] = 35;
        }

        private double compute_K()
        {
            return ConValues.k1 * ((1 - ConValues.b) + ConValues.b * ((double)this.rankerDocLengh / ConValues.avdl));
        }
    }
}
