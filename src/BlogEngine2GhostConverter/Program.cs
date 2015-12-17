﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace BlogEngine2GhostConverter
{
    class Program
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        static void Main(string[] args)
        {            
            if (args.Length != 2)
            {
                Console.WriteLine("Invalid arguments!");
                Console.WriteLine("Sample command:BlogEngine2GhostConverter data.xml data.json");
                return;
            }

            string inputFileName = args[0];
            string outputFileName = args[1];

            var convertedData = ConvertInputFile(inputFileName);

            var metaData = new
            {
                exported_on = GetEpochTime(DateTime.UtcNow),
                version = "004"
            };

            var rootData = new
            {
                meta = metaData,
                data = convertedData
            };

            string jsonData =JsonConvert.SerializeObject(rootData, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include
            });

            File.WriteAllText(outputFileName, jsonData);

            Console.WriteLine("Success!");
        }

        private static object ConvertInputFile(string input)
        {
            XNamespace xNamespace = "http://www.blogml.com/2006/09/BlogML";            

            var root = XElement.Load(File.OpenRead(input));            
            var postRoot = root.Element(xNamespace + "posts");
            var posts = postRoot.Elements(xNamespace + "post");
            var convertedPosts = new List<object>();
            var tagsBelongingToPost = new List<object>();

            var categories = root.Element(xNamespace + "categories")
                .Elements(xNamespace + "category")
                .Select((p, s) => new {p, s })
                .ToDictionary(t => (Guid)t.p.Attribute("id"), v => new
                {
                    id = v.s,
                    name = v.p.Element(xNamespace + "title").Value,
                    slug = GetSlugFromTitle(v.p.Element(xNamespace + "title").Value),
                    description = ""
                });

            int postId = 0;            
            foreach (var post in posts)
            {
                postId++;
                convertedPosts.Add(new
                {
                    id = postId,
                    uuid = Guid.NewGuid(),
                    title = post.Element(xNamespace + "title").Value,
                    slug = GetSlugFromTitle(post.Element(xNamespace + "title").Value),
                    markdown = post.Element(xNamespace + "content").Value,
                    html = post.Element(xNamespace + "content").Value,
                    image = (object)null,
                    featured = 0,
                    page = 0,
                    status = "published",
                    language = "en_US",
                    meta_title = (object)null,
                    meta_description = (object)null,
                    author_id = 1,
                    created_at = GetEpochTime((DateTime)post.Attribute("date-created")),
                    created_by = 1,
                    updated_at = GetEpochTime((DateTime)post.Attribute("date-modified")),
                    updated_by = 1,
                    published_at = GetEpochTime((DateTime)post.Attribute("date-created")),
                    published_by = 1
                });

                var categoryTags = post.Element(xNamespace + "categories");
                if (categoryTags != null)
                {
                    var id = postId;
                    var tags = categoryTags
                        .Elements(xNamespace + "category")
                        .Select(x => new { post_id = id, tag_id = categories[(Guid)x.Attribute("ref")].id });

                    tagsBelongingToPost.AddRange(tags);
                }
            }

            return new
            {
                posts = convertedPosts,
                tags = categories.Values.ToList(),
                posts_tags = tagsBelongingToPost
            };
        }

        private static long GetEpochTime(DateTime dateTime)
        {
            return (long)(dateTime - Epoch).TotalMilliseconds;
        }

        private static string GetSlugFromTitle(string url)
        {
            return Regex.Replace(Regex.Replace(url, "[^A-Za-z0-9-]+", "-"), "-{2,}", "-");
        }     
    }
}