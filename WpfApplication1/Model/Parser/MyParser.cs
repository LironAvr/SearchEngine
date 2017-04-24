using Newtonsoft.Json;
using Search_Engine.Indexer;
using Search_Engine.Stemmer;
using Search_Engine.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Search_Engine.Parser
{
    /// <summary>
    /// Parser class
    /// </summary>
    class MyParser : IParser
    {
        //current doc info
        private string fileId;
        private string docNo;
        private string date;
        private string language;
        private string docTitle;
        private static bool INDEX = true;
        public event PropertyChangedEventHandler PropertyChanged;
        private DocumentData currentDocData;
        private HashSet<string> languages;
        private HashSet<string> stopWords;
        private IIndexer indexer;
        private IStemmer stemmer;
        private Stopwatch watch;
        private List<string> _query;

        //current metadata
        private int index;
        private bool stem = false;
        //bool firstRun = true;
        public int _docCounter;
        private long totalTermCounter;

        /// <summary>
        /// MyParser Constructor
        /// </summary>
        /// <param name="stopWordsPath"></param>
        /// <param name="indexer"></param>
        /// <param name="stemmer"></param>
        /// <param name="stemming"></param>
        public MyParser(IIndexer indexer, IStemmer stemmer, bool stemming)
        {
            stopWords = new HashSet<string>();
            languages = new HashSet<string>();
            this.indexer = indexer;
            this.stemmer = stemmer;
            stem = stemming;
            _docCounter = 0;
            watch = new Stopwatch();
            totalTermCounter = 0;
        }

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        #region StopWords

        public HashSet<string> StopWords
        {
            get { return stopWords; }
            set { stopWords = value; }
        }

        //Initiates the stopwords dictionary
        public void initiateStopWords(string path)
        {
            try
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    string[] words = reader.ReadToEnd().Split('\n');
                    foreach (string word in words)
                    {
                        addStopWord(word.TrimEnd('\r'));
                    }
                }
            }
            catch { Console.WriteLine("Error {0.1}"); }
            AddTagsToSW();
        }

        //Adds a stopword to the dictionary
        private void addStopWord(string word)
        {
            try
            {
                stopWords.Add(word);
            }
            catch { /*Duplicated stop word*/ }
        }

        /// <summary>
        /// a method to determine if a word is a stop word or not.
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        private bool isStopWord(string word)
        {
            bool ans = false;
            if (stopWords.Contains(word.ToLower()))
            {
                ans = true;
            }
            return ans;
        }

        //Adds some extra unwanted signs and tags to the stopwords dictionary
        private void AddTagsToSW()
        {
            addStopWord("[article]");
            addStopWord("[article");
            addStopWord("article]");
            addStopWord("[text]");
            addStopWord("[text");
            addStopWord("text]");
            addStopWord("[editorial report]");
            addStopWord("[excerpt]");
            addStopWord("[excerpts]");
            addStopWord("[itim report]");
            addStopWord("[summary]");
            addStopWord(".");
            addStopWord("");
            addStopWord(",");
            addStopWord("-");
            addStopWord("[");
            addStopWord("]");
            addStopWord(" ");
            addStopWord("f");
            addStopWord("/f");
            addStopWord("f p=106");
            addStopWord("bfn");
            addStopWord("p=106");
            addStopWord("<p=106>");
            addStopWord("h");
            addStopWord("h1");
            addStopWord("h2");
            addStopWord("h3");
            addStopWord("h4");
            addStopWord("h5");
            addStopWord("/h");
            addStopWord("/h1");
            addStopWord("/h2");
            addStopWord("/h3");
            addStopWord("/h4");
            addStopWord("/h5");
        }

        #endregion

        #region Parsing

        //Initiates the doc parsing sequence
        public void parseDoc(string fileName, ArrayList doc)
        {
            watch.Start();
            this.fileId = fileName;
            this.docNo = null;
            this.date = null;
            this.language = "0";
            this.docTitle = null;
            totalTermCounter += index;
            index = 0;
            currentDocData = new DocumentData(fileId);
            startParse(doc);
        }

        public List<string> parseQuery(string query)
        {
            _query = new List<string>();
            parseText(stem, query, !INDEX);
            return _query;
        }

        //First Part of Parsing - Extracts important information about the doc (by tags)
        private void startParse(ArrayList doc)
        {
            ArrayList headlineData = null;
            string text = null;
            bool headline = true, textDone = false;
            foreach (string line in doc)
            {
                if (line.Contains("</HEADLINE>"))
                {
                    headline = false;
                    continue;
                }
                else if (line.Contains("</TEXT>"))
                {
                    textDone = true;
                    parseText(stem, text, INDEX);
                    continue;
                }

                if (line.Contains("<DOCNO>"))
                {
                    docNo = line.Split(new string[] { "<DOCNO>", "</DOCNO>" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim(' ');
                }
                else if (line.Contains("<DATE1>"))
                {
                    date = line.Split(new string[] { "<DATE1>", "</DATE1>" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim(' ');
                }
                else if (line.Contains("<F P=105>"))
                {
                    string[] lang = line.Split(new string[] { "<F P=105>", "</F>" }, StringSplitOptions.RemoveEmptyEntries);
                    if (lang.Length > 1)
                    {
                        language = lang[1].Trim(' ', ',', ':', ';', '-', '\r');
                    }
                    else language = lang[0].Trim(' ', ',', ':', ';', '-', '\r');
                    //////////
                    if (language == null) language = "0";
                    else if (language.Contains(" "))
                    {
                        language = language.Split(' ')[0];
                    }
                    //////////
                    try
                    {
                        language = ConValues.Languages[language.ToLower()];
                    }

                    catch { language = "0"; }
                    languages.Add(language.ToLower());
                }
                else if (line.Contains("</TI></H3>"))
                {
                    docTitle = line.Split(new string[] { "<H3> <TI>", "</TI></H3>" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim(' ');
                }
                else if (line.Contains("<HEADLINE>"))
                {
                    headline = true;
                    headlineData = new ArrayList();
                }
                else if (line.Contains("<TEXT>"))
                {
                    textDone = false;
                    text = "";
                }
                else if (line.Contains("<F>") | line.Contains("Type:BFN"))
                {
                    continue;
                }
                else if (headlineData != null & headline)
                {
                    headlineData.Add(line);
                    continue;
                }
                else if (text != null & !textDone)
                {
                    text += " " + line;
                    continue;
                }
            }
        }

        //Second Part of the Parsing - Parses the words in the Text field of the document
        private void parseText(bool stem, string docText, bool isIndex)
        {
            if (isIndex)
            {
                currentDocData.DocId = docNo;
                currentDocData.DocTitle = docTitle;
                currentDocData.Language = language;
            }

            double check;
            string currWord;
            string[] text = docText.Split(new char[] { '\t', '\"', ' ', ':', '\\', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            //Main Parsing loop - for every word in the text
            int i;
            for (i = 0 ; i < text.Length; i++)
            {
                //between
                currWord = text[i];
                if (currWord.ToLower() == "between" & i + 3 < text.Length)
                {
                    if (ConValues.CONNECTORS.Contains(text[i + 2].ToLower()))
                    {
                        currWord += " " + (text[i + 1] + " " + text[i + 2] + " " + text[i + 3]);
                        addTerm(currWord.ToLower(), fileId, docNo, TermType.Expression, isIndex);
                        i += 3;
                        continue;
                    }
                }

                //Stop Words
                if (isStopWord(currWord)) continue;

                //Dates
                if (isDay(currWord) & i + 1 < text.Length && isMonth(text[i + 1]))
                {
                    if (i + 2 < text.Length && isYear(text[i + 2]))
                    {
                        currWord = convertToDateFormat(text[i], text[i + 1], text[i + 2]);
                        addTerm(currWord, fileId, docNo, TermType.Date, isIndex);
                        i += 2;
                        continue;
                    }
                    else
                    {
                        currWord = convertToDateFormat(text[i], text[i + 1], "0");
                        addTerm(currWord, fileId, docNo, TermType.Date, isIndex);
                        i++;
                        continue;
                    }
                }
                else if (i + 1 < text.Length && (isMonth(currWord) & isDay(text[i + 1])))
                {
                    if (i + 2 < text.Length && isYear(text[i + 2]))
                    {
                        currWord = convertToDateFormat(text[i + 1], text[i], text[i + 2]);
                        addTerm(currWord, fileId, docNo, TermType.Date, isIndex);
                        i += 2;
                        continue;
                    }
                    else
                    {
                        currWord = convertToDateFormat(text[i + 1], text[i], "0");
                        addTerm(currWord, fileId, docNo, TermType.Date, isIndex);
                        i++;
                        continue;
                    }
                }
                else if ((i + 1 < text.Length && (isMonth(currWord) & isYear(text[i + 1]))))
                {
                    currWord = convertToDateFormat("0", text[i], text[i + 1]);
                    addTerm(currWord, fileId, docNo, TermType.Date, isIndex);
                    i++;
                    continue;
                }

                //Expressions (more than one word Between or - ) 
                if (currWord.Contains('-'))
                {
                    string[] expr = currWord.Split('-');
                    if (expr[0] != "" & isNumber(expr[0]))
                    {
                        expr[0] = converToNumber(expr[0]);
                        addTerm(expr[0], fileId, docNo, TermType.Num, isIndex);
                    }
                    if (expr[1] != "" & isNumber(expr[1]))
                    {
                        expr[1] = converToNumber(expr[1]);
                        addTerm(expr[1], fileId, docNo, TermType.Num, isIndex);
                    }
                    addTerm((expr[0] + "-" + expr[1]).ToLower(), fileId, docNo, TermType.Expression, isIndex);
                    continue;
                }

                else if (currWord.Contains("["))
                {
                    string word = currWord.Trim('[');
                    if (!currWord.Contains(']'))
                        addTerm(word.ToLower(), fileId, docNo, TermType.Word, isIndex);
                    else { addTerm(currWord.Trim(']').ToLower(), fileId, docNo, TermType.Expression, isIndex); continue; }
                    while (i + 1 < text.Length && !word.Contains(']'))
                    {
                        if (addTerm(text[i + 1].Trim(']').ToLower(),fileId,docNo, TermType.Word, isIndex))
                            word += " " + text[i + 1];
                        i++;
                    }
                    addTerm(word.Trim(']').ToLower(), fileId, docNo, TermType.Expression, isIndex);
                    continue;
                }

                //Precents
                if (Double.TryParse(currWord[0].ToString(), out check))
                {
                    if (currWord.Contains('%'))
                    {
                        addTerm(currWord, fileId, docNo, TermType.Percent, isIndex);
                        continue;
                    }
                    else if (i + 1 < text.Length && isPercentage(text[i + 1]))
                    {
                        currWord += '%';
                        addTerm(currWord, fileId, docNo, TermType.Percent, isIndex);
                        i++;
                        continue;
                    }
                }

                //Prices
                if (currWord[0] == '$')
                {
                    if (currWord.Length > 1)
                        currWord = converToNumber(currWord.Substring(1)) + " Dollars";
                    else currWord = "Dollars";
                    addTerm(currWord, fileId, docNo, TermType.Price, isIndex);
                    continue;
                }
                else if (isNumber(currWord))
                {
                    if (i + 1 < text.Length && isDollarSign(text[i + 1].ToLower()))
                    {
                        currWord = converToNumber(currWord) + " Dollars";
                        i++;
                        addTerm(currWord, fileId, docNo, TermType.Price, isIndex);
                        continue;
                    }
                    else if (i + 3 < text.Length && (isNumSuffix(text[i + 1]) & isUSD(text[ i + 2].ToLower()) & isDollarSign(text[i +3])))
                    {
                        currWord = converToNumber(currWord) + fixSuffix(text[i + 1]) + " Dollars";
                        i += 3;
                        addTerm(currWord, fileId, docNo, TermType.Price, isIndex);
                        continue;
                    }

                //Numbers
                    else if (i + 1 < text.Length && isNumSuffix(text[i + 1]))
                    {
                        currWord = converToNumber(currWord, text[i + 1]);
                        i++;
                        addTerm(currWord, fileId, docNo, TermType.Num, isIndex);
                        continue;
                    }
                    else if (i + 1 < text.Length && isWeightSuffix(text[i + 1]))
                    {
                        currWord = converToNumber(currWord, text[i + 1]);
                        i++;
                        addTerm(currWord, fileId, docNo, TermType.Weight, isIndex);
                        continue;
                    }
                    else
                    {
                        currWord = converToNumber(currWord);
                        addTerm(currWord, fileId, docNo, TermType.Num, isIndex);
                        continue;
                    }
                }

                //Stemmer
                currWord = fixWord(currWord);
                if (currWord.Length < 2) continue;
                if (ConValues.USE_STEMMER) currWord = stemmer.stemTerm(currWord.ToLower());

                //Add Word
                addTerm(currWord.ToLower(), fileId, docNo, TermType.Word, isIndex);
                NotifyPropertyChanged("parseClock");
            }

            if (isIndex)
            {
                currentDocData.DocLength = i;
                currentDocData.writeFile();
                _docCounter++;
                NotifyPropertyChanged("docCounter");
            }
        }

        //Removes all unwanted signs in the beggining and in the end of the term
        private string fixWord(string currWord)
        {
            var sb = new StringBuilder();

            foreach (char c in currWord)
            {
                if (!char.IsPunctuation(c))
                    sb.Append(c);
            }
            return sb.ToString() ;
        }

        #endregion

        #region NumberHandlers

        //checks if suffix is a legit number suffix
        private bool isNumSuffix(string suffix)
        {
            suffix = suffix.ToLower();
            return ((suffix == "m" | suffix == "million" | suffix == "bn" | suffix == "billion" | suffix == "trillion")
                | (isNumber(suffix) & suffix.Contains("/")));
          
        }

        /// <summary>
        /// this method will check if a term is a number.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public bool isNumber(string term)
        {
            double temp;
            if (term.Contains('$') & term.Contains('%')) { return false; }
            term = term.Replace(",", "");
            term = term.Replace("/", "");
            return Double.TryParse(term, out temp);
        }

        //Converts the term into a proper Number term
        private string converToNumber(string currWord, string suffix = null)
        {
            string word = "";
            if (suffix != null)
            {
                if (!suffix.Contains("/"))
                {
                    suffix = fixSuffix(suffix);
                    word = (fixNumber(currWord + suffix));
                }
                else word = addFraction(currWord, suffix);
            }
            else word = fixNumber(currWord);
            return word;
        }

        //Adds a fraction to the main number
        private string addFraction(string currWord, string suffix)
        {
            string[] numbers = suffix.Split('/');
            double first, second, prefix;
            if (Double.TryParse(numbers[0], out first) & Double.TryParse(numbers[1], out second) & Double.TryParse(currWord.Replace(",", ""), out prefix))
            {
                first = prefix + (first / second);
                return first.ToString();
            }
            else return currWord;
        }

        //Fixing a number to a proper Number form
        private string fixNumber(string currWord)
        {
            string newNumber = currWord.Replace(",", "");
            double number;

            if (Double.TryParse(newNumber, out number) && number > 1000000)
            {
                number = number / 1000000;
                newNumber = number.ToString() + " M";
                return newNumber;
            }
            else if (currWord.Remove(0, currWord.Length - 1).ToLower() == "m")
            {
                newNumber = currWord.Replace("m", " M");
                return newNumber;
            }

            else if (currWord.Length > 1)
            {
                if (currWord.Remove(0, currWord.Length - 2).ToLower() == "bn")
                {
                    newNumber = currWord.Replace("bn", "000 M");
                    return newNumber;
                }
                else if (currWord.Remove(0, currWord.Length - 2).ToLower() == "mm")
                {
                    newNumber = currWord.Replace("mm", "000000 M");
                    return newNumber;
                }
            }
            return currWord;
        }

        //Return the suffix of a number in its proper form
        private string fixSuffix(string suffix)
        {
            suffix = suffix.ToLower();
            if (suffix == "m" | suffix == "million") return " M";
            else if (suffix == "bn" | suffix == "billion") return "000 M";
            else if (suffix == "tn" | suffix == "trillion") return "000000 M";
            else if (suffix == "gram" | suffix == "grams") return " grams";
            else if (suffix == "kg" | suffix == "kgs" | suffix == "kilogram" | suffix == "kilograms") return " kgs";
            else if (suffix == "ton" | suffix == "tons") return " tons";
            else return suffix;
        }

        #endregion

        #region Percents Handler

        //Checks if nextWord represents precents
        private bool isPercentage(string nextWord)
        {
            nextWord = nextWord.ToLower();
            if (nextWord == "percentage" | nextWord == "percent" | nextWord == "%") return true;
            else return false;
        }

        #endregion

        #region DateHandlers

        /// <summary>
        /// this method will help us understand if the term is DD or DDth
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        private bool isDay(string term)
        {
            //check if number are in format DDth
            if (term.EndsWith("th") & term.Length == 4)
                return true;

            int tmp = 0;
            if (Int32.TryParse(term, out tmp) & (tmp < 32 & tmp > 0))
                return true;

            return false;
        }

        /// <summary>
        /// this method will help us understand the format of the month.
        /// if the term is a month the answer will be true
        /// else
        /// false.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        private bool isMonth(string term)
        {
            return ConValues.MONTHS.ContainsKey(term.ToLower());
        }

        //Checks if term represents a year
        private bool isYear(string term)
        {
            int year = 0;
            if (term.Length == 4 | term.Length == 2)
                return (Int32.TryParse(term, out year));
            
            else return false;
        }

        //Returns term in a proper month format
        private string monthToMMformat(string term)
        {
            return ConValues.MONTHS[term.ToLower()];
        }

        /// <summary>
        /// This method will take 3 param and will convert it to a valid Date format as wanted.
        /// </summary>
        /// <param name="day"></param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        private string convertToDateFormat(string day, string month, string year)
        {
            string ans = "";
            if (day.Length > 2)
            {
                day = day.Substring(0, 2);
            }
            //MM-DD
            if (year == "0")
            {
                return monthToMMformat(month) + "-" + day;
            }
            //YYYY-MM or YYYY-MM-DD
            else
            {
                if (year.Length == 2)
                {
                    int yearPrefix;
                    Int32.TryParse(year, out yearPrefix);
                    if (yearPrefix < 30)
                    {
                        ans = "20" + year + "-" + monthToMMformat(month);
                    }
                    else
                    {
                        ans = "19" + year + "-" + monthToMMformat(month);
                    }
                }
                else
                    ans = year + "-" + monthToMMformat(month);

                if (day == "0")
                {
                    return ans;
                }
                else return ans + "-" + day;
            }
        }

        #endregion

        #region Prices Handlers
        /// <summary>
        /// this method will return true or false if the term contain a $ sign.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        private bool isDollarSign(string term)
        {
            return (term.Contains("$") | term == "dollar" | term == "dollars");
        }

        /// <summary>
        /// this method will be called only when isDollarSign return true.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        private string NumberAfterDollarSign(string term)
        {
            return term.Substring(1, term.Length - 1);
        }

        //Checks if check is U.S (Dollars)
        private bool isUSD(string check)
        {
            if (check == "u.s.") return true;
            return false;
        }
        #endregion

        #region WeightHandlers

        private bool isWeightSuffix(string suffix)
        {
            suffix = suffix.ToLower();
            return (suffix == "gram" | suffix == "ton" | suffix == "kg" | suffix == "grams" | suffix == "tons" | suffix == "kgs");
        }

        #endregion

        //Adds a term by adding it to the DocumentData and sending it to the indexer
        private bool addTerm(string term, string file, string doc, TermType type, bool posting)
        {
            if (indexer == null) { Console.WriteLine("Error - NULL Indexer"); return false; }
            term = term.Trim(new char[] { '"', '!', '?', ';', ' ', ':', '\\', '(', ')', '{', '}', '|', '~', '#', '.', ',', '-', '<', '>', '`', (char)96, (char)39, '_' });
            if (isStopWord(term)) return false;

            /**/term = term.Trim(new char[] { '[', ']' });
            if (posting)
            {
                currentDocData.addTerm(term);
                watch.Stop();
                indexer.addTerm(term, file, doc, index, type, language);
                watch.Start();
                index++;
            }
            else
                _query.Add(term);
            
            return true;
        }

        public void resetWatch()
        {
            watch.Reset();
        }

        public double watchSeconds()
        {
            return watch.Elapsed.TotalSeconds;
        }

        public double watchMinutes()
        {
            return watch.Elapsed.TotalMinutes;
        }

        public int docCounter()
        {
            return _docCounter;
        }

        public HashSet<string> getLanguages()
        {
            return languages;
        }

        public void exportLanguages()
        {
            using (System.IO.StreamWriter file1 =
                    new System.IO.StreamWriter(ConValues.Posting + "\\Languages.txt", true))
            {
                foreach (string lang in languages)
                    file1.WriteLine(lang);
            }
        }

        public void exportAvgDocLen()
        {
            using (System.IO.StreamWriter file1 =
                    new System.IO.StreamWriter(ConValues.Posting + "\\Avg_Doc_Len.txt", true))
            {
                file1.WriteLine(totalTermCounter / (double)_docCounter);
            }
            ConValues.totalNumberOfDoc = _docCounter;
        }

        public void exportCommonPairs()
        {
            var ans = fixPairs();
            string path = ConValues.Posting;
            if (ConValues.USE_STEMMER)
                path += "\\Common_Pairs(stem).txt";
            else path += "\\Common_Pairs(unstemmed).txt";
            using (System.IO.StreamWriter file1 =
                    new System.IO.StreamWriter(path, true))
            {
                foreach (KeyValuePair<string, List<string>> current in ans)
                {
                    file1.WriteLine(JsonConvert.SerializeObject(current));
                }
            }
        }

        private Dictionary<string, List<string>> fixPairs()
        {
            Dictionary<string, Dictionary<string, int>> tempDic = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, List<string>> ans = new Dictionary<string, List<string>>();
            List<string> keys = new List<string>();
            keys.AddRange(_commonPairs.Keys);

            foreach (string term in keys)
            {
                var sortedDict = _commonPairs[term].OrderByDescending(entry => entry.Value)
                               .Take(5)
                               .ToDictionary(pair => pair.Key, pair => pair.Value);
                
                tempDic.Add(term, sortedDict);
                _commonPairs.Remove(term);
            }
            _commonPairs.Clear();
            _commonPairs = tempDic;

            foreach (string key in keys)
            {
                ans.Add(key, new List<string>(_commonPairs[key].Keys));
            }           

            return ans;
        }

        private Dictionary<string, Dictionary<string, int>> _commonPairs = new Dictionary<string, Dictionary<string, int>>();
        private string prevWord = null;


        private void addCommonPair(string term, string prev)
        {
            if (prev == null) return;
            if (_commonPairs.ContainsKey(prev))
            {
                if (_commonPairs[prev].ContainsKey(term))
                {
                    _commonPairs[prev][term]++;
                }
                else
                    _commonPairs[prev].Add(term, 1);
            }
            else
            {
                _commonPairs.Add(prev, new Dictionary<string, int>());
                _commonPairs[prev].Add(term, 1);
            }
        }
    }
}
