using System;
using System.Linq;
using System.Threading;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;

namespace Schaeflein.Community.AzureSearch
{
	class Program
	{
		// This sample shows how to delete, create, upload documents and query an index
		static void Main(string[] args)
		{
			// Put your search service name here. This is the hostname portion of your service URL.
			// For example, if your service URL is https://myservice.search.windows.net, then your
			// service name is myservice.
			string searchServiceName = "=[your service name here]";

			string apiKey = "[your apiKey here]";


			SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));

			//Console.WriteLine("{0}", "Creating index...\n");
			//CreateIndex(serviceClient);

			SearchIndexClient indexClient = serviceClient.Indexes.GetClient("posts");

			Console.WriteLine("{0}", "Uploading documents...\n");
			UploadDocuments(indexClient);
			//IndexFromSitemap(indexClient);
			IndexFromRss(indexClient);


			Console.WriteLine("{0}", "Complete.  Press any key to end application...\n");
			Console.ReadKey();
		}

		private static void IndexFromSitemap(SearchIndexClient indexClient)
		{
			List<GhostPost> documents = new List<GhostPost>();
			HtmlDocument html = new HtmlDocument();

			SitemapReader smReader = new SitemapReader("http://www.schaeflein.net/sitemap-pages.xml");
			var pages = smReader.ReadSitemap();

			foreach (var page in pages)
			{
				string responseContent = String.Empty;
				var request = (HttpWebRequest)HttpWebRequest.Create(page.Location);
				request.Method = "GET";
				var response = request.GetResponse();
				using (var reader = new StreamReader(response.GetResponseStream()))
				{
					//responseContent = reader.ReadToEnd();
					html.Load(reader);
				}

				
				string content = ExtractViewableTextCleaned(html.GetElementbyId("page-content"));

				documents.Add(new GhostPost
				{
					//Guid = page.Id,
					//Title = page.Title,
					Content = content,
					//Description = page.Description,
					Link = page.Location,
					//PubDate = page.Date
				});
			}

			try
			{
				var batch = IndexBatch.MergeOrUpload<GhostPost>(documents);
				indexClient.Documents.Index(batch);
			}
			catch (IndexBatchException e)
			{
				// Sometimes when your Search service is under load, indexing will fail for some of the documents in
				// the batch. Depending on your application, you can take compensating actions like delaying and
				// retrying. For this simple demo, we just log the failed document keys and continue.
				Console.WriteLine(
						"Failed to index some of the documents: {0}",
						String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
			}
		}

		private static void DeleteHotelsIndexIfExists(SearchServiceClient serviceClient)
		{
			if (serviceClient.Indexes.Exists("hotels"))
			{
				serviceClient.Indexes.Delete("hotels");
			}
		}

		private static void CreateIndex(SearchServiceClient serviceClient)
		{
			var definition = new Index()
			{
				Name = "posts",
				Fields = new[]
					{
						new Field("guid", DataType.String)                   { IsKey = true },
						new Field("title", DataType.String)                  { IsSearchable = true, IsFilterable = true, IsRetrievable=true },
						new Field("content", DataType.String)                  { IsSearchable = true },
						new Field("description", DataType.String)             { IsRetrievable=true },
						new Field("link", DataType.String)                      { IsRetrievable=true },
						new Field("category", DataType.Collection(DataType.String))     { IsSearchable = true, IsFilterable = true, IsFacetable = true },
						new Field("pubDate", DataType.DateTimeOffset)    { IsFilterable = true, IsRetrievable=true, IsSortable = true, IsFacetable = true },
					}
			};

			serviceClient.Indexes.Create(definition);
		}

		private static void UploadDocuments(SearchIndexClient indexClient)
		{
			List<GhostPost> documents = new List<GhostPost>();

			dynamic data = JObject.Parse(File.ReadAllText(@"schaeflein-consulting.ghost.2016-04-29.json"));
			foreach (var item in data.db[0].data.posts)
			{
				HtmlDocument html = new HtmlDocument();
				html.LoadHtml((string)item.html);
				string content = ExtractViewableTextCleaned(html.DocumentNode);

				documents.Add(new GhostPost
				{
					Guid = item.uuid,
					Title = item.title,
					Content = content,
					Description = "",
					Link = String.Format("http://www.schaeflein.net/{0}", item.slug),
					PubDate = item.published_at
				});
			}

			try
			{
				//var batch = IndexBatch.MergeOrUpload<GhostPost>(documents);
				var batch = IndexBatch.Delete("guid", documents.Select(d => d.Guid));
				indexClient.Documents.Index(batch);
			}
			catch (IndexBatchException e)
			{
				// Sometimes when your Search service is under load, indexing will fail for some of the documents in
				// the batch. Depending on your application, you can take compensating actions like delaying and
				// retrying. For this simple demo, we just log the failed document keys and continue.
				Console.WriteLine(
						"Failed to index some of the documents: {0}",
						String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
			}

			// Wait a while for indexing to complete.
			Thread.Sleep(2000);

		}

		private static void IndexFromRss(SearchIndexClient indexClient)
		{
			List<GhostPost> documents = new List<GhostPost>();

			RssReader rssReader = new RssReader("http://www.schaeflein.net/rss");
			var posts = rssReader.Execute();

			foreach (var post in posts)
			{
				HtmlDocument html = new HtmlDocument();
				html.LoadHtml(post.Content);
				string content = ExtractViewableTextCleaned(html.DocumentNode);

				documents.Add(new GhostPost
				{
					Guid = post.Id,
					Title = post.Title,
					Content = content,
					Description = post.Description,
					Link = post.Link,
					PubDate = post.Date
				});
			}

			try
			{
				var batch = IndexBatch.MergeOrUpload<GhostPost>(documents);
				indexClient.Documents.Index(batch);
			}
			catch (IndexBatchException e)
			{
				// Sometimes when your Search service is under load, indexing will fail for some of the documents in
				// the batch. Depending on your application, you can take compensating actions like delaying and
				// retrying. For this simple demo, we just log the failed document keys and continue.
				Console.WriteLine(
						"Failed to index some of the documents: {0}",
						String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
			}

			// Wait a while for indexing to complete.
			Thread.Sleep(2000);
		}

		private static void SearchDocuments(SearchIndexClient indexClient, string searchText, string filter = null)
		{
			//// Execute search based on search text and optional filter
			//var sp = new SearchParameters();

			//if (!String.IsNullOrEmpty(filter))
			//{
			//	sp.Filter = filter;
			//}

			//DocumentSearchResult<Hotel> response = indexClient.Documents.Search<Hotel>(searchText, sp);
			//foreach (SearchResult<Hotel> result in response.Results)
			//{
			//	Console.WriteLine(result.Document);
			//}
		}



		private static Regex _removeRepeatedWhitespaceRegex = new Regex(@"(\s|\n|\r){2,}", RegexOptions.Singleline | RegexOptions.IgnoreCase);
		public static string ExtractViewableTextCleaned(HtmlNode node)
		{
			string textWithLotsOfWhiteSpaces = ExtractViewableText(node);
			return _removeRepeatedWhitespaceRegex.Replace(textWithLotsOfWhiteSpaces, " ");
		}

		public static string ExtractViewableText(HtmlNode node)
		{
			StringBuilder sb = new StringBuilder();
			ExtractViewableTextHelper(sb, node);
			return sb.ToString();
		}

		private static void ExtractViewableTextHelper(StringBuilder sb, HtmlNode node)
		{
			if (node.Name != "script" && node.Name != "style" && node.Name != "pre")
			{
				if (node.NodeType == HtmlNodeType.Text)
				{
					AppendNodeText(sb, node);
				}

				foreach (HtmlNode child in node.ChildNodes)
				{
					ExtractViewableTextHelper(sb, child);
				}
			}
		}

		private static void AppendNodeText(StringBuilder sb, HtmlNode node)
		{
			string text = ((HtmlTextNode)node).Text;
			if (string.IsNullOrWhiteSpace(text) == false)
			{
				sb.Append(text);

				// If the last char isn't a white-space, add a white space
				// otherwise words will be added ontop of each other when they're only separated by
				// tags
				if (text.EndsWith("\t") || text.EndsWith("\n") || text.EndsWith(" ") || text.EndsWith("\r"))
				{
					// We're good!
				}
				else
				{
					sb.Append(" ");
				}
			}
		}

	}
}