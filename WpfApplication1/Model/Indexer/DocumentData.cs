using Newtonsoft.Json;
using Search_Engine.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Search_Engine.Indexer
{
    class DocumentData
    {
        //private string _fileId;
        private string _docId;
        private string _docTitle;
        //private string _docDate;
        private int _docLength;
        private Dictionary<string, int> _termFreq;
        private string _language;
        private string _maxFreqTerm;
        private int _maxFreq;
                
        /// <summary>
        /// Constroctur inorder to init the Document.
        /// 
        /// docTitle = name of the the document title. for example: FBIS3-1.
        /// docDate 
        /// fileName = file name, for example: FB396001.
        /// language = document language. for example: English 
        /// maxTermTF = the most freq term in this document.
        /// maxTermTFCount = the freq of maxTermTF in this document.        /// 
        /// </summary>
        /// <param name="newTerm"></param>
        public DocumentData(string fileId)
        {
            _termFreq = new Dictionary<string, int>();
            _maxFreqTerm = null;
        }

        public void writeFile()
        {
            _maxFreq = _termFreq[_maxFreqTerm];
            writeToFile();
        }

        private void writeToFile()
        {
            string data = JsonConvert.SerializeObject(this) + '\r';
            try
            {
                if (!Directory.Exists(ConValues.Documents))
                    Directory.CreateDirectory(ConValues.Documents);
                BinaryWriter bw = new BinaryWriter(File.Open(ConValues.Documents + @"\docData.bin", FileMode.Append));
                bw.Write(data);
                bw.Flush();
                bw.Close();
            }
            catch { }
        }

        public string DocId
        {
            get { return _docId; }
            set { _docId = value; }
        }

        

        public int DocLength
        {
            get { return _docLength; }
            set { _docLength = value; }
        }

        /// <summary>
        /// getter, setter to docTitle field.
        /// </summary>
        public string DocTitle
        {
            get { return _docTitle; }
            set { _docTitle = value; }
        }

        /// <summary>
        /// getter, setter to language field.
        /// </summary>
        [JsonIgnore]
        public string Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// getter, setter to maxTermTF field.
        /// </summary>
        public string MostFreqTerm
        {
            get { return _maxFreqTerm; }
            set { _maxFreqTerm = value; }
        }

        /// <summary>
        /// getter, setter to freq of the most frequent term field.
        /// </summary>
        public int MaxFreq
        {
            get { return _maxFreq; }
            set { _maxFreq = value; }
        }

        /// <summary>
        /// This function will update the dictionray termFreq.        
        /// A first term in dic = will add the term to dic and update MaxTF value.
        /// A new term in this doc = will add it to the dictionray with 1 appearnce.       
        /// An exist term = will update the number of appearnce of this term and check maxTF.
        /// </summary>        
        public void addTerm(string term)
        {

            if (_termFreq.Count < 1) //if it is the first term in the doc.
            {
                _termFreq.Add(term, 1);//add the first freq of a new word.                
                _maxFreqTerm = term;
            }        
            else if(_termFreq.ContainsKey(term))
            {
                _termFreq[term] ++ ;
                if (_termFreq[term]> _termFreq[_maxFreqTerm]) //new max TF.
                {
                    _maxFreqTerm = term;
                }
            }
            else
            {
                _termFreq.Add(term, 1);//add the first freq of a new word.
            }
        }
    }
}