using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace ReleaseManager
{
    public class ReleaseItem : IComparable<ReleaseItem>
    {
        public string releaseId { get; set; }
        public string id { get; set; }
        public string URI { get; set; }
        public string WEBDAV_URL { get; set; }
        public string TITLE { get; set; }
        public bool ISSHARED { get; set; }
        public int SUBTYPE { get; set; }
        public DateTime? added { get; set; }
        public List<Release> definitelyConflictsWith { get; set; }
        public List<Release> possiblyConflictsWith { get; set; }

        public int CompareTo(ReleaseItem r)
        {
            return string.Compare(TITLE, r.TITLE);
        }

        public XmlNode ToXml(XmlDocument db)
        {
            XmlElement newItem = db.CreateElement("item");

            XmlAttribute uriAttribute = db.CreateAttribute("uri");
            uriAttribute.Value = URI;

            XmlAttribute releaseAttribute = db.CreateAttribute("release");
            releaseAttribute.Value = releaseId;

            newItem.Attributes.Append(uriAttribute);
            newItem.Attributes.Append(releaseAttribute);

            //Create webdav node
            XmlNode webdavNode = db.CreateNode(XmlNodeType.Element, "webdav_url", "");
            webdavNode.InnerText = WEBDAV_URL;
            newItem.AppendChild(webdavNode);

            //Create title node
            XmlNode titleNode = db.CreateNode(XmlNodeType.Element, "title", "");
            titleNode.InnerText = TITLE;
            newItem.AppendChild(titleNode);

            //Create isShared node
            XmlNode isSharedNode = db.CreateNode(XmlNodeType.Element, "is_shared", "");
            isSharedNode.InnerText = ISSHARED.ToString();
            newItem.AppendChild(isSharedNode);

            //Create subType node
            XmlNode subTypeNode = db.CreateNode(XmlNodeType.Element, "subtype", "");
            subTypeNode.InnerText = SUBTYPE.ToString();
            newItem.AppendChild(subTypeNode);

            var dateAdded = db.CreateAttribute("dateAdded");
            dateAdded.Value = Utilities.getFancyDateTime(this.added);
            newItem.Attributes.Append(dateAdded);

            if (id == null)
            {
                id = System.Guid.NewGuid().ToString();
            }
            
            XmlAttribute idAttr = db.CreateAttribute("id");
            idAttr.Value = id;
            newItem.Attributes.Append(idAttr);

            return newItem;
        }
    }
}