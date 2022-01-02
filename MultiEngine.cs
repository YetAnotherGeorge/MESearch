using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace MESearch
{
    class SearchEngine
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string QueryFormat { get; set; }

        public SearchEngine()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="QueryFormat">use %s in place of query</param>
        /// <param name="ShortName">preferably a single char to denote this particular search engine</param>
        public SearchEngine(string Name, string ShortName, string QueryFormat)
        {
            this.Name = Name;
            this.QueryFormat = QueryFormat;
            this.ShortName = ShortName;
        }
        /// <summary>
        /// Initialize search engine from JSON string {"Name":"...", ...} or simplified {"Name", "ShortName", "QueryFormat"}
        /// </summary>
        /// <param name="json">JSON or simplified JSON string</param>
        public SearchEngine(string json)
        {
            //Possible formats:
            //{"Name":"Google","ShortName":"g","QueryFormat":"https://www.google.com/search?q=%s"}
            //{"Google", "g", "https://www.google.com/search?q=%s"}

            //find non escaped quotation mark count
            int qc = (json[0] == '\"' || json[0] == '\'') ? 1 : 0;
            for (int i = 1; i < json.Length; i++)
                if ((json[i] == '\"' || json[i] == '\'') && json[i - 1] != '\\')
                    qc++;

            //if 12 quotes assume full JSON string, if 6 custom deserialization
            if (qc == 12)
            {
                SearchEngine e = JsonConvert.DeserializeObject<SearchEngine>(json);
                this.Name = e.Name;
                this.ShortName = e.ShortName;
                this.QueryFormat = e.QueryFormat;
            }
            else if (qc == 6)
            {
                if (!json.StartsWith('{') || !json.EndsWith('}'))
                    throw new Exception($"Invalid JSON string: {json}");
                json = json.Substring(1, json.Length - 2).Trim();

                //split JSON by ',' not inside of quotation marks (\" will be ignored)
                List<string> parts = new List<string>();
                StringBuilder sb = new StringBuilder();

                bool inside = json[0] == '\"';
                for (int i = 1; i < json.Length; i++)
                {
                    if (json[i] == ',' && !inside)
                    {
                        parts.Add(sb.ToString().Trim());
                        sb.Clear();
                        continue;
                    }

                    if (json[i] == '\"' && json[i - 1] != '\\')
                        inside = !inside;
                    else
                        sb.Append(json[i]);
                }
                if (sb.Length > 0)
                    parts.Add(sb.ToString().Trim());

                if (parts.Count != 3)
                    throw new Exception("Parsing exception, parts.Count != 3");

                this.Name = parts[0];
                this.ShortName = parts[1];
                this.QueryFormat = parts[2];

                //Console.WriteLine(parts.ToStringCF(new ContainerFormat("[\n|   @\n   @|\n]")));
            }
            else
                throw new Exception($"Invalid SearchEngine string: {json}");
        }

        public string CreateQueryURL(string search)
        {
            string query = QueryFormat.Replace("%s", HttpUtility.UrlEncode(search));
            return query;
        }
        public override string ToString()
        {
            return $"Search Engine object: [name: \"{Name}\", shortName: \"{ShortName}\", queryFormat: \"{QueryFormat}\"]";
        }
    }

    static class MultiEngine
    {
        //Save file format:
        //[line 0]:     browserPath \r\n
        //[lines 1-..]: engineDictionary json
        //
        //engineDictionary uses the StringHashU32 of the ShortName property
        private const string dataFilePath = "MESearchDat.dat";
        private static Dictionary<uint, SearchEngine> engineDictionary;
        private static string browserPath;

        static uint StringHashU32(string str)
        {
            UInt64 hashedValue = 3074457345618258791ul;
            for (int i = 0; i < str.Length; i++)
            {
                hashedValue += str[i];
                hashedValue *= 3074457345618258799ul;
            }
            return (uint)hashedValue;
        }

        /// <summary>
        /// Initializes multi engine manager from disk and fills engines list with existing engines
        /// </summary>
        public static void Init()
        {
            //load engines and browserPath from disk
            if (!System.IO.File.Exists(dataFilePath))
            {
                browserPath = null;
                engineDictionary = new Dictionary<uint, SearchEngine>();
            }
            else
            {
                string txt = System.IO.File.ReadAllText(dataFilePath);
                int flp = txt.IndexOf("\r\n"); //first line position

                browserPath = txt.Substring(0, flp);
                string dictionaryJSON = txt.Substring(flp + 2);
                engineDictionary = JsonConvert.DeserializeObject<Dictionary<uint, SearchEngine>>(dictionaryJSON);
            }
        }
        private static void UpdateFile()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(browserPath);
            sb.Append("\r\n");
            sb.Append(JsonConvert.SerializeObject(engineDictionary, Formatting.Indented));

            System.IO.File.WriteAllText(dataFilePath, sb.ToString());
        }


        /// <summary>
        /// Add engine to engine collection
        /// </summary>
        /// <param name="toAdd"></param>
        public static void AddEngine(SearchEngine toAdd)
        {
            uint key = StringHashU32(toAdd.ShortName);

            if (engineDictionary.ContainsKey(key))
            {
                engineDictionary.ToList().ForEach((kvp) => Console.WriteLine("   " + kvp.Value));

                SearchEngine inDict = engineDictionary[key];
                Console.ForegroundColor = ConsoleColor.Red;

                if (inDict.ShortName == toAdd.ShortName)
                    throw new Exception($"\nCannot add search engine \"{toAdd}\", engine already exists");
                else
                    throw new Exception($"\nCannot add search engine {toAdd} because of hash mismatch with engine {inDict}");
            }

            engineDictionary.Add(key, toAdd);
            Console.WriteLine($"Added {toAdd}.\n");
            UpdateFile();
        }
        /// <summary>
        /// Remove engine from engine collection
        /// </summary>
        /// <param name="ShortName"></param>
        public static void RemEngine(string ShortName)
        {
            uint key = StringHashU32(ShortName);
            if (!engineDictionary.ContainsKey(key))
                throw new Exception($"ShortName not found in engine dictionary: {ShortName}\n");

            SearchEngine toRem = engineDictionary[key];
            engineDictionary.Remove(key);
            Console.WriteLine($"Removed {toRem}.\n");
            UpdateFile();
        }
        /// <summary>
        /// Sets the browserPath variable
        /// </summary>
        /// <param name="path"></param>
        public static void SetBrowserPath(string path)
        {
            browserPath = path;
            UpdateFile();
        }

        /// <summary>
        /// Opens the required tabs based on the search query
        /// </summary>
        /// <param name="query">in the format ":ShortNames query"</param>
        public static void PerformSearch(string query)
        {
            if(browserPath == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Browser Path not set. Set Browser Path using the -BrowserPath command.\n");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            int fs = query.IndexOf(' '); //first space position
            if (!query.StartsWith(':') || fs == -1)
                throw new Exception($"Invalid search query: {query}");

            //get selected search engines string
            string es = query.Substring(1, fs - 1).ToLower();

            //split by ',' or by nothing
            string[] shortNames;

            if(es.Contains(','))
                shortNames = es.Split(',', StringSplitOptions.RemoveEmptyEntries);
            else
            {
                shortNames = new string[es.Length];
                for (int i = 0; i < es.Length; i++)
                    shortNames[i] = es[i].ToString();
            }

            //get actual query string
            query = query.Substring(fs + 1);

            //generate urls and open them
            for(int i = 0; i < shortNames.Length; i++)
            {
                uint h = StringHashU32(shortNames[i]);
                SearchEngine e = engineDictionary[h];
                string url = e.CreateQueryURL(query);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"[{e.Name, -15}]: ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(url);

                Process.Start(browserPath, url);
            }
            Console.WriteLine();
        }
        /// <summary>
        /// Prints engine list to the console
        /// </summary>
        public static void PrintAll()
        {
            Console.WriteLine("Available Search Engines:");
            engineDictionary.ToList().ForEach(kvp => Console.WriteLine($"   " + kvp.Value));
            Console.WriteLine();
        }
    }
}
