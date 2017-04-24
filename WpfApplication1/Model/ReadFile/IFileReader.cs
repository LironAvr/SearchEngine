using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Search_Engine.ReadFile
{
    interface IFileReader : INotifyPropertyChanged
    {
        bool ReadFile();
        int fileCounter();
        void initiate(string path);
    }
}
