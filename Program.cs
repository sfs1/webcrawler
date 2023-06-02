using System.Net;
using System.Text.RegularExpressions;

namespace searchengine_test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<String> Links = Spider("https://en.wikipedia.org/wiki/Main_Page");
            int depth = 5;
            for (int i = 0; i < depth; i++)
            {
                foreach (string link in Links)
                    Links.Concat(Spider(link));
            }




            File.WriteAllLines(@"C:\Users\billy\Desktop\links.txt", Links);
        }

        private static List<String> Spider(string url)
        {
            List<String> AllLinks = new();
            List<Thread> SpiderThreads = new();

            string[] FirstLinks = GetLinks(url);


            foreach (string Link in FirstLinks)
            {
                SpiderThreads.Add(new Thread(() =>
                {
                    foreach (string a in GetLinks(Link))
                    {
                        Console.WriteLine(a);
                        AllLinks.Add(a);
                    }
                }));
                AllLinks.Add(Link);
            }

            foreach (Thread t in SpiderThreads)
                t.Start();
            foreach (Thread t in SpiderThreads)
            {
                while (true)
                {
                    if (t.Join(100)) break;
                }
            }
            return AllLinks;


        }

    private static string[] GetLinks(string url)
        {
            string request = HttpGet(url);

            request.Replace("</a>", String.Empty); // remove closing, might be not needed 

            if (String.IsNullOrWhiteSpace(request)) { return Array.Empty<string>(); }

            string[] ahrefs = request.Split("<a href=\"");
            List<string> links = new List<String>();

            string host = new Uri(url).Host;
            foreach (string ahref in ahrefs)
            {
                string thing;
                //if (!ahref.EndsWith("\"")) continue;
                if (ahref.StartsWith("javascript")) continue;
                if (!ahref.Contains("\"")) continue;
                thing = ahref.Substring(0, ahref.IndexOf("\""));
                if (thing.StartsWith("//")) thing = thing.Substring(1);
                if (thing.StartsWith("#")) continue;
                if (thing.StartsWith("/" + host)) thing = thing.Substring(0);
                if (thing.StartsWith("/")) thing = host + thing;
                if (!httpregex.IsMatch(thing)) thing = "http://" + thing;
                links.Add(thing);
            }

            links.Remove(links.First());

            return links.ToArray();

        }

        private static Regex httpregex = new Regex("^http(|s):\\/\\/", RegexOptions.IgnoreCase);

        private static string HttpGet(string url)
        {
            string link = url;

            if (!httpregex.IsMatch(link)) link = "http://" + link;  

            HttpWebRequest req = (HttpWebRequest)WebRequest.CreateHttp(new Uri(link));
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/113.0.1774.57";

            try
            {
                return new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();
            } catch(Exception)
            {
                return "";
            }
        }
    }
}