using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace ConvertDictionary
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string json;
            List<string> myList = new List<string>();

            using (FileStream fs = new FileStream(@"C:\Users\becky\OneDrive\Brooks Stuff\Projects\Word Game\ConvertDictionary\dictionary.json", FileMode.Open))
            {
                using (StreamReader r = new StreamReader(fs))
                {
                    json = r.ReadToEnd();
                }

                fs.Dispose();
            }

            // Create a dictionary from the string "json"
            SortedList<string, string> myDictionary =
                JsonConvert.DeserializeObject<SortedList<string, string>>(json);

            foreach (KeyValuePair<string, string> kvp in myDictionary.OrderByDescending(key => key.Key.Length))
            {
                if (kvp.Key.Contains("-")) continue;
                if (kvp.Key.Contains(" ")) continue;

                // if word length is between 3 and 8 characters, write it to the output file
                if (kvp.Key.Length <= 8 && kvp.Key.Length >= 3)
                    File.AppendAllText(@"C:\Users\becky\OneDrive\Brooks Stuff\Projects\Word Game\ConvertDictionary\dictionary.txt", string.Format("{0}{1}", kvp.Key, Environment.NewLine));
            }
        }
    }
}