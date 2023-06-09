﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

namespace searchengine_test
{
    internal class Program
    {
        private static List<MDLink> AllLinks = new List<MDLink>();
        private const string StartLink = "https://en.wikipedia.org/wiki/Main_Page";
        static void Main(string[] args)
        {
            //List<String> Links = Crawl(StartLink);
            //foreach (string link in Links)
            //    AllLinks.Concat(Crawl(link));

            MDLink[] links = GetLinks(StartLink);
            Console.WriteLine(links.Length);

            /*foreach (MDLink a in links)
            {
                //Console.WriteLine("{0}: {1}", a.link, string.Join(", ", a.metadata));
                Console.WriteLine("Got link: {0}, {1}", a.link, string.Join(", ", a.metadata)); //DEBUG
                foreach(MDLink b in GetLinks(a.link))
                    Console.WriteLine("Got link: {0}, {1}", b.link, string.Join(", ", b.metadata)); //DEBUG

            }*/


            AllLinks = Crawl(StartLink, 2).ToList();

            foreach(MDLink link in AllLinks)
            {
                Console.WriteLine("{0}, {1}", link.link, string.Join(", ", link.metadata));
            }
            Console.WriteLine("{0} total links.", AllLinks.Count);
            //File.WriteAllLines(@"C:\Users\billy\Desktop\links.txt", AllLinks);
        }
        private static MDLink[] Crawl(string url, int depth = 0)
        {
            Console.WriteLine("Crawling url={0}, depth={1}", url, depth);
            List<MDLink> outlinks = GetLinks(url).ToList();
            List<MDLink[]> CombineLinks = new List<MDLink[]>();
            List<Thread> CrawlThreads = new List<Thread>();
            if (depth == 0) return outlinks.ToArray();

            foreach (MDLink link in outlinks)
            {
                // TODO: Multithreading
                // Recursively crawl each link and decrease the depth so we dont end up in an infinite loop.
                    CombineLinks.Add(Crawl(link.link, depth - 1));
            }
            // Start all the threads, then wait for each of them to be finished.



            // Now we need to add all CombineLinks to outlinks, then return it.

            foreach (MDLink[] Links in CombineLinks)
            {
                outlinks = outlinks.Concat(Links).ToList();
                Console.WriteLine("{0}, {1}", outlinks.Count, Links.Length);
            }

            return outlinks.ToArray();

        }

        // Link with metadata e.g. hyperlink text or image alt 
        struct MDLink
        {
            public string link;
            public List<string> metadata;
        }

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
                // TODO: handle extra tags, we might have a <span> or something like that inbetween the <a>
                if (!ahref.Contains("\"")) continue;
                link = ahref.Substring(0,ahref.IndexOf("\"")); // <a href="/example.html"> Example Link </a>
                // make sure its a valid link
                if (!IsValidUrl(link)) continue;

                if (link.StartsWith("/" + host)) link = link.Substring(0); // if its "/www.wikipedia.com/x", make it "www.wikipedia.com/x"
                if (link.StartsWith("//")) link = link.Substring(1); // if its "//www.wikipedia.com/x", make it "www.wikipedia.com/x"
                if (link.StartsWith("/")) link = host + link; // e.g. "/example.html" -> "https://www.example.com/example.html
                
                

                if (!httpregex.IsMatch(link)) link = "http://" + link; // make sure it its a valid url, TODO: check if we should use http or https

                //link = link.Substring(0, link.IndexOf("</a>"));

                // grab the metadata e.g. hyperlink text or image alt 
                // TODO: handle extra tags, we might have a <span> or something like that inbetween the <a>
                string metadata = ahref.Substring(0, ahref.IndexOf("</a>")).Substring(ahref.IndexOf(">") + 1);
                // returns whatever is inside the <a> tags (including any other tags though) -^

                if (metadata.StartsWith("<img")) continue; // skip images (for now)

                metadata = RemoveTags(metadata);

                if (metadata.Length > 30) continue; // if its this long then we most likely have html code, this needs re-doing but it works for now
                // check if we already have the link, and add only the metadata if we do


                bool FoundMetadata = false;
                foreach (MDLink l in links)
                {

                    MDLink ll = l;
                    if (l.link != link) continue;
                    ll.metadata.Add(metadata);
                    links[links.IndexOf(l)] = ll; // i cant be bothered to test if i actually need this
                    FoundMetadata = true;
                    break;
                }
                if (FoundMetadata) continue; // because we dont need to add another entry

                MDLink NewMDLink = new MDLink()
                {
                    link = link,
                    metadata = new List<string>(new string[] { metadata} )
                };
                
                links.Add(NewMDLink);


            }

            
            return links.ToArray();

        }
        
        private static Regex httpregex = new Regex("^http(|s):\\/\\/", RegexOptions.IgnoreCase);
        
        private static bool IsValidUrl(string url)
        {
            
            if (url.StartsWith("javascript")) return false; // javascript:console.log("this is not a valid link!")
            if (url.StartsWith("#")) return false ; // a '#' means jump to a div, e.g. '#mydiv' will jump to 'mydiv'
            return true;
        }

        private static string[] tags = { "span", "div", "i", "b", "li", "h1", "h2", "h3", "center" };
        private static string RemoveTags(string html)
        {
            string newhtml = html;
            // make sure that there are any tags at all
            if (!html.Contains("<") && !html.Contains(">")) return html;

            foreach(string tag in tags)
            {
                // check if we need to remove this tag
                if (!newhtml.Contains($"<{tag}>")) continue;
                
                int StartIndex = newhtml.IndexOf("<" +tag);
                // remove the opening tag
                IEnumerable<char> remove = newhtml.Take(new Range(StartIndex, newhtml.IndexOf(">", StartIndex) + 1));
                newhtml = newhtml.Replace(new String(remove.ToArray()), string.Empty); // im genuinly surprised this even works
                newhtml = newhtml.Replace($"</{tag}>", string.Empty);
                


            }

            return newhtml;
        }

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