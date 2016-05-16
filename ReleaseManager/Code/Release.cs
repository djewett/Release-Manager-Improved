using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace ReleaseManager
{
    public class Release : IComparable<Release>
    {
        public string id { get; set; }
        public string title { get; set; }
        public DateTime? added { get; set; }
        public bool isDeleted() {
            return finalized.HasValue;
        }
        public DateTime? finalized { get; set; }
        public string note { get; set; }
        public string deletedBy { get; set; }
        public string addedBy { get; set; }

        public XmlNode ToXml(XmlDocument db)
        {
            XmlNode releaseNode = db.CreateElement("release");

            var idNode = db.CreateAttribute("id");
            if (this.id == null)
            {
                this.id = System.Guid.NewGuid().ToString();                
            }
            idNode.Value = this.id;
            releaseNode.Attributes.Append(idNode);

            var noteNode = db.CreateNode(XmlNodeType.Element, "note", "");
            noteNode.InnerText = this.note;
            releaseNode.AppendChild(noteNode);

            var dateAdded = db.CreateAttribute("dateAdded");
            dateAdded.Value = Utilities.getFancyDateTime(this.added);
            releaseNode.Attributes.Append(dateAdded);

            var titleNode = db.CreateNode(XmlNodeType.Element, "title", "");
            titleNode.InnerText = this.title;
            releaseNode.AppendChild(titleNode);

            var addedByNode = db.CreateNode(XmlNodeType.Element, "addedBy", "");
            if (String.IsNullOrEmpty(this.addedBy))
            {
                this.addedBy = Utilities.getCurrentUser();
            }
            addedByNode.InnerText = this.addedBy;
            releaseNode.AppendChild(addedByNode);

            var deletedByNode = db.CreateNode(XmlNodeType.Element, "deletedBy", "");
            deletedByNode.InnerText = this.deletedBy;
            releaseNode.AppendChild(deletedByNode);

            var finalizedAttr = db.CreateAttribute("dateFinalized");
            if (this.finalized.HasValue)
            {
                finalizedAttr.Value = Utilities.getFancyDateTime(this.finalized);
            }
            releaseNode.Attributes.Append(finalizedAttr);

            return releaseNode;
        }

        public XmlNode itemsToXml(XmlDocument db)
        {
            var itemsNode = db.CreateElement("items");
            foreach (var item in this.items)
            {
                itemsNode.AppendChild(item.ToXml(db));
            }
            return itemsNode;
        }

        public List<ReleaseItem> items
        {
            get;
            set;
        }

        public int CompareTo(Release r)
        {
            return String.Compare(title, r.title);
        }

        internal bool containsItem(string uri)
        {
            foreach (var item in items)
            {
                if (item.URI.Equals(uri))
                {
                    return true;
                }
            }
            return false;
        }
    }
}