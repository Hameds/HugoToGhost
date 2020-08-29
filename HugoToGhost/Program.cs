using Epoch.net;
using MobileDocRenderer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HugoToGhost
{
    class Program
    {
        
        static void Main(string[] args)
        {
            Console.ResetColor();
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine($"Hi 👋 {Environment.NewLine}" +
                $"Welcome to 'Hugo markdown' to 'Ghost cms json' converter {Environment.NewLine}");
            Console.Write("Please enter hugo markdown posts path (e.g. c:/hugo/content/post) ");

            var hugoPostsDirectory = Console.ReadLine();

            if (!Directory.Exists(hugoPostsDirectory))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"path '{hugoPostsDirectory}' not found");
                Console.ResetColor();
                return;
            }

            ConvertHugoPostsToGhostFormat(hugoPostsDirectory);
        }



        #region Helper Methods

        private static void ConvertHugoPostsToGhostFormat(string hugoPostsDirectory)
        {
            var hugoPostFiles = Directory.EnumerateFiles(hugoPostsDirectory,"*.md");
            if (!hugoPostFiles.Any())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"No hugo markdown post file found in '{hugoPostsDirectory}'");
                Console.ResetColor();
                return;
            }
            Console.WriteLine($"Converting {hugoPostFiles.Count()} hugo posts");

            List<GhostPost> ghostPosts = GetHugoPosts(hugoPostFiles);

            JObject ghost = GetGhostJson(ghostPosts);

            var ghostJsonFilePath = Path.Combine(hugoPostsDirectory, "ghost.json");
            File.WriteAllText(ghostJsonFilePath, ghost.ToString());

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Successfully converted {ghostPosts.Count()}. You can find json file here: '{ghostJsonFilePath}'");
            Console.ResetColor();

            Console.WriteLine($"Have a nice day. ❤ from Iran");
        }

        private static JObject GetGhostJson(List<GhostPost> ghostPosts)
        {
            return JObject.FromObject(new
            {
                meta = new
                {
                    exported_on = DateTime.Now.ToLongEpochTimestamp(),
                    version = "3.31.2"
                },
                data = new
                {
                    posts =
                                    from p in ghostPosts
                                    orderby p.Title
                                    select new
                                    {
                                        title = p.Title,
                                        status = "published",
                                        mobiledoc = GetMobileDoc(p.Body),
                                        published_at = p.PublishDate.ToLongEpochTimestamp()
                                    }
                }
            });
        }

        private static List<GhostPost> GetHugoPosts(IEnumerable<string> hugoPostFiles)
        {
            var ghostPosts = new List<GhostPost>();

            foreach (var hugoPostFilePath in hugoPostFiles)
            {
                try
                {
                    var hugoPostFileContent = File.ReadAllText(hugoPostFilePath);

                    var hugoPostContent = hugoPostFileContent.Split("---");

                    if (!hugoPostContent.Any())
                    {
                        Console.WriteLine($"No hugo file match in {hugoPostFilePath}");
                    }
                    else
                    {
                        var hugoPostTitle = ExtractFromContent(hugoPostContent[1], "title: \"(.+)\"");
                        var hugoPostDate = ExtractFromContent(hugoPostContent[1], "date: (.+)");
                        var hugoPostMarkdown = hugoPostContent[2].Trim();

                        var ghostPost = new GhostPost
                        {
                            Title = hugoPostTitle.Replace("title:", string.Empty).Replace("\"", string.Empty).Trim(),
                            PublishDate = Convert.ToDateTime(hugoPostDate.Replace("date:", string.Empty).Trim()),
                            Body = hugoPostMarkdown
                        };


                        ghostPosts.Add(ghostPost);

                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Error converting hugo post from {hugoPostFilePath}: {ex.Message}");
                }

            }

            return ghostPosts;
        }

        private static string GetMobileDoc(string markdownContent)
        {
            var document = new MobileDocBuilder()
                .WithCardSection(x => x.WithCardIndex(0))
             .WithCard(section => section
             .WithName("markdown")
             .WithPayload(JObject.FromObject(new
             {
                 markdown = markdownContent
             }))

             );

            return MobileDocSerializer.Serialize(document.Build()).ToString();
        }

        private static string ExtractFromContent(string text, string pattern)
        {
            return Regex.Match(text, pattern).Value;
        }

        #endregion

        class GhostPost
        {
            public string Title { get; set; }

            public DateTime PublishDate { get; set; }

            public string Body { get; set; }

        }
    }
}
