using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.IO;
using System.Resources;
using System.Xml;
namespace ReleaseManager
{
    public partial class ManageReleases : System.Web.UI.Page
    {
        Panel releaseListPanel = new Panel();
        CheckBox showAll = new CheckBox();
        bool showDeletedReleases = false;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request["showItemsInRelease"] != null && !IsPostBack)
            {
                showItemsInRelease(Request["showItemsInRelease"]);
            }
            else
            {
                showDeletedReleases = true; // (Session.Contents["showDeletedReleases"] != null) ? (bool)Session.Contents["showDeletedReleases"] : false;
                showAll.ID = "showAll";
                showAll.Text = "Include finalized releases";
                showAll.AutoPostBack = true;
                showAll.CheckedChanged += new EventHandler(showAll_CheckedChanged);
                
                //TODO: fix this
                showAll.Visible = false;

                showReleases();
                ReleaseItems.CssClass = "";
                txtExportSettings.Attributes["onclick"] = "this.select();";
                txtImportSettings.Attributes["onclick"] = "this.select();";
                exportXml.Attributes["onclick"] = "this.select();";
            }
            
        }

        void btnClearRelease_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string release = btn.CommandArgument;

            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            rmRep.finalizeRelease(release);

            showReleases();
        }

        void btnCreateImportExportSettingsXML_Click(object sender, EventArgs e)
        {
            PanelImportExport.Visible = true;
            LinkButton btn = (LinkButton)sender;
            string releaseId = btn.CommandArgument;

            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);

            var release = rmRep.getRelease(releaseId);

            string response = rmRep.createExportSettingsCP(releaseId);
            txtExportSettings.Text = response;
            txtImportSettings.Text = rmRep.createImportSettingsFromExportSettings(response, releaseId);

            importExportName.Controls.Clear();
            importExportName.Controls.Add(new LiteralControl(release.title));

            Literal lit = new Literal();
            lit.Text = "<br /><br />";

            //Create download links
            HyperLink exportHyperlink = new HyperLink();
            exportHyperlink.NavigateUrl = @"temp/" + ReleaseManagerRepository.getFileNameRelease(releaseId, "export");
            exportHyperlink.Text = "Download export settings";

            //PanelImportExport.Controls.Add(lit); PanelImportExport.Controls.Add(exportHyperlink);
            panelExportHyperlink.Controls.Add(exportHyperlink);

            HyperLink importHyperlink = new HyperLink();
            importHyperlink.NavigateUrl = @"temp/" + ReleaseManagerRepository.getFileNameRelease(releaseId, "import");
            importHyperlink.Text = "Download import settings";

            finalizeRelease.CommandArgument = releaseId;
            finalizeRelease.Visible = !release.isDeleted();

            exportRelease.CommandArgument = releaseId;
            exportRelease.Visible = release.isDeleted();

            //PanelImportExport.Controls.Add(lit); PanelImportExport.Controls.Add(importHyperlink);
            panelImportHyperlink.Controls.Add(importHyperlink);
        }

        protected void exportReleaseClick(object sender, EventArgs e)
        {
            var rmRep = new ReleaseManagerRepository(Server, Request);
            ExportReleasePanel.Visible = true;
            var button = (LinkButton)sender;

            exportXml.Text = rmRep.getExportXml(button.CommandArgument);
        }

        private void showReleases()
        {
            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            //showReleases(rmRep.getAllReleases());
            var releases = rmRep.getReleases();
            rmRep.moveFinalizedReleasesToEnd(ref releases);
            showReleases(releases);
        }

        private void showReleases(List<Release> releases)
        {
            PanelReleases.Controls.Remove(releaseListPanel);
            releaseListPanel.ID = "releaseList";
            releaseListPanel.Controls.Clear();
            //PanelReleases.Controls.Add(new LiteralControl(""));
            var i = 0;
            foreach (var release in releases)
            {
                if (release.isDeleted() && !showDeletedReleases)
                {
                    continue;
                }

                var releasePanel = new Panel();
                var itemContent = "<div class=\"item" + (release.isDeleted() ? " deleted" : "") + "\">" + release.title
                    + " (" + release.items.Count + " item" + (release.items.Count != 1 ? "s" : String.Empty) + ")";

                itemContent += "<br /><span class=\"info\">Added by " + release.addedBy + " on " + getNiceDate(release.added) + ".";
                if (release.isDeleted())
                {
                    itemContent += " Finalized by " + release.deletedBy + " on " + getNiceDate(release.finalized) + ".";
                }
                itemContent += "</span>";

                itemContent += "</div>";
                releasePanel.Controls.Add(new LiteralControl(itemContent));

                var controlPanel = new Panel();
                controlPanel.CssClass = "controls";

                var viewItems = new LiteralControl("<input type=\"button\" class=\"viewItems\" data-releaseId=\""+release.id+"\" value=\"View Items\" />");
                controlPanel.Controls.Add(viewItems);


                ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
                if(rmRep.releaseContainsErrors(release))
                {
                    var btnViewReleaseItemsButton = new LinkButton();
                    btnViewReleaseItemsButton.Text = "Errors";
                    btnViewReleaseItemsButton.CommandArgument = release.id;
                    btnViewReleaseItemsButton.CssClass = "errorButtonViewItems";
                    btnViewReleaseItemsButton.Click += new EventHandler(ViewItemsButton_Click);

                    controlPanel.Controls.Add(btnViewReleaseItemsButton);
                }
                else
                {
                    var btnCreateImportExportSettingsXML = new LinkButton();
                    btnCreateImportExportSettingsXML.Text = "Export";
                    btnCreateImportExportSettingsXML.CssClass = "createSettings";
                    btnCreateImportExportSettingsXML.CommandArgument = release.id;
                    btnCreateImportExportSettingsXML.Click += new EventHandler(btnCreateImportExportSettingsXML_Click);

                    controlPanel.Controls.Add(btnCreateImportExportSettingsXML);
                }


                releasePanel.CssClass = "row" + (i % 2 == 1 ? " odd" : "");
                
                releasePanel.Controls.Add(controlPanel);
                releasePanel.Controls.Add(new LiteralControl("<div style=\"clear:both\"></div>"));
                releaseListPanel.Controls.Add(releasePanel);
                i++;
            }
            PanelReleases.Controls.Add(releaseListPanel);

            showAll.Checked = showDeletedReleases;
            PanelReleases.Controls.Add(showAll);

            hideAllPanels();
            PanelReleases.Visible = true;
        }

        private string getNiceDate(DateTime? dateTime)
        {
            return String.Format("{0:d}", dateTime);
        }

        void showAll_CheckedChanged(object sender, EventArgs e)
        {
            //Session.Contents["showDeletedReleases"] = ((CheckBox)sender).Checked;
            showDeletedReleases = true; // (bool)Session.Contents["showDeletedReleases"];
            showReleases();
        }

        protected void finalizeReleaseClickYes(object sender, EventArgs e)
        {
            LinkButton button = (LinkButton)sender;
            string release = button.CommandArgument;

            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            rmRep.finalizeRelease(release);
            
            showReleases();
        }

        protected void finalizeReleaseClickNo(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            string release = button.CommandArgument;

            hideAllPanels();
            showReleases();
        }

        //void viewItems_Click(object sender, EventArgs e)
        //{
        //    Button button = (Button)sender;
        //    string release = button.CommandArgument;

        //    Console.Out.WriteLine("here1");
        //    updateWebDavsInRelease(release);
        //    showItemsInRelease(release);
        //}

        void ViewItemsButton_Click(object sender, EventArgs e)
        {
            LinkButton button = (LinkButton)sender;
            string release = button.CommandArgument;

            System.IO.File.WriteAllText(@"C:\Users\Administrator\Desktop\text.txt", "testesttest");
            Console.Out.WriteLine("here2");
            //logError("test_log_1");
            updateWebDavsInRelease(release);
            showItemsInRelease(release);
        }

        void updateWebDavsInRelease(string releaseId)
        {
            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            var release = rmRep.getRelease(releaseId);

            foreach (var item in release.items)
            {
                if (rmRep.stillExists(item))
                {
                    rmRep.updateItemDetails_New(item, releaseId);
                }
            }

            //XmlNode itemNode = db.SelectSingleNode("//items/item[@uri='" + item.URI + "'][@release='" + release + "']");
        }

        void showItemsInRelease(string releaseId)
        {
            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            var release = rmRep.getRelease(releaseId);

            PanelReleases.Visible = false;
            PanelFinalizeRelease.Visible = false;
            PanelImportExport.Visible = false;

            ReleaseItems.Visible = true;
            ReleaseItems.CssClass = "withItems";
            
            ReleaseItems.Controls.Clear();
            ReleaseItems.Controls.Add(new LiteralControl("<h4>Items in " + release.title + "</h4>"));
            var backButton = new Button();
            backButton.CssClass = "primary";
            backButton.Text = "Back to Releases";
            backButton.ID = "backButton";
            backButton.Click += new EventHandler(backButton_Click);

           

            //var backButton = new LiteralControl("<input type=\"button\" id=\"backButton\" class=\"primary\" value=\"Back to Releases\" />");

            ReleaseItems.Controls.Add(backButton);
           

            var notesPanel = new Panel();
            notesPanel.ID = "notesPanel";

            var notesBox = new TextBox();
            notesBox.ID = "notesBox";
            notesBox.Columns = 60;
            notesBox.Rows = 1;
            if (release.note.Length > 0)
            {
                notesBox.Text = release.note;
            }
            else
            {
                notesBox.Text = "Notes:";
                notesBox.CssClass = "empty";
            }
            notesBox.TextMode = TextBoxMode.MultiLine;

            notesPanel.Controls.Add(notesBox);
            notesPanel.Controls.Add(new LiteralControl("<input id=\"saveNotes\" type=\"button\" value=\"Save Notes\" />"));
            notesPanel.Controls.Add(new LiteralControl("<div class=\"status\"></div>"));
            
            ReleaseItems.Controls.Add(notesPanel);
            var releaseIdField = new HiddenField();
            releaseIdField.Value = release.id;
            releaseIdField.ID = "releaseId";
            ReleaseItems.Controls.Add(releaseIdField);

            ////////////////////////////////////////////////////

            var bundlesPanel = new Panel();
            bundlesPanel.ID = "bundlesPanel";

            var bundleFolderLabel = new Label();
            bundleFolderLabel.ID = "bundleFolderLabel";
            bundleFolderLabel.Text = "Bundle Folder:";

            var bundleFolderBox = new TextBox();
            bundleFolderBox.ID = "bundleFolderBox";
            bundleFolderBox.Columns = 28;
            bundleFolderBox.Rows = 1;
            bundleFolderBox.TextMode = TextBoxMode.SingleLine;

            var bundlePrefixLabel = new Label();
            bundlePrefixLabel.ID = "bundlePrefixLabel";
            bundlePrefixLabel.Text = "Bundle Prefix:";

            var bundlePrefixBox = new TextBox();
            bundlePrefixBox.ID = "bundlePrefixBox";
            bundlePrefixBox.Columns = 28;
            bundlePrefixBox.Rows = 1;
            bundlePrefixBox.TextMode = TextBoxMode.SingleLine;
            //bundlePrefixBox.

            var bundlesButton = new Button();
            bundlesButton.CssClass = "primary";
            bundlesButton.Text = "Create Bundles";
            bundlesButton.ID = "bundlesButton";
            backButton.Click += new EventHandler(bundlesButton_Click);
            //System.IO.File.WriteAllText(@"C:\Users\Administrator\Desktop\text.txt", "testesttest");

            bundlesPanel.Controls.Add(bundleFolderLabel);
            bundlesPanel.Controls.Add(bundleFolderBox);
            bundlesPanel.Controls.Add(bundlePrefixLabel);
            bundlesPanel.Controls.Add(bundlePrefixBox);
            bundlesPanel.Controls.Add(bundlesButton);

            ReleaseItems.Controls.Add(bundlesPanel);

            ////////////////////////////////////////////////////////

            
            if (release.items.Count == 0)
            {
                ReleaseItems.Controls.Add(new LiteralControl("<div class=\"first row\"><em>This release is empty.</em></div>"));
            }
            else
            {
                release.items.Sort(CompareReleaseItems);
                var i = 0;
                foreach (var item in release.items)
                {
                    if (rmRep.stillExists(item))
                    {
                        // ensures the item webdav path is still OK
                        rmRep.updateItemDetails(item, release.id);



                        ////rmRep.updateItemDetails_New(item, release.id);




                        var itemHtml = HttpUtility.UrlDecode(item.WEBDAV_URL).Replace("/webdav/", String.Empty);
                        if (item.possiblyConflictsWith.Count > 0 || item.definitelyConflictsWith.Count > 0)
                        {
                            itemHtml += "<br /><span class=\"info\">Also added to ";
                            foreach (var otherRelease in item.possiblyConflictsWith)
                            {
                                itemHtml += " " + otherRelease.title + ",";
                            }
                            foreach (var otherRelease in item.definitelyConflictsWith)
                            {
                                itemHtml += " " + otherRelease.title + ",";
                            }
                            itemHtml = itemHtml.Substring(0, itemHtml.Length - 1);
                            itemHtml += "</span>";
                        }

                        var viewHtml = "<input type=\"button\" class=\"viewItem\" value=\"Open\" />";
                        var cssClass = (i == 0 ? " first" : "") + ((i % 2) == 1 ? " odd" : "");
                        cssClass += item.possiblyConflictsWith.Count > 0 ? " possibleConflicts" : "";
                        cssClass += item.definitelyConflictsWith.Count > 0 ? " definiteConflicts" : "";
                        var row = "<div class=\"row" + cssClass + "\" data-tcmuri=\"" + item.URI + "\">";
                        row += "<div class=\"item\">" + itemHtml + "</div>";
                        row += "<div class=\"controls\">";
                        row += viewHtml;
                        row += "<a class=\"remove\" href=\"removeFromRelease.aspx?release=" + HttpUtility.UrlEncode(releaseId) + "&uri=" + HttpUtility.UrlEncode(item.URI) + "\">Remove</a>";
                        row += "</div>";
                        row += "<div style=\"clear:both\"></div>";
                        row += "</div>";
                        ReleaseItems.Controls.Add(new LiteralControl(row));
                        i++;
                    }
                    else
                    {
                        // if we've removed the item, let the user know that it's gone.
                        var itemHtml = HttpUtility.UrlDecode(item.WEBDAV_URL).Replace("/webdav/", String.Empty);
                        if (item.possiblyConflictsWith.Count > 0 || item.definitelyConflictsWith.Count > 0)
                        {
                            itemHtml += "<br /><span class=\"info\">The item " + item.TITLE + " no longer exists in the CMS ";
                            itemHtml = itemHtml.Substring(0, itemHtml.Length - 1);
                            itemHtml += "</span>";
                        }

                        var cssClass = (i == 0 ? " first" : "") + ((i % 2) == 1 ? " odd" : "");
                        cssClass += " itemMissing";
                        var row = "<div class=\"row" + cssClass + "\" data-tcmuri=\"" + item.URI + "\">";
                        row += "<div class=\"item\"><img src=\"Themes\\images\\error.png\" /> " + itemHtml + "</div>";
                        row += "<div class=\"controls\">";
                        row += "<a class=\"remove\" href=\"removeFromRelease.aspx?release=" + HttpUtility.UrlEncode(releaseId) + "&uri=" + HttpUtility.UrlEncode(item.URI) + "\">Remove</a>";
                        row += "</div>";
                        row += "<div style=\"clear:both\"></div>";
                        row += "</div>";
                        ReleaseItems.Controls.Add(new LiteralControl(row));
                        i++;



                        //var cssClass = (i == 0 ? " first" : "") + ((i % 2) == 1 ? " odd" : "");
                        //var row = "<div class=\"row" + cssClass + " itemMissing\" data-tcmuri=\"" + item.URI + "\">";
                        //row += "<div class=\"item\">" + item.TITLE + " [" + item.URI + "] - Not found in CMS, removed from release</div>";
                        //row += "<div style=\"clear:both\"></div>";
                        //row += "</div>";
                        //ReleaseItems.Controls.Add(new LiteralControl(row));

                        //rmRep.removeFromRelease(item.URI, release.id);

                    }
                }
            }
        }

        void backButton_Click(object sender, EventArgs e)
        {
            hideAllPanels();
            showReleases();
        }

        void bundlesButton_Click(object sender, EventArgs e)
        {
            ////hideAllPanels();
            ////showReleases();
        }

        private static int CompareReleaseItems(ReleaseItem item1, ReleaseItem item2)
        {
            return item1.WEBDAV_URL.CompareTo(item2.WEBDAV_URL);
        }

        //void removeItemFromRelease_Click(object sender, EventArgs e)
        //{
        //    PanelReleases.Controls.Add(new LiteralControl("test"));

        //    ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
        //    Button button = (Button)sender;
        //    var paramParts = button.CommandArgument.Split(',');
        //    var release = paramParts[0];
        //    var item = paramParts[1];
        //    showItemsInRelease("asdf");
        //}

        void btnDeleteRelease_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string release = btn.CommandArgument;
            try
            {
                ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
                rmRep.removeRelease(release);
                btn.Parent.Controls.Remove(btn);
                showReleases();
            }
            catch (Exception)
            {
            }
        }

        protected void btnAddNewRelease_Click(object sender, EventArgs e)
        {
            if (!txtNewRelease.Text.Equals(string.Empty))
            {
                ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
                rmRep.addRelease(txtNewRelease.Text);
                showReleases();
            }
        }

        protected void CheckBoxListDependencys_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Response.Write(sender.GetType().ToString());
            foreach (ListItem item in CheckBoxListDependencys.Items)
            {
                Response.Write(item.Text + " - " + item.Selected);
            }
        }

        protected void finalizeReleaseClick(object sender, EventArgs e)
        {
            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            LinkButton button = (LinkButton)sender;
            string releaseId = button.CommandArgument;
            var release = rmRep.getRelease(releaseId);
            hideAllPanels();
            finalizeReleaseYes.CommandArgument = releaseId;

            PanelFinalizeRelease.Visible = true;
        }

        protected void addReleaseClick(object sender, EventArgs e)
        {
            LinkButton button = (LinkButton)sender;

            hideAllPanels();
            addReleaseForm.Visible = true;
        }

        void hideAllPanels()
        {
            PanelFinalizeRelease.Visible = false;
            PanelReleases.Visible = false;
            ReleaseItems.Visible = false;
            PanelImportExport.Visible = false;
            ExportReleasePanel.Visible = false;
            addReleaseForm.Visible = false;
        }

        protected void cancelAddNewRelease_Click(object sender, EventArgs e)
        {
            hideAllPanels();
            showReleases();
        }
    }
}
