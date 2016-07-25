using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using Tridion.ContentManager;
using Tridion.ContentManager.CoreService.Client;


namespace ReleaseManager
{
    public class ReleaseManagerRepository
    {

        /// <summary>
        /// Private constants
        /// </summary>
        #region Private Constants
        private static string _APPLICATION_NAME = "SDL Tridion Release Manager";
        private static string _EVENTLOG_NAME = "SDL Tridion Releaes Manager";
        private static string CONFIG_SETTINGS_FILE = @"\\Configuration\\releaseManagerConfig.xml";
        private static string RELEASE_MANAGER_DB = @"\\App_Data\\releaseData.xml";
        private static string TEMP_FOLDER = @"\temp\";
        private static string SCHEMA_NAMESPACE = "http://www.tridion.com/ContentManager/5.2/ImportExport";

        private static System.Web.HttpServerUtility appServer;
        private static XmlDocument db;
        private static HttpRequest req;
        private static XmlDocument importExportDoc = new XmlDocument();
        private static XmlNamespaceManager nm = new XmlNamespaceManager(importExportDoc.NameTable);

        private static SessionAwareCoreServiceClient tridionClient = null;
        #endregion

        public ReleaseManagerRepository(System.Web.HttpServerUtility server, HttpRequest request)
        {
            appServer = server;
            req = request;
            db = getReleaseManagerDB();

            //Namespacemanager
            nm.AddNamespace("cp", SCHEMA_NAMESPACE);
        }

        #region Public Methods

        /// <summary>
        /// Deletes a release from the DB and writes it to a history file
        /// </summary>
        /// <param name="release">Release to finalize</param>
        public void finalizeRelease(string releaseId)
        {
            var release = getRelease(releaseId);

            release.deletedBy = Utilities.getCurrentUser();
            release.finalized = DateTime.Now;
            saveRelease(release);
        }

        public SessionAwareCoreServiceClient getCoreServiceClient()
        {
            var client = new SessionAwareCoreServiceClient(getCoreServiceEndpointName());
            client.Impersonate(HttpContext.Current.User.Identity.Name);
            if (client.State.Equals(CommunicationState.Faulted)) { client.Open(); }
            return client;
        }

        //public SessionAwareCoreServiceClient getStaticCoreServiceClient()
        //{
        //    if (null == tridionClient)
        //    {
        //        tridionClient = new SessionAwareCoreServiceClient(getCoreServiceEndpointName());
        //        tridionClient.Impersonate(HttpContext.Current.User.Identity.Name);
        //        if (tridionClient.State.Equals(CommunicationState.Faulted)) { tridionClient.Open(); }
        //        return tridionClient;
        //    }
        //    else
        //    {
        //        return tridionClient;
        //    }
        //}

        public string getCoreServiceEndpointName()
        {
            return System.Configuration.ConfigurationManager.AppSettings["coreServiceEndpoint"];
        }

        public void importXml()
        {
            var client = getCoreServiceClient();
        }

        /// <summary>
        /// Gets the releases an item is currently in
        /// </summary>
        /// <param name="uri">Uri of the item</param>
        /// <returns></returns>
        public List<Release> getReleasesItemIsIn(string uri)
        {
            var releasesItemIsIn = new List<Release>();

            foreach (var release in getReleases())
            {
                if (db.SelectSingleNode("//items/item[@uri='" + uri + "'][@release='" + release.id + "']") != null)
                {
                    if (db.SelectSingleNode("//release[@id='" + release.id + "'][not(@dateFinalized)]") != null)
                    {
                        releasesItemIsIn.Add(release);
                    }
                }
            }

            return releasesItemIsIn;
        }

        /// <summary>
        /// Gets the releases an item is currently NOT in
        /// </summary>
        /// <param name="uri">Uri of the item</param>
        /// <returns></returns>
        public List<Release> getReleasesItemIsNotIn(string uri)
        {
            var releasesItemIsNotIn = new List<Release>();

            foreach (var release in getReleases())
            {
                if (db.SelectSingleNode("//items/item[@uri='" + uri + "'][@release='" + release.id + "']") == null)
                {
                    if (db.SelectSingleNode("//release[@id='" + release.id + "'][not(@dateFinalized)]") != null)
                    {
                        releasesItemIsNotIn.Add(release);
                    }
                }
            }

            return releasesItemIsNotIn;
        }

        public Release getRelease(XmlNode releaseNode)
        {
            return getRelease(releaseNode, true);
        }

        public Release getRelease(string releaseId)
        {
            return getRelease(getReleaseNodeById(releaseId), true);
        }

        public Release getReleaseShallow(string releaseId)
        {
            return getRelease(getReleaseNodeById(releaseId), false);
        }

        public XmlNode getReleaseNodeById(string releaseId)
        {
            return db.SelectSingleNode("//releases/release[@id='" + releaseId + "']");
        }

        private Release getRelease(XmlNode releaseNode, bool shouldPopulateItems)
        {
            //var releaseNode = db.SelectSingleNode("//releases/release[@id='" + releaseId + "']");
            DateTime? dateAdded = null;
            DateTime? dateFinalized = null;

            Release release = null;

            if (releaseNode != null)
            {
                if (releaseNode.Attributes["dateAdded"] != null && releaseNode.Attributes["dateAdded"].Value != "")
                {
                    dateAdded = DateTime.Parse(releaseNode.Attributes["dateAdded"].Value, null, DateTimeStyles.RoundtripKind);
                }
                if (releaseNode.Attributes["dateFinalized"] != null && releaseNode.Attributes["dateFinalized"].Value != "")
                {
                    dateFinalized = DateTime.Parse(releaseNode.Attributes["dateFinalized"].Value, null, DateTimeStyles.RoundtripKind);
                }

                release = new Release()
                {
                    id = releaseNode.Attributes["id"].InnerText,
                    title = releaseNode.SelectSingleNode("title").InnerText,
                    added = dateAdded,
                    finalized = dateFinalized,
                    note = releaseNode.SelectSingleNode("note").InnerText,
                    addedBy = releaseNode.SelectSingleNode("addedBy") != null ? releaseNode.SelectSingleNode("addedBy").InnerText : "",
                    deletedBy = releaseNode.SelectSingleNode("deletedBy") != null ? releaseNode.SelectSingleNode("deletedBy").InnerText : ""
                };

                if (shouldPopulateItems)
                {
                    var releaseItemNodes = db.SelectNodes("//items/item[@release='" + release.id + "']");
                    var items = new List<ReleaseItem>();
                    foreach (XmlNode releaseItemNode in releaseItemNodes)
                    {
                        items.Add(getReleaseItem(releaseItemNode));
                    }
                    release.items = items;

                    populateConflictData(release);
                }
            }

            return release;
        }

        /// <summary>
        /// Removes a release from the db.  
        /// </summary>
        /// <param name="release"></param>
        /// <returns></returns>
        public bool removeRelease(string release)
        {
            //try
            //{
            //    XmlNode releaseNode = db.SelectSingleNode("//release[@id='" + release + "']");
            //    if (releaseNode != null)
            //    {
            //        //releaseNode.ParentNode.RemoveChild(releaseNode);
            //        var deletedAttribute = db.CreateAttribute("isDeleted");
            //        deletedAttribute.Value = "true";
            //        releaseNode.Attributes.Append(deletedAttribute);
            //        db.Save(getPathReleaseManagerDb());
            //        return true;
            //    }
            //    return false;
            //}
            //catch (Exception)
            //{
            //    logError("Unable to delete release '" + release + "' from releasemananger db. (" + getPathReleaseManagerDb() + ")");
            //    return false;
            //}

            var deleteSuccess = true;
            XmlNode releaseNode;
            // Delete the release node 
            try
            {
                // Delete all of the release items
                while ((releaseNode = db.SelectSingleNode("//item[@release='" + release + "']")) != null)
                {
                    releaseNode.ParentNode.RemoveChild(releaseNode);
                }
                // Delete the main release node
                if ((releaseNode = db.SelectSingleNode("//release[@id='" + release + "']")) != null)
                {
                    releaseNode.ParentNode.RemoveChild(releaseNode);
                }
                else
                {
                    deleteSuccess = false;
                }
            }
            catch (Exception)
            {
                logError("Unable to delete release '" + release + "' from releasemananger db. (" + getPathReleaseManagerDb() + ")");
                deleteSuccess = false;
            }
            if (deleteSuccess)
            {
                db.Save(getPathReleaseManagerDb());
            }
            return deleteSuccess;
        }

        /// <summary>
        /// Returns a list of releases who have at lease one item
        /// </summary>
        /// <returns></returns>
        public List<string> getReleasesWithAtLeastOneItem()
        {
            List<string> returnReleases = new List<string>();

            XmlNodeList releases = db.SelectNodes("//release");
            foreach (XmlNode release in releases)
            {
                string releaseName = release.InnerText;
                if (db.SelectNodes("//item[@release='" + releaseName + "']").Count > 0)
                {
                    returnReleases.Add(releaseName);
                }
            }
            return returnReleases;

        }
        /// <summary>
        /// Gets all the releases from the db
        /// </summary>
        /// <returns></returns>
        public List<Release> getReleases()
        {
            XmlNodeList releaseNodes = db.SelectNodes("//releases/release");
            return getReleases(releaseNodes);
        }

        public List<Release> getReleases(XmlNodeList releaseNodes)
        {
            var releasesInDb = new List<Release>();
            if (releaseNodes.Count > 0)
            {

                foreach (XmlNode releaseNode in releaseNodes)
                {
                    releasesInDb.Add(getRelease(releaseNode));
                }
            }
            releasesInDb.Sort();

            //populateConflictData(releasesInDb);

            return releasesInDb;
        }

        public void moveFinalizedReleasesToEnd(ref List<Release> releases)
        {
            releases.Sort();
            var notFinalizedReleaseList = new List<Release>();
            var finalizedReleaseList = new List<Release>();
            foreach (var release in releases)
            {
                if (release.isDeleted())
                {
                    finalizedReleaseList.Add(release);
                }
                else
                {
                    notFinalizedReleaseList.Add(release);
                }
            }
            notFinalizedReleaseList.AddRange(finalizedReleaseList);
            releases = notFinalizedReleaseList;
        }



        //public void updateItemDetails_New(ReleaseItem item, string releaseId)
        //{
        //    SessionAwareCoreServiceClient tridionClient = getCoreServiceClient();

        //    // if item really exists by tcmid
        //    if (tridionClient.IsExistingObject(item.URI))
        //    {

        //        // get the item
        //        RepositoryLocalObjectData tridionItem = (RepositoryLocalObjectData)tridionClient.Read(item.URI, new ReadOptions());

        //        //System.IO.File.WriteAllText(@"C:\Users\Administrator\Desktop\text.txt", item.WEBDAV_URL + "," + tridionItem.LocationInfo.WebDavUrl);
        //        // if the webdav name is incorrect try and fix it
        //        if (item.WEBDAV_URL != tridionItem.LocationInfo.WebDavUrl)
        //        {
                    
        //            item.WEBDAV_URL = tridionItem.LocationInfo.WebDavUrl;
        //            item.TITLE = tridionItem.Title;
        //            updateReleaseItem(item);


        //        }
        //    }
        //    else
        //    {
        //        // item doesn't exist so remove it from the release
        //        removeFromRelease(item.URI, releaseId);
        //    }
        //}

        public void updateItemDetails(ReleaseItem item, string releaseId)
        {
            SessionAwareCoreServiceClient tridionClient = getCoreServiceClient();

            // if item really exists by tcmid
            if (tridionClient.IsExistingObject(item.URI))
            {
                // if the webdav name is incorrect try and fix it
                if (!tridionClient.IsExistingObject(item.WEBDAV_URL))
                {
                    // get the item
                    RepositoryLocalObjectData tridionItem = (RepositoryLocalObjectData)tridionClient.Read(item.URI, new ReadOptions());
                    item.WEBDAV_URL = tridionItem.LocationInfo.WebDavUrl;
                    item.TITLE = tridionItem.Title;
                    updateReleaseItem(item);
                }
            }
            else
            {
                // item doesn't exist so remove it from the release
                removeFromRelease(item.URI, releaseId);
            }
        }

        public bool isItemRenamed(ReleaseItem item, string releaseId)
        {
            bool renamed = false;

            SessionAwareCoreServiceClient tridionClient = getCoreServiceClient();

            if (tridionClient.IsExistingObject(item.URI))
            {
                // get the item
                RepositoryLocalObjectData tridionItem = (RepositoryLocalObjectData)tridionClient.Read(item.URI, new ReadOptions());
                renamed = item.WEBDAV_URL != tridionItem.LocationInfo.WebDavUrl;
            }

            tridionClient.Close();

            return renamed;
        }

        // dj - May 2016
        //public void updateItemDetailsInReleaseData(Release release)
        //{
        //    SessionAwareCoreServiceClient tridionClient = getCoreServiceClient();

        //    foreach (var item in release.items)
        //    {
        //        if (this.stillExists(item) && tridionClient.IsExistingObject(item.URI))
        //        {
        //            RepositoryLocalObjectData tridionItem = (RepositoryLocalObjectData)tridionClient.Read(item.URI, new ReadOptions());
        //            XmlNode webDavNode = db.SelectSingleNode("//items/item[@uri='" + item.URI + "'][@release='" + release.id + "']/webdav_url");
        //            webDavNode.InnerText = tridionItem.LocationInfo.WebDavUrl;
        //        }
        //    }

        //    db.Save(getPathReleaseManagerDb());
        //}


        // dj - July 2016
        public string updateItemInReleaseData(Release release, string itemTcmId)
        {
            string newItemFullPath = "";

            SessionAwareCoreServiceClient tridionClient = getCoreServiceClient();
            RepositoryLocalObjectData tridionItem = (RepositoryLocalObjectData)tridionClient.Read(itemTcmId, new ReadOptions());

            if (null != tridionItem)
            {
                XmlNode webDavNode = db.SelectSingleNode("//items/item[@uri='" + itemTcmId + "'][@release='" + release.id + "']/webdav_url");
                webDavNode.InnerText = tridionItem.LocationInfo.WebDavUrl;

                XmlNode titleNode = db.SelectSingleNode("//items/item[@uri='" + itemTcmId + "'][@release='" + release.id + "']/title");
                titleNode.InnerText = tridionItem.Title;

                //string dbPath = getPathReleaseManagerDb();

                //var file = new FileStream(dbPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);

                //File.Delete(dbPath);

                //var existingReleaseDataFile = new FileStream(dbPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                
                //db.Save(file);


                //Try reverting back to this next:
                db.Save(getPathReleaseManagerDb());

                //File.WriteAllText(getPathReleaseManagerDb(), db.OuterXml);

                
                //file.Unlock();

                //File.Delete(dbPath);
                //File.Copy(dbPath + "1", dbPath);
                //File.Delete(dbPath + "1");

                //while (IsFileLocked(new FileInfo(dbPath)))
                //{
                //    Thread.Sleep(TimeSpan.FromSeconds(8));
                //}

                //file.Close();

                //Thread.Sleep(TimeSpan.FromSeconds(2.5));

                newItemFullPath = HttpUtility.UrlDecode(webDavNode.InnerText).Replace("/webdav/", String.Empty);
            }

            tridionClient.Close();

            return newItemFullPath;
        }
        
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            return false;
        }

        /// <summary>
        /// stillExists
        /// Added by j.winter@contentbloom.com - checks that the item still exists in the CMS
        /// </summary>
        /// <param name="item"></param>
        public bool stillExists(ReleaseItem item)
        {
            using (SessionAwareCoreServiceClient tridionClient = getCoreServiceClient())
            {
                // if item is missing by tcm id, already return false
                if (!tridionClient.IsExistingObject(item.URI))
                {
                    return false;
                }
            }
            return true;
        }

        private void populateConflictData(List<Release> releases)
        {
            foreach (var release in releases)
            {
                populateConflictData(release);
            }
        }

        private void populateConflictData(Release release)
        {
            foreach (var item in release.items)
            {
                item.possiblyConflictsWith = new List<Release>();
                item.definitelyConflictsWith = new List<Release>();

                var otherItems = findItemsByTcmUri(item.URI);

                foreach (var otherItem in otherItems)
                {
                    if (otherItem.id != item.id)
                    {
                        var releaseForOtherItem = findReleaseForItem(otherItem);

                        if ((releaseForOtherItem.finalized == null
                            || releaseForOtherItem.finalized > release.added)
                            && item.added > releaseForOtherItem.added)
                        {
                            item.definitelyConflictsWith.Add(getReleaseShallow(releaseForOtherItem.id));
                        }
                        if (releaseForOtherItem.finalized != null &&
                            (release.added < releaseForOtherItem.finalized
                            && releaseForOtherItem.added < release.added)
                            && item.added > releaseForOtherItem.finalized)
                        {
                            item.possiblyConflictsWith.Add(getReleaseShallow(releaseForOtherItem.id));
                        }
                    }
                }
            }
        }

        private Release findReleaseForItem(ReleaseItem item)
        {
            return getReleaseShallow(item.releaseId);
        }

        private List<ReleaseItem> findItemsByTcmUri(string uri)
        {
            var items = new List<ReleaseItem>();

            var itemNodes = db.SelectNodes("//items/item[@uri='" + uri + "']");
            foreach (XmlNode itemNode in itemNodes)
            {
                items.Add(getReleaseItem(itemNode));
            }

            return items;
        }

        public List<Release> getAllReleases()
        {
            XmlNodeList releaseNodes = db.SelectNodes("//releases/release");
            return getReleases(releaseNodes);
        }

        /// <summary>
        /// Counts number of items in a specific release
        /// </summary>
        /// <param name="release"></param>
        /// <returns></returns>
        public int countItemsInRelease(Release release)
        {
            try
            {
                return db.SelectNodes("//item[@release='" + release.id + "']").Count;
            }
            catch (Exception)
            {

                return 0;
            }

        }

        /// <summary>
        /// Adds a new release to the db
        /// </summary>
        /// <param name="releaseName">Name of the new release</param>
        /// <returns></returns>
        public bool addRelease(string releaseName)
        {
            return saveRelease(new Release() { title = releaseName });
        }

        public bool saveRelease(Release release)
        {
            XmlNode releaseNode = db.SelectSingleNode("//release[@id='" + release.id + "']");

            if (releaseNode != null)
            {
                var releasesNode = db.SelectSingleNode("//releases");
                releasesNode.RemoveChild(releaseNode);
            }
            db.SelectSingleNode("//releases").AppendChild(release.ToXml(db));

            try
            {
                db.Save(getPathReleaseManagerDb());
                return true;
            }
            catch (XmlException ex)
            {
                logError("Error while saving to the database.xml (" + getPathReleaseManagerDb() + "). Message: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Adds a (tridion)item to the db
        /// </summary>
        /// <param name="item">ReleaseManagerObject. The item to add to the db</param>
        /// <param name="release">The release to add the item to</param>
        /// <returns></returns>
        public bool addToRelease(ReleaseItem item, string release)
        {
            XmlNode itemNode = db.SelectSingleNode("//items/item[@uri='" + item.URI + "'][@release='" + release + "']");
            if (itemNode != null) return false;

            item.releaseId = release;
            db.SelectSingleNode("//items").AppendChild(item.ToXml(db));

            try
            {
                db.Save(getPathReleaseManagerDb());
            }
            catch (XmlException ex)
            {
                logError("Error while saving to the database.xml (" + getPathReleaseManagerDb() + "). Message: " + ex.Message);
            }

            return true;
        }

        /// <summary>
        /// Removes an item from a specific release
        /// </summary>
        /// <param name="uri">URI of the item to delete</param>
        /// <param name="release">Release to delete the item from</param>
        /// <returns></returns>
        public bool removeFromRelease(string uri, string release)
        {
            XmlNode itemNode = db.SelectSingleNode("//items/item[@uri='" + uri + "'][@release='" + release + "']");
            if (itemNode == null) return true;

            try
            {
                db.SelectSingleNode("//items").RemoveChild(itemNode);
                db.Save(getPathReleaseManagerDb());
                return true;
            }
            catch (XmlException ex)
            {
                logError("Error while removing item from the database. (" + getPathReleaseManagerDb() + "). Message: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        ///  updates a release node, only works if we have a correct tcm id
        ///  item is updated for all instance of it, to save efficencies in the future
        /// </summary>
        /// <param name="item"></param>
        public void updateReleaseItem(ReleaseItem updatedItem)
        {
            XmlNodeList itemNodes = db.SelectNodes("//items/item[@uri='" + updatedItem.URI + "']");
            if (itemNodes != null) return;

            foreach (XmlNode itemNode in itemNodes)
            {
                //<item uri="tcm:71-7130-64" release="5c4edec2-574c-4fbe-8e8e-e294f3270466" dateAdded="2014-07-19T00:03:27.2099609Z" id="a69d4039-a39b-43ae-bda8-c3cf9dc928d9">
                //  <webdav_url>/webdav/600%20www%2Esmarttarget%2Ecom/Home/homepage.tpg</webdav_url>
                //  <title>homepage</title>
                //  ....
                //</item>

                // an item has child nodes, let's deal with those now:

                itemNode["webdav_url"].Value = updatedItem.WEBDAV_URL;
                itemNode["title"].Value = updatedItem.TITLE;

                try
                {
                    db.Save(getPathReleaseManagerDb());
                }
                catch (XmlException ex)
                {
                    logError("Error while saving updated item to the database.xml (" + getPathReleaseManagerDb() + "). Message: " + ex.Message);
                }
            }

        }


        public void updateReleaseItem_New(ReleaseItem updatedItem)
        {
            XmlNodeList itemNodes = db.SelectNodes("//items/item[@uri='" + updatedItem.URI + "']");
            //if (itemNodes != null) return;

            foreach (XmlNode itemNode in itemNodes)
            {
                //<item uri="tcm:71-7130-64" release="5c4edec2-574c-4fbe-8e8e-e294f3270466" dateAdded="2014-07-19T00:03:27.2099609Z" id="a69d4039-a39b-43ae-bda8-c3cf9dc928d9">
                //  <webdav_url>/webdav/600%20www%2Esmarttarget%2Ecom/Home/homepage.tpg</webdav_url>
                //  <title>homepage</title>
                //  ....
                //</item>

                // an item has child nodes, let's deal with those now:

                itemNode["webdav_url"].Value = updatedItem.WEBDAV_URL;
                itemNode["title"].Value = updatedItem.TITLE;

                //try
                //{
                    db.Save(getPathReleaseManagerDb());



                    //const string BundleNamespace = @"http://www.sdltridion.com/ContentManager/Bundle";
                    //SchemaData bundleTypeSchema = getCoreServiceClient().GetVirtualFolderTypeSchema(BundleNamespace);
                    //string bundleSchemaId = bundleTypeSchema.Id;
                    //var bundle = (VirtualFolderData)getCoreServiceClient().GetDefaultData(Tridion.ContentManager.CoreService.Client.VirtualFolderData, "tcm:5-2199-2", new ReadOptions());
                    //bundle.Configuration = "<Bundle xmlns=\"http://www.sdltridion.com/ContentManager/Bundle\"><Items /></Bundle>";
                    //bundle.TypeSchema = new LinkToSchemaData { IdRef = bundleSchemaId };
                    //bundle.Title = "DJsNewBund";
                    //getCoreServiceClient().Create(bundle, new ReadOptions());
                //}
                //catch (XmlException ex)
                //{
                //    logError("Error while saving updated item to the database.xml (" + getPathReleaseManagerDb() + "). Message: " + ex.Message);
                //}
            }

        }

        //public string getBundleName(string bundleFolder, string bundlePrefix)
        //{

        //}

        // TODO: rename to createBundle (singluar) if you end up using it to process just one bundle
        public string createBundle(string bundleFolder, string bundlePrefix)
        {
            SessionAwareCoreServiceClient client = getCoreServiceClient();

            const string BundleNamespace = @"http://www.sdltridion.com/ContentManager/Bundle";
            SchemaData bundleTypeSchema = client.GetVirtualFolderTypeSchema(BundleNamespace);
            string bundleSchemaId = bundleTypeSchema.Id;

            if(!bundleFolder.StartsWith("tcm:"))
            {
                // If it's not a tcm ID, convert it from webdav to tcm ID.

                // TODO: Check if bundle prefix starts with webdav, and add it if now
                // TODO: Do I need to switch / to \ or vice versa?

                bundleFolder = client.GetTcmUri(bundleFolder, null, null);
            }

            var bundle = (VirtualFolderData)client.GetDefaultData(Tridion.ContentManager.CoreService.Client.ItemType.VirtualFolder, bundleFolder, new ReadOptions());
            bundle.Configuration = "<Bundle xmlns=\"http://www.sdltridion.com/ContentManager/Bundle\"><Items /></Bundle>";
            bundle.TypeSchema = new LinkToSchemaData { IdRef = bundleSchemaId };
            // For the bundle suffix, use the title of the publication the bundle is being created in, which spaces replaced/
            // e.g. "000 Empty Parent" -> "--000_Empty_Parent"
            string bundleSuffix = "--" + bundle.BluePrintInfo.OwningRepository.Title.Replace(" ", "_");
            bundle.Title = bundlePrefix + bundleSuffix;
            var bundleItem = client.Create(bundle, new ReadOptions());

            return bundleItem.Id;
        }

        public void addItemsToBundle(string bundleId, IList<string> itemIdsToAdd)
        {
            SessionAwareCoreServiceClient client = getCoreServiceClient();

            VirtualFolderData bundle =
                (VirtualFolderData)client.Read(bundleId, new ReadOptions());

            XDocument doc = XDocument.Parse(bundle.Configuration);
            XNamespace xmlns = "http://www.sdltridion.com/ContentManager/Bundle";
            XNamespace xlink = "http://www.w3.org/1999/xlink";

            foreach (var itemId in itemIdsToAdd)
            {
                XElement newItemNode = new XElement(xmlns + "Item",
                    new XAttribute(XNamespace.Xmlns + "xlink", xlink),
                    new XAttribute(xlink + "href", itemId)
                );
                doc.Descendants(xmlns + "Items").First().Add(newItemNode);
            }

            bundle.Configuration = doc.ToString();

            //return bundle;

            client.Save(bundle, new ReadOptions());
        }

        //public void AddItemsToBundle(TcmUri bundleId, params TcmUri[] itemsToAdd)
        //{
        //    SessionAwareCoreServiceClient client = getCoreServiceClient();

        //    VirtualFolderData bundle =
        //        (VirtualFolderData)client.Read(bundleId, new ReadOptions());
        //    XmlDocument bundleConfiguration = new XmlDocument();
        //    bundleConfiguration.LoadXml(bundle.Configuration);

        //    XmlNameTable nameTable = new NameTable();
        //    XmlNamespaceManager namespaceManager = new XmlNamespaceManager(nameTable);
        //    namespaceManager.AddNamespace("b", @"http://www.sdltridion.com/ContentManager/Bundle");
        //    namespaceManager.AddNamespace("xlink", @"http://www.w3.org/1999/xlink");

        //    XmlElement itemsElement =
        //        bundleConfiguration.SelectSingleElement("/b:Bundle/b:Items", namespaceManager);

        //    foreach (var repositoryLocalObject in itemsToAdd)
        //    {
        //        XmlElement itemElement =
        //            itemsElement.OwnerDocument.CreateElement("Item", @"http://www.sdltridion.com/ContentManager/Bundle");
        //        itemElement.SetXLinkAttribute("type", "simple");
        //        XmlAttribute xmlAttr =
        //            itemElement.OwnerDocument.CreateAttribute("xlink", "href", "http://www.w3.org/1999/xlink");
        //        xmlAttr.Value = respositoryLocalObject;
        //        itemElement.SetAttributeNode(xmlAttr);
        //        itemsElement.AppendChild(itemElement);
        //    }

        //    client.Save(bundle, new ReadOptions());
        //}

        //public void addItemToBundle(string bundleFolder, string itemTcm)
        //{

        //}

        /// <summary>
        /// Checks to see if a release contains errors
        /// </summary>
        /// <param name="release"></param>
        /// <returns></returns>
        public bool releaseContainsErrors(Release release)
        {
            foreach (var item in release.items)
            {
                if (!stillExists(item))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the item type of an URI
        /// </summary>
        /// <param name="uri">The URI of the item</param>
        /// <returns></returns>
        public static int getItemTypeByURI(string uri)
        {
            int itemtype = 0;

            if (CountStringOccurrences(uri, "-") == 1)
            {
                itemtype = 16; //Component
            }
            else
            {
                itemtype = int.Parse(uri.Substring(uri.LastIndexOf("-") + 1));
            }
            return itemtype;
        }

        #region ImportExport methods

        /// <summary>
        /// Returns the filename of the Import or Export settings file
        /// </summary>
        /// <param name="release">Name of the release to return the filename from</param>
        /// <param name="ImportExport">Do you want the IMPORT or the EXPORT filename</param>
        /// <returns></returns>
        public static string getImportExportSettingsFile(string release, string ImportExport)
        {
            string fileName = getFileNameRelease(release, ImportExport);
            fileName = getPhysicalDirectory() + TEMP_FOLDER + fileName;

            //Check directory
            if (!Directory.Exists(getPhysicalDirectory() + TEMP_FOLDER))
            {
                try
                {
                    Directory.CreateDirectory(getPhysicalDirectory() + TEMP_FOLDER);
                }
                catch (Exception ex)
                {
                    logError("Unable to create temporary directory. Message: " + ex.Message);
                    throw;
                }
            }

            try
            {
                File.Create(fileName).Close();
            }
            catch (Exception ex)
            {
                logError("Unable to create download file. Message:" + ex.Message);
                throw;
            }

            return fileName;
        }

        /// <summary>
        /// Creates a filename for a Import or Export settings file
        /// </summary>
        /// <param name="release"></param>
        /// <param name="ImportExport"></param>
        /// <returns></returns>
        public static string getFileNameRelease(string release, string ImportExport)
        {
            return HttpUtility.HtmlEncode(ImportExport.ToUpper() + "_" + release) + ".xml";
        }

        /// <summary>
        /// Creates the IMPORT settings file by using the export settings file
        /// </summary>
        /// <param name="exportSettings">XML of the exportsettings</param>
        /// <param name="release">Release to create import settings for</param>
        /// <returns></returns>
        public string createImportSettingsFromExportSettings(string exportSettings, string release)
        {

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(exportSettings);

            XmlNode ExportNode = doc.SelectSingleNode("/ExportSettings");
            XmlNode ImportNode = doc.CreateElement("ImportSettings");

            //Copy attributes from exportNode to ImportNode
            foreach (XmlAttribute atrr in ExportNode.Attributes)
            {
                XmlAttribute newAttribute = doc.CreateAttribute(atrr.Name);
                newAttribute.Value = atrr.Value;
                ImportNode.Attributes.Append(newAttribute);
            }

            //Append the xml with items to import to the ImportNode
            ImportNode.InnerXml = ExportNode.InnerXml;
            doc.ReplaceChild(ImportNode, ExportNode);

            //Add isExported node
            foreach (XmlNode browserItemNode in ImportNode.SelectNodes("//cp:BrowserItem", nm))
            {
                //IsExported
                XmlNode IsExported = doc.CreateElement("IsExported", SCHEMA_NAMESPACE);
                IsExported.InnerText = "true";
                browserItemNode.AppendChild(IsExported);
            }

            doc.Save(getImportExportSettingsFile(release, "import"));
            return doc.InnerXml.ToString();
        }

        /// <summary>
        /// Creates the export settings file
        /// </summary>
        /// <param name="release">Release for which to create the export settings</param>
        /// <returns></returns>
        public List<ReleaseItem> getItemsInRelease(string release)
        {
            var itemsInRelease = new List<ReleaseItem>();

            XmlNodeList itemsToRelease = db.SelectNodes("//items/item[@release='" + release + "']");
            foreach (XmlNode releaseItem in itemsToRelease)
            {
                itemsInRelease.Add(getReleaseItem(releaseItem));
            }

            return itemsInRelease;
        }

        public bool HasRelease(string releaseId)
        {
            return db.SelectSingleNode("//releases/release[@id='" + releaseId + "']") != null;
        }

        public bool HasReleaseItem(string itemId)
        {
            return db.SelectSingleNode("//items/item[@id='" + itemId + "']") != null;
        }

        public ReleaseItem getReleaseItem(XmlNode releaseItem)
        {
            var rmo = new ReleaseItem();
            if (releaseItem != null)
            {
                rmo.id = releaseItem.Attributes["id"].Value;
                rmo.releaseId = releaseItem.Attributes["release"].Value;
                rmo.WEBDAV_URL = releaseItem.SelectSingleNode("webdav_url").InnerText;
                rmo.URI = releaseItem.Attributes["uri"].Value;
                rmo.ISSHARED = releaseItem.SelectSingleNode("is_shared").InnerText.Equals("true");
                rmo.SUBTYPE = int.Parse(releaseItem.SelectSingleNode("subtype").InnerText);
                rmo.TITLE = releaseItem.SelectSingleNode("title").InnerText;
                rmo.added = releaseItem.Attributes["dateAdded"] != null ?
                    DateTime.Parse(releaseItem.Attributes["dateAdded"].Value, null, DateTimeStyles.RoundtripKind)
                    : DateTime.Now;
            }
            return rmo;
        }

        public ReleaseItem getReleaseItem(string uri, string release)
        {
            return getReleaseItem(db.SelectSingleNode("//items/item[@uri='" + uri + "'][@release='" + release + "']"));
        }

        /// <summary>
        /// Creates the export settings file
        /// </summary>
        /// <param name="release">Release for which to create the export settings</param>
        /// <returns></returns>
        public string createExportSettingsCP(string release)
        {


            //Select all items from this release
            XmlNodeList itemsToRelease = db.SelectNodes("//items/item[@release='" + release + "']");

            //Load bone structure into document           
            importExportDoc.LoadXml(getBoneStructureSettingsFile());

            var tridionClient = getCoreServiceClient();

            //create filter to show also webdav url
            PublicationsFilterData pubFilterData = new PublicationsFilterData();
            pubFilterData.IncludeWebDavUrlColumn = true;
            XmlElement publications = tridionClient.GetSystemWideListXml(pubFilterData).ToXmlElement();

            //Add list of publications to importExportDoc                
            XmlNode rootBrowserItem = importExportDoc.SelectSingleNode("//cp:BrowserItem/cp:Children", nm);
            foreach (XmlNode publicationNode in publications)
            {
                rootBrowserItem.AppendChild(getPublicationBrowserItemNode(publicationNode));
            }


            //Add items to document
            foreach (XmlNode releaseItem in itemsToRelease)
            {

                string webdavPath = releaseItem.SelectSingleNode("webdav_url").InnerText;
                //Split up webdavpath into array
                char[] splitter = { '/' };
                string[] path = webdavPath.Split(splitter);


                //First 2 parts (three actually, because webdav path always starts with a '/') are ALWAYS the publication path



                string publicationPath = "/" + path[1] + "/" + path[2];

                //cp:BrowserItem/cp:WebDavUrl/cp:Url[. ='" + publicationPath + "']

                //foreach(XmlNode xnode in importExportDoc.SelectNodes("//cp:BrowserItem", nm))
                //{
                //    Console.WriteLine(xnode.OuterXml);
                //    Console.WriteLine("------------");
                //}

                // TODO: JW changed - added double 'webdav'
                XmlNode publicationNode = importExportDoc.SelectSingleNode("//cp:BrowserItem/cp:WebDavUrl/cp:Url[. ='/webdav/" + publicationPath + "']", nm).ParentNode.ParentNode;
                //Set checkstatus of node
                publicationNode.SelectSingleNode("cp:CheckStatus", nm).InnerText = "ItemAndSelectedChildren";

                XmlNode pubChildrenNode = publicationNode.SelectSingleNode("cp:Children", nm);
                int itemTypeReleaseItem = getItemTypeByURI(releaseItem.Attributes["uri"].Value.ToString());

                int startIndex = 3;
                int endIndex = path.Length;

                //loop throug webdav paht of item to add parentItems to importExportDoc
                string webdavPathCurrentItem = publicationPath;
                for (int i = startIndex; i < endIndex; i++)
                {
                    webdavPathCurrentItem += "/" + path[i];

                    //Create browserItemNode for this path
                    //Need: webdavpath, itemType and isShared. isShared has to come from coreservice if its an organizational item from the item to release.                    
                    //It's an organizationalitem if the end of the array isn't reached
                    bool IsShared = false;
                    int currentItemType = 0;
                    string checkStatus = "ItemAndAllChildren"; //SelectedChildrenOnly or ItemAndAllChildren or ItemAndSelectedChildren
                    bool canContainChildren = false;
                    string subType = releaseItem.SelectSingleNode("subtype").InnerText;
                    if (i == (endIndex - 1))
                    {
                        IsShared = Boolean.Parse(releaseItem.SelectSingleNode("is_shared").InnerText);
                        currentItemType = itemTypeReleaseItem;

                        // if its an org itemtype, list all the childeren. Recursively.
                        if (currentItemType == 1 || currentItemType == 2 || currentItemType == 4 || currentItemType == 512)
                        {
                            //checkStatus = "ItemAndAllChildren";
                            canContainChildren = true;

                        }
                    }
                    else
                    {
                        //It's an organizationalitem                                             
                        OrganizationalItemData orgItemData = (OrganizationalItemData)tridionClient.Read(webdavPathCurrentItem, new ReadOptions());
                        IsShared = (bool)orgItemData.BluePrintInfo.IsShared;
                        currentItemType = getItemTypeByURI(orgItemData.Id);

                        if (currentItemType == 1 || currentItemType == 2 || currentItemType == 4 || currentItemType == 512)
                        {
                            checkStatus = "SelectedChildrenOnly";
                            canContainChildren = true;
                        }
                    }

                    //Check if BrowserItemNode already Exists
                    XmlNode BrowserItemNode = null;
                    try //I Like it: ugly ;)
                    {
                        BrowserItemNode = importExportDoc.SelectSingleNode("//cp:BrowserItem/cp:WebDavUrl/cp:Url[. ='" + webdavPathCurrentItem + "']", nm).ParentNode.ParentNode;
                    }
                    catch (Exception)
                    {
                    }

                    if (BrowserItemNode == null)
                    {
                        BrowserItemNode = getBrowserItemNode(webdavPathCurrentItem, currentItemType, IsShared, checkStatus, canContainChildren, subType);
                    }

                    //Add itemnode
                    pubChildrenNode.AppendChild(BrowserItemNode);
                    pubChildrenNode = BrowserItemNode.SelectSingleNode("cp:Children", nm);

                    //List all childeren from org item if it's the last part of the webdav path AND if it's an org item
                    if (i == (endIndex - 1) && (currentItemType == 1 || currentItemType == 2 || currentItemType == 4 || currentItemType == 512))
                    {
                        //Read child items from org item
                        OrganizationalItemItemsFilterData d = new OrganizationalItemItemsFilterData();
                        d.IncludeRelativeWebDavUrlColumn = true;
                        d.BaseColumns = ListBaseColumns.Extended;
                        XmlElement childs = tridionClient.GetListXml(webdavPathCurrentItem, d).ToXmlElement();

                        recursive(childs, pubChildrenNode, webdavPathCurrentItem, tridionClient);
                    }


                }
            }
            //Save it to disk
            importExportDoc.Save(getImportExportSettingsFile(release, "export"));

            return importExportDoc.InnerXml.ToString();

        }

        /// <summary>
        /// Recursively traverse the tree
        /// </summary>
        /// <param name="childs"></param>
        /// <param name="nodeToAppendTo"></param>
        /// <param name="webdavUrl"></param>
        private void recursive(XmlElement childs, XmlNode nodeToAppendTo, string webdavUrl, SessionAwareCoreServiceClient tridionClient)
        {
            //CoreService2010Client tridionClient = new CoreService2010Client("basicHttp_2010");
            string itemWebdavUrl = webdavUrl;
            bool itemIsShared = false;
            string itemCheckStatus = "";
            bool itemCanContainChildren = false;
            int itemType = 0;
            string itemSubType = "0";

            foreach (XmlNode itemNode in childs)
            {
                itemType = getItemTypeByURI(itemNode.Attributes["ID"].Value);
                if (itemType == 1 || itemType == 2 || itemType == 4 || itemType == 512)
                {
                    //Maak browserItemNode                   
                    itemWebdavUrl = webdavUrl + "/" + itemNode.Attributes["URL"].Value;
                    itemIsShared = Boolean.Parse(itemNode.Attributes["IsShared"].Value);
                    itemCheckStatus = "ItemAndSelectedChildren";
                    itemCanContainChildren = true;
                    if (itemNode.Attributes["SubType"] != null)
                    {
                        itemSubType = itemNode.Attributes["SubType"].Value;
                    }
                    XmlNode BrowserItemNode = null;
                    try //I Like it: ugly ;)
                    {
                        BrowserItemNode = importExportDoc.SelectSingleNode("//cp:BrowserItem/cp:WebDavUrl/cp:Url[. ='" + itemWebdavUrl + "']", nm).ParentNode.ParentNode;
                    }
                    catch (Exception)
                    {
                    }
                    if (BrowserItemNode == null)
                    {
                        BrowserItemNode = getBrowserItemNode(itemWebdavUrl, itemType, itemIsShared, itemCheckStatus, itemCanContainChildren, itemSubType);
                    }

                    XmlNode childrenNode = BrowserItemNode.SelectSingleNode("cp:Children", nm);
                    nodeToAppendTo.AppendChild(BrowserItemNode);

                    //Zet childnodes op de zojuist gemaakte browseritemnode->children.
                    OrganizationalItemItemsFilterData d = new OrganizationalItemItemsFilterData();
                    d.IncludeRelativeWebDavUrlColumn = true;
                    d.BaseColumns = ListBaseColumns.Extended;
                    XmlElement itemChilds = tridionClient.GetListXml(itemWebdavUrl, d).ToXmlElement();

                    recursive(itemChilds, childrenNode, itemWebdavUrl, tridionClient);

                }
                else
                {
                    //Maak browserItemnode                                    
                    itemWebdavUrl = webdavUrl + "/" + itemNode.Attributes["URL"].Value;
                    itemIsShared = Boolean.Parse(itemNode.Attributes["IsShared"].Value);
                    itemCheckStatus = "ItemAndAllChildren";
                    itemCanContainChildren = false;
                    if (itemNode.Attributes["SubType"] != null)
                    {
                        itemSubType = itemNode.Attributes["SubType"].Value;
                    }

                    XmlNode BrowserItemNode = null;
                    try //I Like it: ugly ;)
                    {
                        BrowserItemNode = importExportDoc.SelectSingleNode("//cp:BrowserItem/cp:WebDavUrl/cp:Url[. ='" + itemWebdavUrl + "']", nm).ParentNode.ParentNode;
                    }
                    catch (Exception)
                    {
                    }

                    if (BrowserItemNode == null)
                    {
                        BrowserItemNode = getBrowserItemNode(itemWebdavUrl, itemType, itemIsShared, itemCheckStatus, itemCanContainChildren, itemSubType);
                    }
                    //Append it to pubchildrenode
                    nodeToAppendTo.AppendChild(BrowserItemNode);

                }
            }


        }

        /// <summary>
        /// Returns a browserItemNode for a publication
        /// </summary>
        /// <param name="publicationNode"></param>
        /// <returns></returns>
        private static XmlNode getPublicationBrowserItemNode(XmlNode publicationNode)
        {
            XmlNode BrowserItem = importExportDoc.CreateNode(XmlNodeType.Element, "BrowserItem", SCHEMA_NAMESPACE);

            //WebDavUrl
            XmlNode WebDavUrl = importExportDoc.CreateElement("WebDavUrl", SCHEMA_NAMESPACE);
            XmlNode Url = importExportDoc.CreateElement("Url", SCHEMA_NAMESPACE);
            Url.InnerText = "/webdav/" + publicationNode.Attributes["URL"].Value.ToString();
            WebDavUrl.AppendChild(Url); BrowserItem.AppendChild(WebDavUrl);

            //ItemType
            int itemType = 1;
            XmlNode ItemType = importExportDoc.CreateElement("ItemType", SCHEMA_NAMESPACE);
            ItemType.InnerText = ((Tridion.ContentManager.ItemType)itemType).ToString();
            BrowserItem.AppendChild(ItemType);

            //ItemSubType
            XmlNode ItemSubType = importExportDoc.CreateElement("ItemSubType", SCHEMA_NAMESPACE);
            ItemSubType.InnerText = "0";
            BrowserItem.AppendChild(ItemSubType);

            //IsShared
            XmlNode IsShared = importExportDoc.CreateElement("IsShared", SCHEMA_NAMESPACE);
            IsShared.InnerText = "false";
            BrowserItem.AppendChild(IsShared);

            //IsExpandable
            XmlNode IsExpandable = importExportDoc.CreateElement("IsExpandable", SCHEMA_NAMESPACE);
            IsExpandable.InnerText = getExpandableByItemType((Tridion.ContentManager.ItemType)itemType).ToString().ToLower();
            BrowserItem.AppendChild(IsExpandable);

            //CanContainChildren
            XmlNode CanContainChildren = importExportDoc.CreateElement("CanContainChildren", SCHEMA_NAMESPACE);
            CanContainChildren.InnerText = getExpandableByItemType((Tridion.ContentManager.ItemType)itemType).ToString().ToLower();
            BrowserItem.AppendChild(CanContainChildren);

            //Children
            XmlNode Children = importExportDoc.CreateElement("Children", SCHEMA_NAMESPACE);
            BrowserItem.AppendChild(Children);

            //CheckStatus
            XmlNode CheckStatus = importExportDoc.CreateElement("CheckStatus", SCHEMA_NAMESPACE);
            CheckStatus.InnerText = "None";
            BrowserItem.AppendChild(CheckStatus);

            //IsExpanded
            XmlNode IsExpanded = importExportDoc.CreateElement("IsExpanded", SCHEMA_NAMESPACE);
            IsExpanded.InnerText = getExpandableByItemType((Tridion.ContentManager.ItemType)itemType).ToString().ToLower();
            BrowserItem.AppendChild(IsExpanded);

            return BrowserItem;

        }

        /// <summary>
        /// Returns a BrowserItemNode tag for a Tridion object
        /// </summary>
        /// <param name="WebdavPath">Webdav paht of the item</param>
        /// <param name="itemType">Itemtype</param>
        /// <param name="isShared">IsShared</param>
        /// <param name="checkStatus">The checkstatus of the item. Can be: SelectedChildrenOnly, ItemAndAllChildren or ItemAndSelectedChildren</param>
        /// <param name="canContainChildren">If its an orgItem it can contain childeren.</param>
        /// <param name="subType">SubType of the item</param>
        /// <returns></returns>
        private static XmlNode getBrowserItemNode(string WebdavPath, int itemType, bool isShared, string checkStatus, bool canContainChildren, string subType)
        {

            XmlNode BrowserItem = importExportDoc.CreateNode(XmlNodeType.Element, "BrowserItem", SCHEMA_NAMESPACE);

            //WebDavUrl
            XmlNode WebDavUrl = importExportDoc.CreateElement("WebDavUrl", SCHEMA_NAMESPACE);
            XmlNode Url = importExportDoc.CreateElement("Url", SCHEMA_NAMESPACE);
            Url.InnerText = WebdavPath;
            WebDavUrl.AppendChild(Url); BrowserItem.AppendChild(WebDavUrl);

            //ItemType
            //int itemType = getItemTypeByURI(itemToRelease.Attributes["uri"].Value);
            XmlNode ItemType = importExportDoc.CreateElement("ItemType", SCHEMA_NAMESPACE);
            ItemType.InnerText = ((Tridion.ContentManager.ItemType)itemType).ToString();
            BrowserItem.AppendChild(ItemType);

            //ItemSubType
            XmlNode ItemSubType = importExportDoc.CreateElement("ItemSubType", SCHEMA_NAMESPACE);
            ItemSubType.InnerText = subType;
            BrowserItem.AppendChild(ItemSubType);

            //IsShared
            XmlNode IsShared = importExportDoc.CreateElement("IsShared", SCHEMA_NAMESPACE);
            IsShared.InnerText = isShared.ToString().ToLower();
            BrowserItem.AppendChild(IsShared);

            //IsExpandable
            XmlNode IsExpandable = importExportDoc.CreateElement("IsExpandable", SCHEMA_NAMESPACE);
            IsExpandable.InnerText = getExpandableByItemType((Tridion.ContentManager.ItemType)itemType).ToString().ToLower();
            BrowserItem.AppendChild(IsExpandable);

            //CanContainChildren           
            XmlNode CanContainChildren = importExportDoc.CreateElement("CanContainChildren", SCHEMA_NAMESPACE);
            CanContainChildren.InnerText = canContainChildren.ToString().ToLower();//getExpandableByItemType((Tridion.ContentManager.ItemType)itemType).ToString();
            BrowserItem.AppendChild(CanContainChildren);

            //Children
            if (canContainChildren)
            {
                XmlNode Children = importExportDoc.CreateElement("Children", SCHEMA_NAMESPACE);
                BrowserItem.AppendChild(Children);
            }
            //CheckStatus
            XmlNode CheckStatus = importExportDoc.CreateElement("CheckStatus", SCHEMA_NAMESPACE);
            CheckStatus.InnerText = checkStatus;
            BrowserItem.AppendChild(CheckStatus);

            //IsExpanded
            XmlNode IsExpanded = importExportDoc.CreateElement("IsExpanded", SCHEMA_NAMESPACE);
            IsExpanded.InnerText = getExpandableByItemType((Tridion.ContentManager.ItemType)itemType).ToString().ToLower();
            BrowserItem.AppendChild(IsExpanded);

            //sb.Append("<BrowserItem xmlns=\"http://www.tridion.com/ContentManager/5.2/ImportExport\">");
            //sb.Append("<WebDavUrl>");
            ////sb.Append("<Url>/webdav//10%2E233%2E18%2E132</Url>");
            //sb.Append("<Url>/webdav//" + cmUrl.ToString() + "</Url>");
            //sb.Append("</WebDavUrl>");
            //sb.Append("<ItemType>VirtualItemInstance</ItemType>");
            //sb.Append("<ItemSubType>0</ItemSubType>");
            //sb.Append("<IsShared>false</IsShared>");
            //sb.Append("<IsExpandable>true</IsExpandable>");
            //sb.Append("<CanContainChildren>true</CanContainChildren>");
            //sb.Append("<Children></Children>");
            //sb.Append("<CheckStatus>ItemAndSelectedChildren</CheckStatus>");
            //sb.Append("<IsExpanded>true</IsExpanded>");
            //sb.Append("</BroserItem>");


            return BrowserItem;
        }

        /// <summary>
        /// Gets the bone structure of the settings file
        /// </summary>
        /// <returns></returns>
        private static string getBoneStructureSettingsFile()
        {
            string protocol = "Http";
            string name = "";
            int port = 80;
            string description = "";
            if (req != null)
            {
                protocol = UppercaseFirst(req.Url.Scheme);
                name = HttpUtility.UrlEncode(req.Url.Host);
                port = req.Url.Port;
                description = name;
            }

            StringBuilder sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

            sb.Append("<ExportSettings xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
            sb.Append("<Server xmlns=\"http://www.tridion.com/ContentManager/5.2/ImportExport\">");
            sb.Append("<Protocol>" + protocol + "</Protocol>");
            sb.Append("<Name>" + name + "</Name>");
            sb.Append("<Port>" + port + "</Port>");
            sb.Append("<Description>" + description + "</Description>");
            sb.Append("</Server>");

            //<DependencyFilterGroups>
            sb.Append("<DependencyFilterGroups xmlns=\"http://www.tridion.com/ContentManager/5.2/ImportExport\" />");
            sb.Append("<DependencyTypeGroups  xmlns=\"http://www.tridion.com/ContentManager/5.2/ImportExport\">");
            sb.Append("<ClientDependencyTypeGroup>");
            sb.Append("<Id>Layout</Id>");
            sb.Append("<Name>Layout</Name>");
            sb.Append("<Description>Page Templates, Component Templates and included Template Building Blocks.</Description>");
            sb.Append("<DependencyTypes>");
            sb.Append("<DependencyType>PageTemplate</DependencyType>");
            sb.Append("<DependencyType>IncludedTemplateBuildingBlock</DependencyType>");
            sb.Append("<DependencyType>ComponentPresentationComponentTemplate</DependencyType>");
            sb.Append("<DependencyType>TemplateLinkedItem</DependencyType>");
            sb.Append("</DependencyTypes>");
            sb.Append("<Selected>true</Selected>");
            sb.Append("</ClientDependencyTypeGroup>");
            sb.Append("<ClientDependencyTypeGroup>");
            sb.Append("<Id>Structure</Id>");
            sb.Append("<Name>Structure</Name>");
            sb.Append("<Description>Folders,StructureGroups,Keywords,TargetGroups,DefaultValues,RelatedandParentKeywords,TrackedCategoriesandMultimediaTypes.</Description>");
            sb.Append("<DependencyTypes>");
            sb.Append("<DependencyType>Publication</DependencyType>");
            sb.Append("<DependencyType>OrganizationalItemPublication</DependencyType>");
            sb.Append("<DependencyType>OrganizationalItemFolder</DependencyType>");
            sb.Append("<DependencyType>OrganizationalItemStructureGroup</DependencyType>");
            sb.Append("<DependencyType>OrganizationalItemCategory</DependencyType>");
            sb.Append("<DependencyType>Keyword</DependencyType>");
            sb.Append("<DependencyType>TargetGroup</DependencyType>");
            sb.Append("<DependencyType>AllowedMultimediaType</DependencyType>");
            sb.Append("<DependencyType>MultimediaType</DependencyType>");
            sb.Append("<DependencyType>ComponentPresentationTargetGroup</DependencyType>");
            sb.Append("<DependencyType>DefaultLinkedComponent</DependencyType>");
            sb.Append("<DependencyType>DefaultKeyword</DependencyType>");
            sb.Append("<DependencyType>RelatedKeyword</DependencyType>");
            sb.Append("<DependencyType>ParentKeyword</DependencyType>");
            sb.Append("<DependencyType>TrackedCategory</DependencyType>");
            sb.Append("</DependencyTypes>");
            sb.Append("<Selected>true</Selected>");
            sb.Append("</ClientDependencyTypeGroup>");
            sb.Append("<ClientDependencyTypeGroup>");
            sb.Append("<Id>Content</Id>");
            sb.Append("<Name>Content</Name>");
            sb.Append("<Description>LinkedItemsandComponentsinaComponentPresentation.</Description>");
            sb.Append("<DependencyTypes>");
            sb.Append("<DependencyType>LinkedComponent</DependencyType>");
            sb.Append("<DependencyType>LinkedCategory</DependencyType>");
            sb.Append("<DependencyType>LinkedKeyword</DependencyType>");
            sb.Append("<DependencyType>ComponentPresentationComponent</DependencyType>");
            sb.Append("<DependencyType>Category</DependencyType>");
            sb.Append("</DependencyTypes>");
            sb.Append("<Selected>true</Selected>");
            sb.Append("</ClientDependencyTypeGroup>");
            sb.Append("<ClientDependencyTypeGroup>");
            sb.Append("<Id>Definition</Id>");
            sb.Append("<Name>Definition</Name>");
            sb.Append("<Description>AnytypeofSchemaandParentCategories.</Description>");
            sb.Append("<DependencyTypes>");
            sb.Append("<DependencyType>MetadataSchema</DependencyType>");
            sb.Append("<DependencyType>LinkedSchema</DependencyType>");
            sb.Append("<DependencyType>EmbeddedSchema</DependencyType>");
            sb.Append("<DependencyType>Schema</DependencyType>");
            sb.Append("<DependencyType>ComponentTemplateAllowedSchema</DependencyType>");
            sb.Append("<DependencyType>SchemaAllowedSchema</DependencyType>");
            sb.Append("<DependencyType>AllowedParentCategory</DependencyType>");
            sb.Append("<DependencyType>ParameterSchema</DependencyType>");
            sb.Append("</DependencyTypes>");
            sb.Append("<Selected>true</Selected>");
            sb.Append("</ClientDependencyTypeGroup>");
            sb.Append("<ClientDependencyTypeGroup>");
            sb.Append("<Id>Configuration</Id>");
            sb.Append("<Name>Configuration</Name>");
            sb.Append("<Description>ItemsreferencedinVirtualFolderconfiguration.</Description>");
            sb.Append("<DependencyTypes>");
            sb.Append("<DependencyType>VirtualFolderConfiguration</DependencyType>");
            sb.Append("</DependencyTypes>");
            sb.Append("<Selected>true</Selected>");
            sb.Append("</ClientDependencyTypeGroup>");
            sb.Append("<ClientDependencyTypeGroup>");
            sb.Append("<Id>Defaultitems</Id>");
            sb.Append("<Name>Defaultitems</Name>");
            sb.Append("<Description>AllDefaultTemplates,DefaultMultimediaSchemaandDefaultTemplateBuildingBlock.</Description>");
            sb.Append("<DependencyTypes>");
            sb.Append("<DependencyType>DefaultComponentTemplate</DependencyType>");
            sb.Append("<DependencyType>DefaultPageTemplate</DependencyType>");
            sb.Append("<DependencyType>DefaultTemplateBuildingBlock</DependencyType>");
            sb.Append("<DependencyType>DefaultMultimediaSchema</DependencyType>");
            sb.Append("</DependencyTypes>");
            sb.Append("<Selected>true</Selected>");
            sb.Append("</ClientDependencyTypeGroup>");
            sb.Append("<ClientDependencyTypeGroup>");
            sb.Append("<Id>Workflow</Id>");
            sb.Append("<Name>Workflow</Name>");
            sb.Append("<Description>Allitemsrelatedtoworkflow.</Description>");
            sb.Append("<DependencyTypes>");
            sb.Append("<DependencyType>ApprovalStatus</DependencyType>");
            sb.Append("<DependencyType>PageTemplateProcess</DependencyType>");
            sb.Append("<DependencyType>ComponentTemplateProcess</DependencyType>");
            sb.Append("<DependencyType>PageProcess</DependencyType>");
            sb.Append("<DependencyType>ComponentProcess</DependencyType>");
            sb.Append("<DependencyType>ComponentSnapshotTemplate</DependencyType>");
            sb.Append("<DependencyType>PageSnapshotTemplate</DependencyType>");
            sb.Append("<DependencyType>TaskProcess</DependencyType>");
            sb.Append("<DependencyType>TemplateBundleProcess</DependencyType>");
            sb.Append("<DependencyType>PageBundleProcess</DependencyType>");
            sb.Append("<DependencyType>SchemaBundleProcess</DependencyType>");
            sb.Append("<DependencyType>ProcessDefinitionTemplateBuildingBlock</DependencyType>");
            sb.Append("</DependencyTypes>");
            sb.Append("<Selected>true</Selected>");
            sb.Append("</ClientDependencyTypeGroup>");
            sb.Append("<ClientDependencyTypeGroup>");
            sb.Append("<Id>Security</Id>");
            sb.Append("<Name>Security</Name>");
            sb.Append("<Description>Allsecurityrelateditems.</Description>");
            sb.Append("<DependencyTypes>");
            sb.Append("<DependencyType>Group</DependencyType>");
            sb.Append("<DependencyType>GroupMembership</DependencyType>");
            sb.Append("</DependencyTypes>");
            sb.Append("<Selected>true</Selected>");
            sb.Append("</ClientDependencyTypeGroup>");
            sb.Append("</DependencyTypeGroups>");

            //<ItemFilters
            sb.Append("<ItemFilters xmlns=\"http://www.tridion.com/ContentManager/5.2/ImportExport\">");
            sb.Append("<ItemFilter>");
            sb.Append("<Value>TargetGroups</Value>");
            sb.Append("<Selected>false</Selected>");
            sb.Append("</ItemFilter>");
            sb.Append("<ItemFilter>");
            sb.Append("<Value>Schemas</Value>");
            sb.Append("<Selected>false</Selected>");
            sb.Append("</ItemFilter>");
            sb.Append("<ItemFilter>");
            sb.Append("<Value>Components</Value>");
            sb.Append("<Selected>false</Selected>");
            sb.Append("</ItemFilter>");
            sb.Append("<ItemFilter>");
            sb.Append("<Value>TemplateBuildingBlocks</Value>");
            sb.Append("<Selected>false</Selected>");
            sb.Append("</ItemFilter>");
            sb.Append("<ItemFilter>");
            sb.Append("<Value>ComponentTemplates</Value>");
            sb.Append("<Selected>false</Selected>");
            sb.Append("</ItemFilter>");
            sb.Append("<ItemFilter>");
            sb.Append("<Value>PageTemplates</Value>");
            sb.Append("<Selected>false</Selected>");
            sb.Append("</ItemFilter>");
            sb.Append("<ItemFilter>");
            sb.Append("<Value>Pages</Value>");
            sb.Append("<Selected>false</Selected>");
            sb.Append("</ItemFilter>");
            sb.Append("<ItemFilter>");
            sb.Append("<Value>ApprovalStatuses</Value>");
            sb.Append("<Selected>false</Selected>");
            sb.Append("</ItemFilter>");
            sb.Append("<ItemFilter>");
            sb.Append("<Value>ProcessDefinitions</Value>");
            sb.Append("<Selected>false</Selected>");
            sb.Append("</ItemFilter>");
            sb.Append("<ItemFilter>");
            sb.Append("<Value>MultimediaTypes</Value>");
            sb.Append("<Selected>false</Selected>");
            sb.Append("</ItemFilter>");
            sb.Append("<ItemFilter>");
            sb.Append("<Value>Keywords</Value>");
            sb.Append("<Selected>false</Selected>");
            sb.Append("</ItemFilter>");
            sb.Append("<ItemFilter>");
            sb.Append("<Value>Groups</Value>");
            sb.Append("<Selected>false</Selected>");
            sb.Append("</ItemFilter>");
            sb.Append("</ItemFilters>");


            //BrowserItem
            sb.Append("<BrowserItem xmlns=\"http://www.tridion.com/ContentManager/5.2/ImportExport\">");
            sb.Append("<WebDavUrl>");
            //sb.Append("<Url>/webdav//10%2E233%2E18%2E132</Url>");
            sb.Append("<Url>/webdav//" + name + "</Url>");
            sb.Append("</WebDavUrl>");
            sb.Append("<ItemType>VirtualItemInstance</ItemType>");
            sb.Append("<ItemSubType>0</ItemSubType>");
            sb.Append("<IsShared>false</IsShared>");
            sb.Append("<IsExpandable>true</IsExpandable>");
            sb.Append("<CanContainChildren>true</CanContainChildren>");
            sb.Append("<Children></Children>");
            sb.Append("<CheckStatus>ItemAndSelectedChildren</CheckStatus>");
            sb.Append("<IsExpanded>true</IsExpanded>");
            sb.Append("</BrowserItem>");

            //<PackageLocation>            
            sb.Append("<PackageLocation xmlns=\"http://www.tridion.com/ContentManager/5.2/ImportExport\"></PackageLocation>");

            //<ErrorHandlingMode>
            sb.Append("<ErrorHandlingMode xmlns=\"http://www.tridion.com/ContentManager/5.2/ImportExport\">Interactive</ErrorHandlingMode>");

            sb.Append("</ExportSettings>");

            return sb.ToString();
        }
        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Logs the error to the eventlog
        /// </summary>
        /// <param name="message"></param>
        private static void logError(string message)
        {
            XmlDocument db = getConfigXmlFile();
            string eventlogName = db.SelectSingleNode("/settings/appsettings/diagnostics/write_to_eventlog").Attributes["eventlogname"].Value;
            eventlogName = (eventlogName.Equals(string.Empty)) ? _EVENTLOG_NAME : eventlogName;
            bool writeToLog = Boolean.Parse(db.SelectSingleNode("/settings/appsettings/diagnostics/write_to_eventlog").InnerText);
            if (!writeToLog) return;
            try
            {
                EventLog ev = new EventLog(eventlogName);
                ev.Source = _APPLICATION_NAME;
                ev.WriteEntry(message, EventLogEntryType.Error);
                ev.Close();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to log this error: " + message + ". Check your logging settings in " + CONFIG_SETTINGS_FILE, ex.InnerException);
            }

        }

        /// <summary>
        /// Loads the configuration.xml into an XmlDocument and returns it
        /// </summary>
        /// <returns></returns>
        private static XmlDocument getConfigXmlFile()
        {

            XmlDocument db = new XmlDocument();
            try
            {
                string filePath = getConfigFilePath();
                db.Load(filePath);

                return db;
            }
            catch (Exception)
            {
                logError("Unable to load config xml. (" + CONFIG_SETTINGS_FILE + "). Make sure the file exists!");

                return null;
            }
        }

        /// <summary>
        /// Loads the releaseData.xml
        /// </summary>
        /// <returns></returns>
        private static XmlDocument getReleaseManagerDB()
        {
            XmlDocument db = new XmlDocument();

            //If releasemanagerDb does not exists, create it.
            if (!File.Exists(getPathReleaseManagerDb()))
            {
                db.LoadXml(getBoneStructureReleaseManagerDB());
                db.Save(getPathReleaseManagerDb());
                return db;
            }

            try
            {
                db.Load(getPathReleaseManagerDb());
                return db;
            }
            catch (Exception)
            {
                logError("Unable to load releaseData xml. (" + RELEASE_MANAGER_DB + "). Make sure the file exists!");
                throw new ApplicationException();

            }
        }

        /// <summary>
        /// Returns the basic xml for the releasemanager db
        /// </summary>
        /// <returns></returns>
        private static string getBoneStructureReleaseManagerDB()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.Append("<db>");
            sb.Append("<releases>");
            sb.Append("</releases>");
            sb.Append("<items>");
            sb.Append("<!--<item uri=\"tcm:0-21-1\" release=\"testrelease1\">");
            sb.Append("<webdav_url>/webdav/ctpurl</webdav_url>");
            sb.Append("<title></title>");
            sb.Append("<version_start></version_start>");
            sb.Append("<version_end></version_end>");
            sb.Append("</item>-->");
            sb.Append("</items>");
            sb.Append("</db>");
            return sb.ToString();
        }
        /// <summary>
        /// Resolve the path to the db.xml
        /// </summary>
        /// <returns></returns>
        private static string getConfigFilePath()
        {
            return getPhysicalDirectory() + CONFIG_SETTINGS_FILE;
        }

        /// <summary>
        /// Returns the path of the ReleaseManager DB
        /// </summary>
        /// <returns></returns>
        private static string getPathReleaseManagerDb()
        {
            return getPhysicalDirectory() + RELEASE_MANAGER_DB; ;
        }

        /// <summary>
        /// Gets the directory of the application
        /// </summary>
        /// <returns></returns>
        private static string getPhysicalDirectory()
        {
            // define which character is seperating fields
            char[] splitter = { '\\' };
            string path = req.PhysicalPath;
            path = path.Substring(0, path.LastIndexOf("\\"));
            return path;
        }

        /// <summary>
        /// Count occurrences of strings.
        /// </summary>
        private static int CountStringOccurrences(string text, string pattern)
        {
            // Loop through all instances of the string 'text'.
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i)) != -1)
            {
                i += pattern.Length;
                count++;
            }
            return count;
        }

        private static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }


        private static bool getExpandableByItemType(Tridion.ContentManager.ItemType itemType)
        {
            switch (itemType)
            {
                case Tridion.ContentManager.ItemType.ActivityDefinition:
                    return false;
                case Tridion.ContentManager.ItemType.ActivityHistory:
                    return false;
                case Tridion.ContentManager.ItemType.ActivityInstance:
                    return false;
                case Tridion.ContentManager.ItemType.ApprovalStatus:
                    return false;
                case Tridion.ContentManager.ItemType.Category:
                    return true;
                case Tridion.ContentManager.ItemType.Component:
                    return false;
                case Tridion.ContentManager.ItemType.ComponentTemplate:
                    return false;
                case Tridion.ContentManager.ItemType.DirectoryGroupMapping:
                    return false;
                case Tridion.ContentManager.ItemType.DirectoryService:
                    return false;
                case Tridion.ContentManager.ItemType.Folder:
                    return true;
                case Tridion.ContentManager.ItemType.Group:
                    return true;
                case Tridion.ContentManager.ItemType.Keyword:
                    return true;
                case Tridion.ContentManager.ItemType.MultimediaType:
                    return false;
                case Tridion.ContentManager.ItemType.MultipleOperations:
                    return false;
                case Tridion.ContentManager.ItemType.None:
                    return false;
                case Tridion.ContentManager.ItemType.Page:
                    return false;
                case Tridion.ContentManager.ItemType.PageTemplate:
                    return false;
                case Tridion.ContentManager.ItemType.ProcessDefinition:
                    return false;
                case Tridion.ContentManager.ItemType.ProcessHistory:
                    return false;
                case Tridion.ContentManager.ItemType.ProcessInstance:
                    return false;
                case Tridion.ContentManager.ItemType.Publication:
                    return true;
                case Tridion.ContentManager.ItemType.PublicationTarget:
                    return false;
                case Tridion.ContentManager.ItemType.PublishTransaction:
                    return false;
                case Tridion.ContentManager.ItemType.Schema:
                    return false;
                case Tridion.ContentManager.ItemType.StructureGroup:
                    return true;
                case Tridion.ContentManager.ItemType.TargetDestination:
                    return false;
                case Tridion.ContentManager.ItemType.TargetGroup:
                    return true;
                case Tridion.ContentManager.ItemType.TargetType:
                    return false;
                case Tridion.ContentManager.ItemType.TemplateBuildingBlock:
                    return false;
                case Tridion.ContentManager.ItemType.User:
                    return false;
                case Tridion.ContentManager.ItemType.VirtualFolder:
                    return true;
                case Tridion.ContentManager.ItemType.WorkItem:
                    return false;
                default:
                    return false;

            }
        }
        #endregion
        public string getExportXml(string releaseId)
        {
            var release = getRelease(releaseId);
            var doc = new XmlDocument();
            var db = doc.CreateElement("db");
            doc.AppendChild(db);

            var releases = doc.CreateElement("releases");
            releases.AppendChild(release.ToXml(doc));

            db.AppendChild(release.itemsToXml(doc));
            db.AppendChild(releases);

            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + doc.OuterXml;
        }
    }

}
