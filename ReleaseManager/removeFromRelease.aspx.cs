using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ReleaseManager;

namespace ReleaseManager
{
    public partial class removeFromRelease : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e) 
        {
            var rmRep = new ReleaseManagerRepository(Server, Request);
            if (String.IsNullOrEmpty(Request["uri"]) || String.IsNullOrEmpty(Request["release"])) return;

            var rmo = rmRep.getReleaseItem(Request["uri"], Request["release"]);
            var release = rmRep.getRelease(Request["release"]);
            itemName.Controls.Clear();
            itemName.Controls.Add(new LiteralControl("<strong>" + rmo.TITLE + "</strong> (" + HttpUtility.UrlDecode(rmo.WEBDAV_URL) +")") );

            releaseName.Controls.Clear();
            releaseName.Controls.Add(new LiteralControl("<strong>" + release.title + "</strong>"));
        }

        protected void noButton_Click(object sender, EventArgs e)
        {
            sendBack();
        }

        protected void yesButton_Click(object sender, EventArgs e)
        {
            var rmRep = new ReleaseManagerRepository(Server, Request);
            rmRep.removeFromRelease(Request["uri"], Request["release"]);

            sendBack();
        }

        void sendBack()
        {
            Response.Redirect("manageReleases.aspx?showItemsInRelease=" + Request["release"]);
        }
    }
}