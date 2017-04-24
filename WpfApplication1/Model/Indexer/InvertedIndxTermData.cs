using Newtonsoft.Json;

namespace Search_Engine.Model.Indexer
{
    public class InvertedIndxTermData
    {
        protected string term;
        protected int tf_Corpus;//Term Freq in Corpus
        protected int docFreq;
        protected string docNumber;
        protected string fileName;
        protected int type;

        public InvertedIndxTermData(string newTerm, int termType)
        {
            term = newTerm;
            tf_Corpus = 0;
            docFreq = 0;
            type = termType;
        }

        public int gs_tf_Corpus
        {
            get { return tf_Corpus; }
            set { tf_Corpus = value; }
        }

        public int gs_docFreq
        {
            get { return docFreq; }
            set { docFreq = value; }
        }

        public int Type
        {
            get { return type; }
            set { type = value; }
        }

        public virtual void updatingValue(int df, int dicTF)
        {
            docFreq = docFreq + df;
            tf_Corpus = tf_Corpus + dicTF;
        }
    }
}
