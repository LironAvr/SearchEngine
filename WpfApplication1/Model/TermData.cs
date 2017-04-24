using System;
using System.Collections.Generic;
using System.Linq;
using Search_Engine.Model.Indexer;

namespace Search_Engine
{
    public enum TermType : int { Word, Num, Expression, Percent, Price, Date, Weight };

    [Serializable]
    public class TermData : InvertedIndxTermData
    {
        private Dictionary<string, Dictionary<string, List<int>>> docTermLocations;
        
        /// <summary>
        /// Constroctur inorder to init the term.
        /// 
        /// term field = name of the the new term. for example: student.
        /// tf_Corpus = term frequency in all corpus.
        /// docFreq = in how many unique document this term is appearing.
        /// docNumber = document number, for example: FBIS3-1.
        /// fileName = file name, for example: FB396001.
        /// doc_TF will be a dictionary in order to know all the doc that this term appear on.
        /// tf and df will gain value greater than one when they will have relation to a document.
        /// </summary>
        /// <param name="newTerm"></param>
        public TermData(string newTerm, int termType) : base(newTerm, termType)
        {
            docTermLocations = new Dictionary<string, Dictionary<string, List<int>>>();
        }
       
        public Dictionary<string, Dictionary<string, List<int>>> gs_docTermLocations
        {
            get { return docTermLocations; }
            set { docTermLocations = value; }
        }

        /// <summary>
        /// this function will add a document to the dc_TF dictionary if it's new.
        /// If this term is already appearing the this document this function will add the new location
        /// to the list appearing in the dictaionary value.
        /// 
        /// We will update the counters (docFreq,tf_Corpus) when necessary
        /// </summary>
        /// <param name="document"></param>
        /// <param name="termLocation"></param>                     
        public void addDoc(string document, int termLocation, string file, string language)
        {
            var uniqueFileDoc = file + document;
            if (language == "") language = "0";
            if (!docTermLocations.ContainsKey(language))
                docTermLocations.Add(language, new Dictionary<string, List<int>>());

            if (!docTermLocations[language].ContainsKey(uniqueFileDoc))//new doc.
            {
                List<int> appear = new List<int>();
                fileName = file;
                docNumber = document;
                appear.Add(termLocation);
                docTermLocations[language].Add(uniqueFileDoc, appear);
                docFreq++;
                tf_Corpus++;
            }
            else // existing doc
            {
                docTermLocations[language][uniqueFileDoc].Add(termLocation);
                tf_Corpus++;
            }
        }

        /// <summary>
        /// calc the tf of a term in a specific doc.
        /// zero indicate that there is now apperance of the term in this document.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public int tfAtDoc(string document)
        {
            if (!docTermLocations.ContainsKey(document))
                return 0;
            else
                return docTermLocations[document].Count();
        }

        public void updatingValue(TermData td)
        {
            foreach(string lang in td.gs_docTermLocations.Keys)
            {
                foreach (KeyValuePair<string, List<int>> update in td.gs_docTermLocations[lang])
                {
                    if (!docTermLocations.ContainsKey(lang)) docTermLocations.Add(lang, new Dictionary<string, List<int>>());

                    if (docTermLocations[lang].ContainsKey(update.Key))
                    {
                        docTermLocations[lang][update.Key].AddRange(update.Value);
                    }
                    else
                    {
                        docTermLocations[lang].Add(update.Key, update.Value);
                    }
                }
            }
                docFreq += td.gs_docFreq;
                tf_Corpus += td.gs_tf_Corpus;
        }
    }
}