using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.ComponentModel;
using Search_Engine.Utilities;
using System.Diagnostics;
using Search_Engine.Model.Indexer;

namespace Search_Engine.Indexer
{
    class MyIndexer : IIndexer
    {
        
        private string postingListPath;
        /// <summary>
        /// dictionary which fetch the data for term.
        /// The key = Term.
        /// Value - termData: location-> location at doc.
        ///                   doc-> adding to doc dictionray called :"doc_TF" in termData object.
        ///                   this list indicates all the relevant doc of this term.
        /// </summary>
        private Dictionary<string, TermData> Term_Dic;
        private Dictionary<string, Tuple<InvertedIndxTermData, int>> invIndex;
        private int documentCounter;
        private int posting_Part;
        private Dictionary<string, int> prefix_list;
        private bool firstRun = true;
        private int _totalTerms;
        private Stopwatch watch;
        private string status;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string msg)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(msg));
        }

        /// <summary>
        /// getter, setter to posting_Part field.
        /// </summary>
        public int PostingPart
        {
            get { return posting_Part; }
            set { posting_Part = value; }
        }

        /// <summary>
        /// getter, setter to documentCounter field.
        /// </summary>
        /// 
        private int DocumentCounter
        {
            get { return documentCounter; }
            set { documentCounter = value; }
        }

        /// <summary>
        /// getter, setter to Term_Dic field.
        /// </summary>
        public Dictionary<string, TermData> termDic
        {
            get { return Term_Dic; }
            set { Term_Dic = value; }
        }

        public string InvertedStatus
        {
            get { return status; }
            set { status = value; }
        }

        /// <summary>
        /// Constractor, to init a new instance of Indexer.
        /// </summary>
        public MyIndexer()
        {
            Term_Dic = new Dictionary<string, TermData>();
            invIndex = new Dictionary<string, Tuple<InvertedIndxTermData, int>>();
            posting_Part = 1;
            documentCounter = 0;
            _totalTerms = 0;
            watch = new Stopwatch();
            prefix_list = new Dictionary<string, int>();
            status = "Indexer Online";
            NotifyPropertyChanged("InvertedStatus");
        }

        /// <summary>
        /// Creating a folder for posting files.
        /// </summary>
        public void initPostingFolder()
        {
            //know name of posting folder with or without stemming.
            if (!ConValues.USE_STEMMER)
            {
                postingListPath = postingListPath + "\\Posting\\";
            }
            else
            {
                postingListPath = postingListPath + "\\Stemming_Posting\\";
            }
            //Check if Exists than Delete it before creation.
            if (Directory.Exists(postingListPath))
            {
                Directory.Delete(postingListPath, true); ;
            }
            Directory.CreateDirectory(postingListPath);
        }

        public void addTerm(string term, string fileNo, string DocNumber, int locationAtDoc, TermType type, string language)
        {
            if (firstRun)
            {
                status += "\nIndexing...";
                NotifyPropertyChanged("InvertedStatus");
                firstRun = false;
            }
            AddTerm(term, fileNo, DocNumber, locationAtDoc, type, language);
        }

        /// <summary>
        /// this function will update the dictionaries.
        /// </summary>
        /// <param name="term"></param> param from the parser
        /// <param name="locationAtDoc"></param>param from the parser
        /// <param name="DocNumber"></param>param from the parser
        private void AddTerm(string term, string fileNo, string DocNumber, int locationAtDoc, TermType type, string language)
        {
            if (!Term_Dic.ContainsKey(term))
            {
                TermData newTermData = new TermData(term, (int)type);
                Term_Dic.Add(term, newTermData);
            }
            Term_Dic[term].addDoc(DocNumber, locationAtDoc, fileNo, language);
            checkPostingLimit();
        }

        /// <summary>
        /// This function will check if we want to create a posting batch files
        /// of continue to parse more files. 
        /// The value that decide weather to stop is located at the conValue class.
        /// //(locationAtDoc / ConValues.DOC_PER_POSTING)
        /// </summary>
        private void checkPostingLimit()
        {

            if (Term_Dic.Count >= ConValues.termLimit)
            {
                creatingBinaryTermFiles();
                Term_Dic.Clear();
            }
        }

        public void finish()
        {
            status += "\nFinal Merging...";
            NotifyPropertyChanged("InvertedStatus");
            watch.Start();
            mergingHelp();
            loadInvIndex();
            watch.Stop();
            PostingPart = 1;
            NotifyPropertyChanged("mergeClock");
            NotifyPropertyChanged("TotalTerms");
        }

        /// <summary>
        /// Create new instences of Binary Writer in order to write the posting list files to the disk.
        /// </summary>
        #region CreateBinary

        private void creatingBinaryTermFiles()
        {
            if (!watch.IsRunning)
            {
                watch.Start();
            }
            Dictionary<string, BinaryWriter> openBW = new Dictionary<string, BinaryWriter>();
            BinaryWriter bwNum = new BinaryWriter(File.Open(postingListPath + "Num" + posting_Part + ".bin", FileMode.Append));
            BinaryWriter bwExpression = new BinaryWriter(File.Open(postingListPath + "Expression" + posting_Part + ".bin", FileMode.Append));
            BinaryWriter bwPercent = new BinaryWriter(File.Open(postingListPath + "Percent" + posting_Part + ".bin", FileMode.Append));
            BinaryWriter bwPrice = new BinaryWriter(File.Open(postingListPath + "Price" + posting_Part + ".bin", FileMode.Append));
            BinaryWriter bwDate = new BinaryWriter(File.Open(postingListPath + "Date" + posting_Part + ".bin", FileMode.Append));
            BinaryWriter bwWeight = new BinaryWriter(File.Open(postingListPath + "Weight" + posting_Part + ".bin", FileMode.Append));
            openBW.Add("Num", bwNum);
            openBW.Add("Expression", bwExpression);
            openBW.Add("Percent", bwPercent);
            openBW.Add("Price", bwPrice);
            openBW.Add("Date", bwDate);
            openBW.Add("Weight", bwWeight);

            //binary for each prefix pair in the abc//
            for (int i = 65; i <= 90; i++)
            {
                for (int j = 65; j <= 90; j++)
                {
                    string pair = ((char)i).ToString() + ((char)j).ToString();

                    BinaryWriter bWPair = new BinaryWriter(File.Open(postingListPath + pair + posting_Part + ".bin", FileMode.Append));
                    openBW.Add(pair, bWPair);
                }
            }

            foreach (KeyValuePair<string, TermData> KV in Term_Dic.OrderBy(i => i.Key))
            {
                string data = JsonConvert.SerializeObject(KV);
                bool findMyFile = false;
                var firstChar = KV.Key[0].ToString().ToUpper();
                string currentPrefix = "";
                bool sign = false;
                if (KV.Key.Length > 1)
                {// a word and not a number//
                    currentPrefix = KV.Key[0].ToString().ToUpper() + KV.Key[1].ToString().ToUpper();
                    if ((KV.Key[0] < (char)65 | KV.Key[0] > (char)122) | (KV.Key[0] > (char)90 & KV.Key[0] < (char)97))
                    {
                        sign = true;
                    }
                    else if ((KV.Key[1] < (char)65 | KV.Key[1] > (char)122) | (KV.Key[1] > (char)90 & KV.Key[1] < (char)97))
                    {
                        sign = true;
                    }
                }
                if (KV.Value.Type == 2)
                {
                    bwExpression.Write(data);
                    findMyFile = true;
                    continue;
                }
                else if (KV.Value.Type == 3)
                {
                    bwPercent.Write(data);
                    findMyFile = true;
                    continue;
                }
                else if (KV.Value.Type == 4)
                {
                    bwPrice.Write(data);
                    findMyFile = true;
                    continue;
                }
                else if (KV.Value.Type == 5)
                {
                    bwDate.Write(data);
                    findMyFile = true;
                    continue;
                }
                else if (KV.Value.Type == 6)
                {
                    bwWeight.Write(data);
                    findMyFile = true;
                    continue;
                }

                else if (KV.Value.Type == 1)
                {
                    bwNum.Write(data);
                    findMyFile = true;
                }
                //not a number                
                else if (!findMyFile & currentPrefix.Length > 1 & (!sign))
                {
                    try
                    {
                        openBW[currentPrefix].Write(data);
                    }
                    catch
                    {
                        Console.WriteLine("error in data/prefix" + currentPrefix + ":" + data);
                    }
                    findMyFile = true;
                }
            }

            foreach (BinaryWriter bw in openBW.Values)
            {
                bw.Flush();
                bw.Close();
            }
            posting_Part++;
            watch.Stop();
            NotifyPropertyChanged("mergeClock");
        }

        #endregion

        /// <summary>
        /// this method will save the inverted index on disc in order that we will
        /// later on will be able to load it without creating it again.
        /// </summary>
        public void saveInvIndex()
        {
            BinaryWriter bwInvInd = new BinaryWriter(File.Open(postingListPath + "InvIndex.bin", FileMode.Append));
            foreach (KeyValuePair<string, Tuple<InvertedIndxTermData, int>> p in invIndex)
            {
                string data = JsonConvert.SerializeObject(p);
                bwInvInd.Write(data);
            }
            bwInvInd.Flush();
            bwInvInd.Close();
        }

        /// <summary>
        /// This method will help us load an inverterd index file without creating a new one.
        /// </summary>
        public void loadInvIndex()
        {
            string path;
            if (!ConValues.USE_STEMMER)
            {
                path = ConValues.Posting + "\\Posting\\";
            }
            else path = ConValues.Posting + "\\Stemming_Posting\\";

            if (File.Exists(path + "InvIndex.bin"))
            {
                BinaryReader brInvInd = new BinaryReader(File.Open(path + "\\InvIndex.bin", FileMode.Open));
                while (brInvInd.BaseStream.Position < brInvInd.BaseStream.Length)
                {
                    string tmp = brInvInd.ReadString();
                    KeyValuePair<string, Tuple<InvertedIndxTermData, int>> KVP = JsonConvert.DeserializeObject<KeyValuePair<string, Tuple<InvertedIndxTermData, int>>>(tmp);
                    if(!invIndex.ContainsKey(KVP.Key))
                        invIndex.Add(KVP.Key, KVP.Value);
                    else
                    {
                        invIndex[KVP.Key].Item1.updatingValue(KVP.Value.Item1.gs_docFreq, KVP.Value.Item1.gs_tf_Corpus);
                    }
                }
                brInvInd.Close();
            }
            else
            {
                Console.WriteLine("Error - Inverted Index is missing");
            }
            loadAvgDocLength();
        }


        public void merging(string postingLetter)
        {
            mergingPostFiles(postingLetter);
        }

        /// <summary>
        /// This method will merge posting file for a few iteration of specific letter into
        /// one appearnce of posting file for the specific letter.
        /// </summary>
        private void mergingPostFiles(string postingLetter)
        {
            TermType type;
            switch (postingLetter)
            {
                case "Num":
                    type = TermType.Num;
                    break;
                case "Weight":
                    type = TermType.Weight;
                    break;
                case "Price":
                    type = TermType.Price;
                    break;
                case "Percent":
                    type = TermType.Percent;
                    break;
                case "Expression":
                    type = TermType.Expression;
                    break;
                case "Date":
                    type = TermType.Date;
                    break;
                default:
                    type = TermType.Word;
                    break;
            }
            Dictionary<string, TermData> MergeDic = new Dictionary<string, TermData>();
            for (int i = 1; i < posting_Part; i++) //maybe need to change to mergne only part and not all
            {
                using (BinaryReader br = new BinaryReader(File.Open((postingListPath + postingLetter + i + ".bin"), FileMode.Open)))
                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        string tmp = br.ReadString();
                        try
                        {
                            KeyValuePair<string, TermData> KVP = JsonConvert.DeserializeObject<KeyValuePair<string, TermData>>(tmp);
                            if (!MergeDic.ContainsKey(KVP.Key))
                            {
                                MergeDic.Add(KVP.Key, KVP.Value);
                            }
                            else
                            {
                                MergeDic[KVP.Key].updatingValue(KVP.Value);
                            }
                        }
                        catch { }
                    }
                File.Delete(postingListPath + postingLetter + i + ".bin");
            }
            var MergeOrder = MergeDic.OrderBy(key => key.Key);
            var FinalMergeDic = MergeOrder.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);

            int indexCounter = 1;
            BinaryWriter bwLetter = new BinaryWriter(File.Open(postingListPath + postingLetter + ".bin", FileMode.Append));
            BinaryWriter bwInvInd = new BinaryWriter(File.Open(postingListPath + "InvIndex.bin", FileMode.Append));
            foreach (KeyValuePair<string, TermData> KVP in FinalMergeDic)
            {
                KVP.Value.Type = (int)type;
                var invIndTerm = new InvertedIndxTermData(KVP.Key, KVP.Value.Type);
                var population = new Tuple<InvertedIndxTermData, int>(invIndTerm, indexCounter++);
                invIndTerm.gs_tf_Corpus = KVP.Value.gs_tf_Corpus;
                invIndTerm.gs_docFreq = KVP.Value.gs_docFreq;
                KeyValuePair<string, Tuple<InvertedIndxTermData, int>> inv = new KeyValuePair<string, Tuple<InvertedIndxTermData, int>>(KVP.Key, population);
                string InvData = JsonConvert.SerializeObject(inv);
                string data = JsonConvert.SerializeObject(KVP);
                bwLetter.Write(data);
                bwInvInd.Write(InvData);
                _totalTerms++;
                NotifyPropertyChanged("TotalTerms");
            }
            bwLetter.Flush();
            bwLetter.Close();
            bwInvInd.Flush();
            bwInvInd.Close();
        }

        public void mergingHelp()
        {
            creatingBinaryTermFiles();
            Term_Dic.Clear();
            
            for (int i = 65; i <= 90; i++)
            {
                for (int j = 65; j <= 90; j++)
                {
                    string pair = ((char)i).ToString() + ((char)j).ToString();
                    merging(pair);
                }
            }
            merging("Num");
            merging("Expression");
            merging("Percent");
            merging("Price");
            merging("Date");
            merging("Weight");
            status += "\nFinished Merging";
            NotifyPropertyChanged("InvertedStatus");
        }

        public int amountOfTerms()
        {
            return _totalTerms;
        }

        public void initiate(string path)
        {
            postingListPath = ConValues.Posting;
            initPostingFolder();
        }

        public double watchSeconds()
        {
            return watch.Elapsed.TotalSeconds;
        }

        public double watchMinutes()
        {
            return watch.Elapsed.Minutes;
        }

        public bool isInvertedIndex()
        {
            return InvertedIndex();
        }

        private bool InvertedIndex()
        {
            if (invIndex.Count > 0)
                return true;
            return false;
        }

        public Dictionary<string, Tuple<InvertedIndxTermData, int>> getInv()
        {
            return invIndex;
        }

        public Dictionary<string, Tuple<InvertedIndxTermData, int>> Index
        {
            get { return invIndex; }
        }


        public void clearIndex()
        {
            invIndex.Clear();
        }

        public void loadAvgDocLength()
        {
            double res;
            if (File.Exists(ConValues.Posting + "\\Avg_Doc_Len.txt"))
            {
                using (StreamReader reader = new StreamReader(ConValues.Posting + "\\Avg_Doc_Len.txt", true))
                {
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        var tmp = reader.ReadLine();
                        if (Double.TryParse(tmp, out res))
                            ConValues.avdl = res;
                    }
                }
            }
            else
            {
                Console.WriteLine("error - cannot upload avg doc len");
            }
        }
    }
}


