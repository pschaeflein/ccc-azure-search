#region Using

using System;
using System.Xml;
using System.Collections.ObjectModel;

#endregion

namespace Schaeflein.Community.AzureSearch
{
	/// <summary>
	/// Parses remote RSS 2.0 feeds.
	/// </summary>
	[Serializable]
	public class RssReader : IDisposable
	{

		#region Constructors

		public RssReader()
		{ }

		public RssReader(string feedUrl)
		{
			_FeedUrl = feedUrl;
		}

		#endregion

		#region Properties

		private string _FeedUrl;
		/// <summary>
		/// Gets or sets the URL of the RSS feed to parse.
		/// </summary>
		public string FeedUrl
		{
			get { return _FeedUrl; }
			set { _FeedUrl = value; }
		}

		private Collection<RssItem> _Items = new Collection<RssItem>();
		/// <summary>
		/// Gets all the items in the RSS feed.
		/// </summary>
		public Collection<RssItem> Items
		{
			get { return _Items; }
		}

		private string _Title;
		/// <summary>
		/// Gets the title of the RSS feed.
		/// </summary>
		public string Title
		{
			get { return _Title; }
		}

		private string _Description;
		/// <summary>
		/// Gets the description of the RSS feed.
		/// </summary>
		public string Description
		{
			get { return _Description; }
		}

		private DateTime _LastUpdated;
		/// <summary>
		/// Gets the date and time of the retrievel and
		/// parsing of the remote RSS feed.
		/// </summary>
		public DateTime LastUpdated
		{
			get { return _LastUpdated; }
		}

		private TimeSpan _UpdateFrequency;
		/// <summary>
		/// Gets the time before the feed get's silently updated.
		/// Is TimeSpan.Zero unless the CreateAndCache method has been used.
		/// </summary>
		public TimeSpan UpdateFrequency
		{
			get { return _UpdateFrequency; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Retrieves the remote RSS feed and parses it.
		/// </summary>
		/// <exception cref="System.Net.WebException" />
		public Collection<RssItem> Execute()
		{
			if (String.IsNullOrEmpty(FeedUrl))
				throw new ArgumentException("The feed url must be set");

			using (XmlReader reader = XmlReader.Create(FeedUrl))
			{
				XmlDocument doc = new XmlDocument();

				XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
				nsmgr.AddNamespace("content", "http://purl.org/rss/1.0/modules/content/");
				nsmgr.AddNamespace("atom", "http://www.w3.org/2005/Atom");
				nsmgr.AddNamespace("media", "http://search.yahoo.com/mrss/");




				doc.Load(reader);

				ParseElement(doc.SelectSingleNode("//channel"), nsmgr, "title", ref _Title);
				ParseElement(doc.SelectSingleNode("//channel"), nsmgr, "description", ref _Description);
				ParseItems(doc, nsmgr);

				_LastUpdated = DateTime.Now;

				return _Items;
			}
		}

		/// <summary>
		/// Parses the xml document in order to retrieve the RSS items.
		/// </summary>
		private void ParseItems(XmlDocument doc, XmlNamespaceManager nsmgr)
		{
			_Items.Clear();
			XmlNodeList nodes = doc.SelectNodes("rss/channel/item");

			foreach (XmlNode node in nodes)
			{
				RssItem item = new RssItem();
				ParseElement(node, nsmgr, "guid", ref item.Id);
				ParseElement(node, nsmgr, "title", ref item.Title);
				ParseElement(node, nsmgr, "description", ref item.Description);
				ParseElement(node, nsmgr, "link", ref item.Link);
				ParseElement(node, nsmgr, "content:encoded", ref item.Content);

				string date = null;
				ParseElement(node, nsmgr, "pubDate", ref date);
				DateTime.TryParse(date, out item.Date);

				_Items.Add(item);
			}
		}

		/// <summary>
		/// Parses the XmlNode with the specified XPath query 
		/// and assigns the value to the property parameter.
		/// </summary>
		private void ParseElement(XmlNode parent, XmlNamespaceManager nsmgr, string xPath, ref string property)
		{
			XmlNode node = parent.SelectSingleNode(xPath, nsmgr);
			if (node != null)
				property = node.InnerText;
			else
				property = "Unresolvable";
		}

		#endregion

		#region IDisposable Members

		private bool _IsDisposed;

		/// <summary>
		/// Performs the disposal.
		/// </summary>
		private void Dispose(bool disposing)
		{
			if (disposing && !_IsDisposed)
			{
				_Items.Clear();
				_FeedUrl = null;
				_Title = null;
				_Description = null;
			}

			_IsDisposed = true;
		}

		/// <summary>
		/// Releases the object to the garbage collector
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

	}

	#region RssItem struct

	/// <summary>
	/// Represents a RSS feed item.
	/// </summary>
	[Serializable]
	public struct RssItem
	{
		/// <summary>
		/// The publishing date.
		/// </summary>
		public DateTime Date;

		/// <summary>
		/// The title of the item.
		/// </summary>
		public string Title;

		/// <summary>
		/// A description of the content or the content itself.
		/// </summary>
		public string Description;

		/// <summary>
		/// The link to the webpage where the item was published.
		/// </summary>
		public string Link;

		/// <summary>
		/// The encoded content of the item
		/// </summary>
		public string Content;

		/// <summary>
		/// The unique identifier of the item
		/// </summary>
		public string Id;
	}

	#endregion
}
