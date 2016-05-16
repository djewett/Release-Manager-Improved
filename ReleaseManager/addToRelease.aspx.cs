using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using Tridion.ContentManager.CoreService.Client;
using System.Xml;
//using Tridion.Web.UI.Models.TCM54;

namespace ReleaseManager
{
    public partial class AddToRelease : System.Web.UI.Page
    {
        //static CoreService2010Client tridionClient = new CoreService2010Client("basicHttp_2010");
        
        SessionAwareCoreServiceClient tridionClient; //= new ReleaseManagerExtension.CoreService2011.CoreServiceClient("basicHttp");

        public AddToRelease(){
            //rmRep = new ReleaseManagerRepository(Server, Request);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            tridionClient = rmRep.getCoreServiceClient();

            string uri = getUri();
            if (uri == "") { return; }

            panelReleasesItemIsIn.Controls.Clear();
            panelReleasesItemIsNotIn.Controls.Clear();
            ReleaseItem releaseItem = null;

            if (uri.Contains(",")) // multiple items selected
            {
                itemLabelPlaceholder.Controls.Clear();
                itemLabelPlaceholder.Controls.Add(new LiteralControl("these items"));
            }
            else
            {
                releaseItem = Utilities.getProspectiveReleaseItem(uri, tridionClient);
            }

            var releases = rmRep.getReleases();
            releases.Sort();
            foreach (var release in releases)
            {
                if (!release.isDeleted())
                {
                    var isChecked = releaseItem != null &&
                        release.containsItem(releaseItem.URI);
                    CheckBox itemInRelease = getReleaseCheckbox(release, isChecked);
                    allReleases.Controls.Add(itemInRelease);
                    allReleases.Controls.Add(new LiteralControl("<div class=\"spacer\"></div>"));
                }
            }

            lblMessage.Visible = !lblMessage.Text.Equals(String.Empty);
        }

        /// <summary>
        /// Creates a checkbox for a release with an item.
        /// </summary>
        /// <param name="release"></param>
        /// <param name="selected"></param>
        /// <returns></returns>
        private CheckBox getReleaseCheckbox(Release release, bool selected)
        {
            CheckBox checkbox = new CheckBox();
            checkbox.Checked = selected;
            checkbox.Text = release.title;
            checkbox.AutoPostBack = true;
            checkbox.InputAttributes.Add("id", release.id);
            checkbox.CheckedChanged += new EventHandler(itemInRelease_CheckedChanged);
            return checkbox;
        }

        void itemInRelease_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
            string uri = getUri();
            if (uri == "") { return; }
            string release = checkbox.InputAttributes["id"]; // checkbox.Text;
            string releaseName = checkbox.Text;

            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);

            if (uri.Contains(","))
            {
                // handle multiple items selected
                var items = uri.Split(',');
                foreach (var item in items)
                {
                    var rmObject = Utilities.getProspectiveReleaseItem(item, tridionClient);
                    if (checkbox.Checked)
                    {
                        rmRep.addToRelease(rmObject, release);
                        lblMessage.Text = "<strong>Items added to release " + releaseName + ".</strong>";
                    }
                    else
                    {
                        rmRep.removeFromRelease(item, release);
                        lblMessage.Text = "<strong>Items removed from release " + releaseName + ".</strong>";
                    }
                }
            }
            else
            {
                var rmObject = Utilities.getProspectiveReleaseItem(uri, tridionClient);
                if (checkbox.Checked)
                {
                    //Add to this release                
                    rmRep.addToRelease(rmObject, release);
                    lblMessage.Text = "<strong>Item added to release: " + releaseName + ".</strong>";
                }
                else
                {
                    //Remove from this release
                    rmRep.removeFromRelease(uri, release);
                    lblMessage.Text = "<strong>Item removed from release: " + releaseName + ".</strong>";
                }
            }
            lblMessage.Text += "<input type=\"button\" value=\"Ok, close this\" onclick=\"window.close();\"/>";
            lblMessage.Visible = true;
            cancelAdd.Visible = false;
        }

        /// <summary>
        /// Retrieves the uri parameter from the request object
        /// </summary>
        /// <returns></returns>
        private string getUri()
        {
            if (Request["uris"] == null) { return ""; }

            return Request["uris"].ToString();
        }

        //protected void Button1_Click(object sender, EventArgs e)
        //{
        //    rm.createImportExportSettingsCP("Raboweb Release 9.1");
        //}
    }
}
