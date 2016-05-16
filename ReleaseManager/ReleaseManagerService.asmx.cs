using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Script.Services;
using System.Web.Script.Serialization;
using System.Xml;

namespace ReleaseManager
{
    /// <summary>
    /// Summary description for ReleaseManagerService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class ReleaseManagerService : System.Web.Services.WebService
    {
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object AddNote(string releaseId, string note)
        {
            //try
            //{
            var rmo = new ReleaseManagerRepository(Server, Context.Request);
            var release = rmo.getRelease(releaseId);
            release.note = note;

            if (rmo.saveRelease(release))
            {
                return new { message = "Notes saved." };
            }
            else
            {
                return new { message = "Notes not saved." };
            }

            
            //}
            //catch (Exception e)
            //{
            //    return "Error saving note.";
            //}
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public object ImportRelease(string releaseXml)
        {
            var rmo = new ReleaseManagerRepository(Server, Context.Request);
            var doc = new XmlDocument();
            try
            {
                var errors = new List<string>();

                doc.LoadXml(releaseXml);
                
                var releaseNode = doc.SelectSingleNode("//releases/release");
                if (releaseNode == null)
                {
                    return new { result = "error", message = "Could not select a release node" };
                }

                var newRelease = rmo.getRelease(releaseNode);
                if (newRelease == null)
                {
                    return new { result = "error", message = "Could not create release object from node" };
                }

                newRelease.finalized = null;
                newRelease.added = null;
                newRelease.addedBy = null;
                newRelease.deletedBy = null;
                
                if (rmo.HasRelease(newRelease.id))
                {
                    errors.Add("This release already exists.");
                }
                else
                {
                    foreach (XmlNode itemNode in doc.SelectNodes("//items/item"))
                    {
                        var item = rmo.getReleaseItem(itemNode);
                        if (rmo.HasReleaseItem(item.id))
                        {
                            errors.Add("Item already exists: " + item.id);
                        }
                        else
                        {
                            var newItem = Utilities.getProspectiveReleaseItemByWebdav(item.WEBDAV_URL, rmo.getCoreServiceClient());
                            if (newItem == null)
                            {
                                errors.Add(item.TITLE + " (" + item.URI + ") could not be found.");
                            }
                            else
                            {
                                rmo.addToRelease(newItem, newRelease.id);
                            }
                        }
                    }
                }

                if (errors.Count == 0)
                {
                    rmo.saveRelease(newRelease);
                    return new { result = "success", message = "Release imported." };
                }
                else
                {
                    return new { result = "error", message = "Error: " + String.Join(" ", errors.ToArray()) };
                }
            }
            catch (Exception e)
            {
                return new { result = "error", message = "The XML could not be loaded: " + e.Message + ", " + e.StackTrace };
            }
        }
    }
}
