using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.IO;
using System.Resources;
using System.Xml;
using System.Web.UI.HtmlControls;
using Tridion.ContentManager.CoreService.Client;
namespace ReleaseManager
{
    public partial class ManageReleases : System.Web.UI.Page
    {
        Panel releaseListPanel = new Panel();
        CheckBox showAll = new CheckBox();
        bool showDeletedReleases = false;

        Label createBundlesErrorMessageLabel = new Label();
                
        // DJ
        //private Button bundlesButton = new Button();
        private LiteralControl bundlesLiteralControl;
        private Button zzzButton;

        protected void Page_Load(object sender, EventArgs e)
        {
            zzzButton = new Button();
            ////var yyyButton = new Button();
            zzzButton.Text = "zzz";
            zzzButton.CssClass = "zzz";
            zzzButton.Click += new EventHandler(bundlesButton_Click);



            createBundlesErrorMessageLabel.Text = "Invalid Input(s)";
            createBundlesErrorMessageLabel.ID = "createBundlesErrorMessage";
            createBundlesErrorMessageLabel.ForeColor = System.Drawing.Color.Red;



            if (Request["showItemsInRelease"] != null)// && !IsPostBack)
            {
                showItemsInRelease(Request["showItemsInRelease"]); //, yyyButton);
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
                //loadReleases();
            }
        }

        private void loadReleases()
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

            updateWebDavsInReleaseData(releaseId);

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
            CreateBundlesPanel.Visible = false;

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

                var viewItems = new LiteralControl("<input type=\"button\" class=\"viewItems\" data-releaseId=\"" + release.id + "\" value=\"View Items\" />");
                controlPanel.Controls.Add(viewItems);

                //var createBundsLit = new LiteralControl("<input type=\"button\" id=\"createBundles\" class=\"createBundles\" data-releaseId=\"" + release.id + "\" value=\"Create Bundles\" />");
                //controlPanel.Controls.Add(createBundsLit);

                //zzzButton = new Button();
                //////var yyyButton = new Button();
                //zzzButton.Text = "zzz";
                //zzzButton.CssClass = "zzz";
                //zzzButton.Click += new EventHandler(bundlesButton_Click);
                //controlPanel.Controls.Add(zzzButton);

                //Button bundlesButton = new Button();
                //bundlesButton.CssClass = "bundlesButton";
                //bundlesButton.Text = "Create Bundles (OLD)";
                //bundlesButton.ID = "bundlesButton_" + release.id;
                //bundlesButton.Type
                //bundlesButton.
                //bundlesButton.Click += new EventHandler(bundlesButton_Click);
                //controlPanel.Controls.Add(bundlesButton);
                //releaseListPanel.Controls.Add(bundlesButton);
                //PanelReleases.Controls.Add(bundlesButton);
                //ReleaseItems.Controls.Add(bundlesButton);


                ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
                if (rmRep.releaseContainsErrors(release))
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




            Console.Out.WriteLine("here2");
            //logError("test_log_1");
            updateWebDavsInRelease(release);

            //updateWebDavsInReleaseData(release);


            //Button yyyButton = new Button();
            ////var yyyButton = new Button();
            //yyyButton.Text = "yyy";
            //yyyButton.CssClass = "xxx";
            //yyyButton.Click += new EventHandler(bundlesButton_Click);
            //yyyButton.


            showItemsInRelease(release); //, yyyButton);
        }

        void Xxx_Click(object sender, EventArgs e)
        {
            LinkButton button = (LinkButton)sender;
            button.Enabled = false;
            //System.IO.File.WriteAllText(@"C:\Users\Administrator\Desktop\text.txt", "here here 888");
        }

        void updateWebDavsInRelease(string releaseId)
        {
            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            var release = rmRep.getRelease(releaseId);

            foreach (var item in release.items)
            {
                if (rmRep.stillExists(item))
                {
                    rmRep.updateItemDetails(item, releaseId);
                }
            }

            //XmlNode itemNode = db.SelectSingleNode("//items/item[@uri='" + item.URI + "'][@release='" + release + "']");
        }


        // dj - May 2016
        void updateWebDavsInReleaseData(string releaseId)
        {
            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            var release = rmRep.getRelease(releaseId);
            rmRep.updateItemDetailsInReleaseData(release);

            //System.IO.File.WriteAllText(@"C:\Users\Administrator\Desktop\text1.txt", "releaseId: " + releaseId);
        }


        // DJ
        void showBundles(string releaseId)
        {
            //System.IO.File.WriteAllText(@"C:\Users\Administrator\Desktop\text1.txt", "bundlebundlebundle");
        }


        private void showItemsInRelease(string releaseId) //, Button yyyButton)
        {
            CreateBundlesPanel.Visible = true;

            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            var release = rmRep.getRelease(releaseId);

            PanelReleases.Visible = false;
            PanelFinalizeRelease.Visible = false;
            PanelImportExport.Visible = false;

            ReleaseItems.Visible = true;
            ReleaseItems.CssClass = "withItems";

            ReleaseItems.Controls.Clear();
            ReleaseItems.Controls.Add(new LiteralControl("<h4>Items in " + release.title + "</h4>"));
            //var backButton = new Button();
            //backButton.CssClass = "primary";
            //backButton.Text = "Back to Releases";
            //backButton.ID = "backButton";
            //backButton.Click += new EventHandler(backButton_Click);



            var backButton = new LiteralControl("<input type=\"button\" id=\"backButton\" class=\"primary\" value=\"Back to Releases\" />");

            //var backButton = new Button();
            //backButton.Text = "Back to Releases";
            //backButton.ID = "backButton";
            //backButton.Attributes.Add("class", "primary");
            //backButton.Click += new System.EventHandler(backToReleases_click);

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

            //var bundlesPanel = new Panel();
            //bundlesPanel.ID = "bundlesPanel";

            //var bundleFolderLabel = new Label();
            //bundleFolderLabel.ID = "bundleFolderLabel";
            //bundleFolderLabel.Text = "Bundle Folder:";

            //var bundleFolderBox = new TextBox();
            //bundleFolderBox.ID = "bundleFolderBox";
            //bundleFolderBox.Columns = 28;
            //bundleFolderBox.Rows = 1;
            //bundleFolderBox.TextMode = TextBoxMode.SingleLine;

            //var bundlePrefixLabel = new Label();
            //bundlePrefixLabel.ID = "bundlePrefixLabel";
            //bundlePrefixLabel.Text = "Bundle Prefix:";

            //var bundlePrefixBox = new TextBox();
            //bundlePrefixBox.ID = "bundlePrefixBox";
            //bundlePrefixBox.Columns = 28;
            //bundlePrefixBox.Rows = 1;
            //bundlePrefixBox.TextMode = TextBoxMode.SingleLine;
            //bundlePrefixBox.

            //var bundlesButton = new Button();
            //bundlesButton.CssClass = "bundlesButton";
            //bundlesButton.Text = "Create Bundles";
            //bundlesButton.ID = "bundlesButtonXXX";
            //////bundlesButton.
            //bundlesButton.Click += new EventHandler(bundlesButton_Click);
            ////System.IO.File.WriteAllText(@"C:\Users\Administrator\Desktop\text.txt", "here here 123");

            //bundlesPanel.Controls.Add(bundleFolderLabel);
            //bundlesPanel.Controls.Add(bundleFolderBox);
            //bundlesPanel.Controls.Add(bundlePrefixLabel);
            //bundlesPanel.Controls.Add(bundlePrefixBox);
            //bundlesPanel.Controls.Add(bundlesButton);

            //ReleaseItems.Controls.Add(bundlesButton);
            ////ReleaseItems.Controls.Add(bundlesPanel);

            //var xxxButton = new Button();
            //xxxButton.Text = "XXX";
            ////btnViewReleaseItemsButton.CommandArgument = release.id;
            //xxxButton.CssClass = "xxx";
            ////xxxButton.
            //xxxButton.Click -= new EventHandler(bundlesButton_Click);
            //ReleaseItems.Controls.Add(xxxButton);

            //ReleaseItems.Controls.Add(zzzButton);

            //ReleaseItems.Controls.Add(yyyButton);

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
                        //rmRep.updateItemDetails(item, release.id); // <-- Commented out July 14, 2016

                        //rmRep.updateItemDetailsInReleaseData(release); // <-- Commented out July 14, 2016

                        ////rmRep.updateItemDetails_New(item, release.id);

                        string itemFullPath = HttpUtility.UrlDecode(item.WEBDAV_URL).Replace("/webdav/", String.Empty);
                        var itemHtml = itemFullPath;
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


                        //dynamicbutton.


                        //string refreshItemButtonHtml = "";
                        //if (rmRep.isItemRenamed(item, release.id))
                        //{
                        //    refreshItemButtonHtml = "<input type=\"submit\" id=\"foo\" class=\"refreshRenamedButton\" runat=\"server\" onserverclick=\"createBundClick\" value=\"Refresh\" />";
                        //    //refreshItemButtonHtml = "<asp:Button runat=\"server\" OnClick=\"createBundClick\" Text=\"Refresh\" />";
                        //    //refreshItemButtonHtml = dynamicbutton.
                        //    cssClass += " itemRenamed";
                        //}

                        if (rmRep.isItemRenamed(item, release.id))
                        {
                            cssClass += " itemRenamed";
                        }

                        var row = "<div class=\"row" + cssClass + "\" data-tcmuri=\"" + item.URI + "\">";
                        row += "<div class=\"item\">" + itemHtml + "</div>";
                        row += "<div class=\"controls\">";
                        //row += refreshItemButtonHtml;
                        row += viewHtml;
                        row += "<a class=\"remove\" href=\"removeFromRelease.aspx?release=" + HttpUtility.UrlEncode(releaseId) + "&uri=" + HttpUtility.UrlEncode(item.URI) + "\">Remove</a>";
                        row += "</div>";
                        row += "<div style=\"clear:both\"></div>";
                        row += "</div>";

                        //var itemControls = new LiteralControl(row);
                        //itemControls.Add(dynamicbutton);

                        //ReleaseItems.Controls.Add(new LiteralControl(row));

                        var itemControls = new LiteralControl(row);
                        
                        ReleaseItems.Controls.Add(itemControls);

                        //ReleaseItems.Controls.Remove()

                        if (rmRep.isItemRenamed(item, release.id))
                        {
                            //Label itemRenamedWarningLabel = new Label();
                            //itemRenamedWarningLabel.Text = "Item has been moved or renamed.";
                            //itemRenamedWarningLabel.ForeColor = System.Drawing.Color.Gray;
                            //itemRenamedWarningLabel.Attributes.Add("class", "itemRenamedWarningLabel");

                            //string itemRenamedWarningLabelHtml = "<span class=\"itemRenamedWarningLabel\" style=\"color:Gray;\">The item has been moved or renamed ";
                            //itemRenamedWarningLabelHtml = itemRenamedWarningLabelHtml.Substring(0, itemRenamedWarningLabelHtml.Length - 1);
                            //itemRenamedWarningLabelHtml += "</span>";

                            //ReleaseItems.Controls.Add(itemRenamedWarningLabel);
                            //ReleaseItems.Controls.Add(new LiteralControl(itemRenamedWarningLabelHtml));

                            Button renameRefreshButton = new Button();
                            renameRefreshButton.Attributes.Add("class", "renameRefreshButton");
                            renameRefreshButton.Attributes.Add("data-tcmuri", item.URI);
                            renameRefreshButton.Attributes.Add("lineId", itemControls.UniqueID);
                            renameRefreshButton.Attributes.Add("itemFullPath", itemFullPath);
                            //renameRefreshButton.Attributes.Add("itemRenamedWarningId", itemRenamedWarningLabel.UniqueID);
                            renameRefreshButton.Click += new System.EventHandler(renameRefreshClick);
                            renameRefreshButton.Text = "Refresh";

                            ReleaseItems.Controls.Add(renameRefreshButton);
                        }

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

        //void backButton_Click(object sender, EventArgs e)
        //{
        //    hideAllPanels();
        //    showReleases();
        //}

        void bundlesButton_Click(object sender, EventArgs e)
        {
            ////hideAllPanels();
            ////showReleases();

            //const string BundleNamespace = @"http://www.sdltridion.com/ContentManager/Bundle";
            //SchemaData bundleTypeSchema = getCoreServiceClient().GetVirtualFolderTypeSchema(BundleNamespace);
            //string bundleSchemaId = bundleTypeSchema.Id;
            //var bundle = (VirtualFolderData)getCoreServiceClient().GetDefaultData(Tridion.ContentManager.CoreService.Client.VirtualFolderData, "tcm:5-2199-2", new ReadOptions());
            //bundle.Configuration = "<Bundle xmlns=\"http://www.sdltridion.com/ContentManager/Bundle\"><Items /></Bundle>";
            //bundle.TypeSchema = new LinkToSchemaData { IdRef = bundleSchemaId };
            //bundle.Title = "DJsNewBund";
            //getCoreServiceClient().Create(bundle, new ReadOptions());

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





        private string convertPathToWebDav(string path)
        {
            // First replace all / with %2F, then replace all \ with /.
            var webdav = path.Replace("/", "%2F").Replace('\\', '/');
            // Don't URL encode the string because that will replace / characters, which we need.
            //webdav = HttpUtility.UrlEncode(webdav);
            webdav = "/webdav" + webdav;
            return webdav;
        }

        private void retrieveBundleInputs(ref string bundleFolder, ref string bundlePrefix)
        {
            // TODO: Figure out a better way to retrieve values from text boxes:
            int count = 0;
            foreach (Control c in CreateBundlesPanel.Controls)
            {
                if (c.GetType().ToString() == "System.Web.UI.HtmlControls.HtmlInputText" && (count == 0))
                {
                    // The bundle folder is the first System.Web.UI.HtmlControls.HtmlInputText.
                    bundleFolder = ((HtmlInputText)c).Value;
                    count++;
                }
                else if (c.GetType().ToString() == "System.Web.UI.HtmlControls.HtmlInputText" && (count == 1))
                {
                    // The bundle prefix is the second System.Web.UI.HtmlControls.HtmlInputText.
                    bundlePrefix = ((HtmlInputText)c).Value;
                    count++;
                }
            }
        }

        private string validateFolderInput(string bundleFolderInput, ReleaseManagerRepository rmRep)
        {
            string bundleFolderPath = "";

            var client = rmRep.getCoreServiceClient();

            try
            {
                CreateBundlesPanel.Controls.Remove(createBundlesErrorMessageLabel);

                // Accept a path, webdav or tcm (with or with the "tcm:" prefix)
                string bundleFolderTcm = "";
                //TODO: Add a case for when the bundleFolder starts with "/webdav/"
                if (bundleFolderInput.StartsWith("/webdav/"))
                {
                    bundleFolderTcm = bundleFolderInput;
                }
                else if (bundleFolderInput.StartsWith("\\"))
                {
                    // If the folder string starts with \, then assume it's entered as a path
                    // Check if the first part of the path is a publication - if it's not, assume we are working with a path suffix.
                    var bundleFolderAsWebdav = convertPathToWebDav(bundleFolderInput);
                    //System.IO.File.WriteAllText(@"C:\Users\Administrator\Desktop\text.txt", bundleFolderAsWebdav);
                    bundleFolderTcm = client.GetTcmUri(bundleFolderAsWebdav, null, null);
                }
                else if (!bundleFolderInput.StartsWith("tcm:"))
                {
                    // If it's not prefixed with "tcm:", assume it's otherwise a valid tcm ID (i.e. "5-5209-2" instead of "tcm:5-5209-2") and
                    // prepend the "tcm:" prefix.
                    bundleFolderTcm = "tcm:" + bundleFolderInput;
                }
                else
                {
                    // If bundleFolder does not start with \ (i.e. a path), but starts with "tcm:" then assume it's entered as a tcm ID.
                    bundleFolderTcm = bundleFolderInput;
                }

                var bundleFolderItem = (OrganizationalItemData)client.GetDefaultData(Tridion.ContentManager.CoreService.Client.ItemType.VirtualFolder, bundleFolderTcm, new ReadOptions());
                bundleFolderPath = bundleFolderItem.LocationInfo.Path;
            }
            catch (Exception)
            {
                CreateBundlesPanel.Controls.Add(createBundlesErrorMessageLabel);
            }

            return bundleFolderPath;
        }

        protected void testClick(object sender, EventArgs e)
        {
            string releaseId = Request["showItemsInRelease"];
            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            Release release = rmRep.getRelease(releaseId);

            // TODO: perform validations of input bundleFolder and output descriptive error message under bundles panel here.

            string bundleFolderPath = validateFolderInput("yyy", rmRep);
            if (string.IsNullOrEmpty(bundleFolderPath))
            {
                // Path could not be successfully retrieved, so cancel executing the rest of this method.
                return;
            }
        }

        protected void renameRefreshClick(object sender, EventArgs e)
        {
            // Call showItemsInRelease() to ensure clicking the Create Bundles button does NOT return us to the main Release Manager dialog
            //string releaseId = Request["showItemsInRelease"];
            //showItemsInRelease(releaseId);

            Button btn = (Button)sender;

            Label lbl = new Label();
            ((LiteralControl)ReleaseItems.FindControl(btn.Attributes["lineID"])).Text = ((LiteralControl)ReleaseItems.FindControl(btn.Attributes["lineID"])).Text.Replace("itemRenamed", "");
            lbl.ID = "createBundlesErrorMessage";
            lbl.ForeColor = System.Drawing.Color.Red;
            CreateBundlesPanel.Controls.Add(lbl);

            string itemTcmId = btn.Attributes["data-tcmuri"];

            // Remove item moved/renamed warning label and button.
            //btn.Parent.Controls.Remove(ReleaseItems.FindControl(btn.Attributes["itemRenamedWarningId"]));
            btn.Click -= renameRefreshClick;
            btn.Parent.Controls.Remove(btn);

            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            string releaseId = Request["showItemsInRelease"];
            var release = rmRep.getRelease(releaseId);
            string itemNewFullPath = rmRep.updateItemInReleaseData(release, itemTcmId);

            ((LiteralControl)ReleaseItems.FindControl(btn.Attributes["lineID"])).Text = ((LiteralControl)ReleaseItems.FindControl(btn.Attributes["lineID"])).Text.Replace(btn.Attributes["itemFullPath"], itemNewFullPath);

            //showItemsInRelease(releaseId);
        }

        protected void backToReleases_click(object sender, EventArgs e)
        {
            loadReleases();
        }

        protected void createBundClick(object sender, EventArgs e)
        {
            // TODO: Try to Remove this code if you remove !isPostBack logic in Page_Load method near the top
            // Call showItemsInRelease() to ensure clicking the Create Bundles button does NOT return us to the main Release Manager dialog
            string releaseId = Request["showItemsInRelease"];

            // TODO: is this call needed here???:
            showItemsInRelease(releaseId);

            // Test you can retrieve values from bundle folder and prefix text boxes:
            string bundleFolderInput = "";
            string bundlePrefixInput = "";
            retrieveBundleInputs(ref bundleFolderInput, ref bundlePrefixInput);

            ReleaseManagerRepository rmRep = new ReleaseManagerRepository(Server, Request);
            Release release = rmRep.getRelease(releaseId);

            // TODO: perform validations of input bundleFolder and output descriptive error message under bundles panel here.

            string bundleFolderPath = validateFolderInput(bundleFolderInput, rmRep);
            if (string.IsNullOrEmpty(bundleFolderPath))
            {
                // Path could not be successfully retrieved, so cancel executing the rest of this method.
                return;
            }

            var bundleFoldersAsWebdavs = new List<string>();

            // Get the common part of the bundle path that each relevant publication's bundle folder will have.
            // e.g. \000 Empty Parent\Building Blocks\Bundles -> \Building Blocks\Bundles
            // Note: Folders in Tridion are not allowed to contain \
            string bundleFolderWithoutPrefix = bundleFolderPath.Remove(0, 1);
            int suffixFirstSlash = bundleFolderWithoutPrefix.IndexOf('\\');
            string bundleFolderSuffix = bundleFolderWithoutPrefix.Substring(suffixFirstSlash);
            // First replace all / with %2F, then replace all \ with /.
            bundleFolderSuffix = bundleFolderSuffix.Replace("/", "%2F").Replace('\\', '/');

            // TODO: consider URL encoding webdav, as suggeted should be done here: http://tridion.stackexchange.com/questions/11686/get-item-by-title-and-path-using-core-service

            foreach (var item in release.items)
            {
                // Decided to go with string manipulation to get the list of publication names, instead of using client as it seems like it would be slower
                // Remove "/webdav/" prefix.
                // e.g. /webdav/000%20Empty%20Parent/Building%20Blocks/Content/Folder%201
                string webdavWithoutPrefix = item.WEBDAV_URL.Remove(0, 8);
                int indexOfFirstSlash = webdavWithoutPrefix.IndexOf('/');
                // e.g.: 000%20Empty%20Parent/Building%20Blocks/Content/Folder%201 < indexOfFirstSlash=20
                string pub = webdavWithoutPrefix.Substring(0, indexOfFirstSlash);

                // Why remove /webdav/ if we're just going to add it back?:
                string currBundleFolderAsWebdav = "/webdav/" + pub + bundleFolderSuffix;
                if (!bundleFoldersAsWebdavs.Contains(currBundleFolderAsWebdav))
                {
                    bundleFoldersAsWebdavs.Add(currBundleFolderAsWebdav);

                    rmRep.createBundle(currBundleFolderAsWebdav, bundlePrefixInput);
                }

                // TODO: add items to bundle
            }
            // TODO: You will probably want to check for each pub in list whether it is a child of the publication
            // specified as the first part of the path given by"bundleFolder".

            //foreach (var currBundleFolderAsWebdav in bundleFoldersAsWebdavs)
            //{
            //    rmRep.createBundle(currBundleFolderAsWebdav, bundlePrefixInput);
            //}
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
