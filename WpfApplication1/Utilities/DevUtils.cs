using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Search_Engine.Utilities
{
    public static class DevUtils
    {
        public static List<string> semantic(string word)
        {
            List<string> termlist = new List<string>();
            string requestURL = "http://api.datamuse.com/words?ml=" + word + "&max=2";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestURL);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8);
            string result = reader.ReadToEnd();
            reader.Close();
            response.Close();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                JArray json = JArray.Parse(result);
                foreach (JToken token in json.Children())
                {
                    termlist.Add((string)token["word"]);
                }
            }
            return termlist;
        }
    }
}
