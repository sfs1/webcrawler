﻿using System.Net;
using System.Text.RegularExpressions;

namespace searchengine_test
{
    internal class Program
    {
        private static List<MDLink> AllLinks = new();
        private const string StartLink = "https://en.wikipedia.org/wiki/Main_Page";
        static void Main(string[] args)
        {
            //List<String> Links = Crawl(StartLink);
            //foreach (string link in Links)
            //    AllLinks.Concat(Crawl(link));

            foreach (MDLink a in GetLinks(StartLink))
            {
                Console.WriteLine("{0}: {1}", a.link, string.Join(", ", a.metadata));
            }


            //File.WriteAllLines(@"C:\Users\billy\Desktop\links.txt", Links);
        }

        // Link with metadata e.g. hyperlink text or image alt 
        struct MDLink
        {
            public string link;
            public List<string> metadata;
        }

       /* private static Dictionary<String, List<String>> Crawl(string url)
        {
            Dictionary<String, List<String>> AllLinksWithMetadata = new();
            List<Thread> SpiderThreads = new();

            MDLink[] FirstLinks = GetLinks(url);


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


        }*/

    private static MDLink[] GetLinks(string url)
        {
            string request = HttpGet(url);

            //request.Replace("</a>", String.Empty); // remove closing, might be not needed 

            if (String.IsNullOrWhiteSpace(request)) { return Array.Empty<MDLink>(); }

            List<string> ahrefs = request.Split("<a href=\"").ToList();
            ahrefs.Remove(ahrefs.First());
            List<MDLink> links = new List<MDLink>();

            string host = new Uri(url).Host;
            foreach (string ahref in ahrefs)
            {
                string link;
                // make sure it has a second '"' so we can parse it
                if (!ahref.Contains("\"")) continue;
                link = ahref.Substring(ahref.IndexOf("\"")); // <a href="/example.html"> Example Link </a>
                // make sure its a valid link, ill probably move it into a seperate function later
                if (link.StartsWith("javascript")) continue; // javascript:console.log("this is not a valid link!")
                if (link.StartsWith("#")) continue; // a '#' means jump to a div, e.g. '#mydiv' will jump to 'mydiv'
                if (link.StartsWith("/" + host)) link = link.Substring(0); // if its "/www.wikipedia.com/x", make it "www.wikipedia.com/x"
                if (link.StartsWith("//")) link = link.Substring(1); // if its "//www.wikipedia.com/x", make it "www.wikipedia.com/x"
                if (link.StartsWith("/")) link = host + link; // e.g. "/example.html" -> "https://www.example.com/example.html
                
                if (!httpregex.IsMatch(link)) link = "http://" + link; // make sure it its a valid url, TODO: check if we should use http or https

                // grab the metadata e.g. hyperlink text or image alt 
                // our link = /example.html"> Example Link </a> [blah, blah, blah]. we need to extract the text betweem the <a> tags
                string metadata = link.Split("</a>")[0]; // now we just need to remove the opening a tag
                metadata = metadata.Substring(metadata.IndexOf(">")); // we should have the text inbetween the <a> tags now


                // check if we already have the link, and add only the metadata if we do


                bool FoundMetadata = false;
                foreach (MDLink l in AllLinks)
                {
                    if (AllLinks.Count == 165) 
                        DoNothing();
                    MDLink ll = l;
                    if (l.link != link) continue;
                    ll.metadata.Add(metadata);
                    AllLinks[AllLinks.IndexOf(l)] = ll; // i cant be bothered to test if i actually need this
                    FoundMetadata = true;
                }
                if (FoundMetadata) continue;

                MDLink NewMDLink = new()
                {
                    link = link,
                    metadata = new List<string>(new string[] { metadata} )
                };

                AllLinks.Add(NewMDLink);

            }

            links.Remove(links.First());

            return links.ToArray();

        }
        private static void DoNothing() { }
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