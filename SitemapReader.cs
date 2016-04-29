using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Schaeflein.Community.AzureSearch
{
	[Serializable]
	public class SitemapReader : IDisposable
	{
		public SitemapReader()
		{
			Urlset = new List<SitemapItem>();
		}

		public SitemapReader(string sitemapUrl)
		{
			this.SitemapUrl = sitemapUrl;
			Urlset = new List<SitemapItem>();
		}

		public string SitemapUrl { get; set; }

		public List<SitemapItem> Urlset { get; set; }

		public List<SitemapItem> ReadSitemap()
		{
			if (String.IsNullOrEmpty(SitemapUrl))
				throw new ArgumentException("The sitemap url must be set");

			using (XmlReader reader = XmlReader.Create(SitemapUrl))
			{
				XmlDocument doc = new XmlDocument();

				XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
				nsmgr.AddNamespace("sitemap", "http://www.sitemaps.org/schemas/sitemap/0.9");
				nsmgr.AddNamespace("image", "http://www.google.com/schemas/sitemap-image/1.1");


				doc.Load(reader);

				ParseItems(doc, nsmgr);

				return Urlset;
			}
		}

		/// <summary>
		/// Parses the xml document in order to retrieve the RSS items.
		/// </summary>
		private void ParseItems(XmlDocument doc, XmlNamespaceManager nsmgr)
		{
			Urlset.Clear();
			XmlNodeList nodes = doc.SelectNodes("sitemap:urlset/sitemap:url", nsmgr);

			foreach (XmlNode node in nodes)
			{
				SitemapItem item = new SitemapItem();
				ParseElement(node, nsmgr, "sitemap:loc", ref item.Location);

				string lastmod = String.Empty;
				ParseElement(node, nsmgr, "sitemap:lastmod", ref lastmod);
				DateTime.TryParse(lastmod, out item.LastModified);

				ParseElement(node, nsmgr, "sitemap:changefreq", ref item.ChangeFrequency);

				string priority = String.Empty;
				ParseElement(node, nsmgr, "sitemap:priority", ref priority);
				Decimal.TryParse(priority, out item.Priority);

				string imageLoc = String.Empty;
				ParseElement(node, nsmgr, "image:image/image:loc", ref imageLoc);
				if (!String.IsNullOrEmpty(imageLoc))
				{
					item.Image = new SitemapImage();
					item.Image.Location = imageLoc;

					ParseElement(node, nsmgr, "image:image/image:caption", ref item.Image.Caption);
				}

				Urlset.Add(item);
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

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}

	[Serializable]
	public struct SitemapItem
	{
		public string Location;
		public DateTime LastModified;
		public string ChangeFrequency;
		public decimal Priority;
		public SitemapImage Image;
	}

	[Serializable]
	public struct SitemapImage
	{
		public string Location;
		public string Caption;
	}
}
