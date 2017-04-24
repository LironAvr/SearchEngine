using Search_Engine.Parser;
using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Search_Engine.ReadFile
{
    class MyReader : IFileReader
    {
        private static string[] filePaths;
        private static string[] seperator = { "<DOC>" };
        //private ArrayList doc;
        private IParser parser;
        private int _fileCounter;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string msg)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(msg));
        }

        /// <summary>
        /// File Reader Constuctor
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="parser"></param>
        public MyReader(IParser parser)
        {
            _fileCounter = 0;
            NotifyPropertyChanged("fileCounter");
            if (parser != null)
            {
                this.parser = parser;
            }
            else Console.WriteLine("Error - null Parser");
        }

        public void initiate(string path)
        {
            try
            {
                filePaths = Directory.GetFiles(path);
            }
            catch
            {
                Console.WriteLine("Error - Invalid Folder Path");
            }
        }

        public bool ReadFile()
        {
            NotifyPropertyChanged("Read File Started");
            string[] filePathElements;
            ArrayList doc = new ArrayList(); ;
            for (int i = 0; i < filePaths.Count(); i++)
            {
                //try
                {
                    using (StreamReader reader = new StreamReader(filePaths[i]))
                    {
                        //try
                        {
                            filePathElements = filePaths[i].Split('\\');
                            string currentLine;
                            while ((currentLine = reader.ReadLine()) != null)
                            {
                                if (currentLine == "<DOC>")
                                {
                                    //send back the doc and file name
                                    //parser parse doc
                                    //NotifyPropertyChanged(doc);
                                    doc = new ArrayList();
                                }
                                else if (currentLine == "</DOC>")
                                {
                                    /*if (check == true)*/
                                    parser.parseDoc(filePathElements.Last(), doc);
                                    //check = false;
                                }
                                else
                                {
                                    doc.Add(currentLine);
                                }                     
                            }
                        }
                        /*catch
                        {
                            Console.WriteLine("Cannot read File")
                            return false;
                        }*/
                    }
                }
                //catch { return false; };   
                _fileCounter++;
                NotifyPropertyChanged("fileCounter");
            }
            //NotifyPropertyChanged("ReadFile Ended");
            //EXPORT DATA
            //exportData();
            //parser.exportCommonPairs();
            return true;
        }

        public int fileCounter()
        {
            return _fileCounter;
        }

        private void exportData()
        {
           // parser.exportLanguages();
            //parser.exportAvgDocLen();
            parser.exportCommonPairs();
        }
    }
}
