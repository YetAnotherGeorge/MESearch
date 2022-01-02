using System;
using System.Collections.Generic;
using IHLib.Extensions.Generic;
namespace MESearch
{

    class Program
    {
        //Usage example
        //-setBrowserPath "C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe"
        //-add {"Google", "g", "https://www.google.com/search?q=%s"}
        //-add {"DuckDuckGo", "d", "https://duckduckgo.com/?q=%s&t=brave&ia=web"}
        //-add { "Yandex", "ydx", "https://yandex.com/search/?text=%s"}
        //-add { "Youtube", "yt", "https://www.youtube.com/results?search_query=%s"}
        //-add { "Twitter", "t", "https://twitter.com/search?q=%s&src=typed_query&f=top"}
        static void Main(string[] args)
        {
            MultiEngine.Init();
            MultiEngine.PrintAll();

            while (true)
            {
                string input = Console.ReadLine();
                switch (input)
                {
                    case string when input.StartsWith('-'):
                        {
                            string s = input.Substring(1);
                            switch (s)
                            {
                                case string when s == "list":
                                    MultiEngine.PrintAll();
                                    break;
                                case string when s.StartsWith("add"):
                                    MultiEngine.AddEngine(new SearchEngine(s.Substring(3).Trim()));
                                    break;
                                case string when s.StartsWith("rem"):
                                    MultiEngine.RemEngine(s.Substring(3).Trim());
                                    break;
                                case string when s.StartsWith("setBrowserPath"):
                                    {
                                        string ts = s.Substring(14).Trim();
                                        if (ts.Contains('\"'))
                                            ts = ts.Substring(1, ts.Length - 2);
                                        MultiEngine.SetBrowserPath(ts);
                                    }
                                    break;
                                default:
                                    throw new Exception("Invalid command.");
                            }
                        }
                        break;
                    default:
                        MultiEngine.PerformSearch(input);
                        break;
                }
            }
        }
    }
}
