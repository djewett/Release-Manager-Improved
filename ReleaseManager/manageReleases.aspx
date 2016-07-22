<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="manageReleases.aspx.cs" Inherits="ReleaseManager.ManageReleases" ValidateRequest="false" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
    <title>Release Manager</title>
    <link rel="Stylesheet" type="text/css" href="css/ReleaseManager.css" />
    <script type="text/javascript" src="js/jquery.min.js"></script>
    <script type="text/javascript" src="js/rmMain.js"></script>
    <script type="text/javascript" language="javascript">
        function ConfirmDeleteRelease(releaseTitle)
        {
            return confirm("Please confirm that you would like to delete release:\n" + releaseTitle);
        }
    </script>
</head>
<body style="background-image:url(images/splash_gradient.png)">
    <form id="form1" runat="server" onsubmit="dropXmlFieldsFromSubmit()">
    <div>

        <asp:Panel ID="CreateBundlesPanel" runat="server">
            <label id="bundleFolderLabel">Bundle Folder:</label>
            <input type="text" id="bundleFolder" runat="server" />
            <label id="bundlePrefixLabel">Bundle Prefix:</label>
            <input type="text" id="bundlePrefix" runat="server" />
            <asp:Button runat="server" id="createBundleButton" OnClick="createBundClick" Text="Create Bundles" />
        </asp:Panel>
        
        <asp:Panel ID="PanelReleases" runat="server">
            <h3>Available Releases</h3>
            <asp:LinkButton runat="server" id="addReleaseButton" OnClick="addReleaseClick" Text="Add a Release" />
            <a href="importRelease.aspx" id="importRelease">Import a Release</a>
            <div style="clear:both;"></div>
        </asp:Panel>

        <asp:Panel ID="ReleaseItems" runat="server"></asp:Panel>

        <asp:Panel ID="panelButtonCreateImportExport" runat="server">
        </asp:Panel>
        
        <asp:Panel ID="PanelImportExport" runat="server" Visible="false">
            <h4>Settings for <asp:PlaceHolder ID="importExportName" runat="server">release name</asp:PlaceHolder></h4>
            <table cellpadding="2">
                <tr>
                    <td><strong>Export settings</strong></td>
                    <td><strong>Import settings</strong></td>
                </tr>   
                <tr>
                    <td>
                        <asp:TextBox ID="txtExportSettings" runat="server" Height="120" Width="300px" TextMode="MultiLine"></asp:TextBox>
                    </td>
                    <td>
                        <asp:TextBox ID="txtImportSettings" runat="server" Height="120" Width="300px" TextMode="MultiLine"></asp:TextBox>        
                    </td>
                </tr>    
                <tr>
                    <td>
                        <asp:Panel ID="panelExportHyperlink" runat="server">
                        </asp:Panel>
                     </td>
                    <td>
                       <asp:Panel ID="panelImportHyperlink" runat="server">
                        </asp:Panel>
                    </td>                
                </tr>     
            </table>
            <asp:LinkButton ID="finalizeRelease" Text="Finalize Release" runat="server" onclick="finalizeReleaseClick" />
            <asp:LinkButton ID="exportRelease" Text="Export Release" runat="server" onclick="exportReleaseClick" />
        </asp:Panel>

        <asp:Panel ID="PanelFinalizeRelease" runat="server" Visible="false">
            <h4>Are you sure you want to finalize this release?</h4>
            <asp:Button text="No" ID="finalizeReleaseNo" OnClick="finalizeReleaseClickNo" runat="server" />
            <asp:LinkButton text="Yes" ID="finalizeReleaseYes" OnClick="finalizeReleaseClickYes" runat="server" />
        </asp:Panel>
        
        <asp:Panel ID="ExportReleasePanel" runat="server" Visible="false">
            <h4>Release Manager Export XML</h4>
            <asp:TextBox TextMode="MultiLine" ID="exportXml" runat="server" Rows="3" Columns="60" />
        </asp:Panel>

        <asp:Panel ID="addReleaseForm" runat="server" Visible="false">
            <label for="txtNewRelease">Release name:</label>
            <asp:TextBox ID="txtNewRelease" runat="server"></asp:TextBox><br />
            <asp:Button ID="btnAddNewRelease" runat="server" Text="Add release"  onclick="btnAddNewRelease_Click" />
            <asp:LinkButton ID="cancelAddNewRelease" runat="server" Text="Cancel" 
                onclick="cancelAddNewRelease_Click" />
        </asp:Panel>

        <asp:Panel ID="PanelButtonsDeleteReleases" runat="server"></asp:Panel>
        
        <asp:Panel ID="panelDefaultSettings" runat="server" Visible="false">
             <table>
                <tr>
                    <td>Dependencys</td>
                    <td>Itemfilters</td>
                </tr>
                <tr>
                    <td> <asp:CheckBoxList ID="CheckBoxListDependencys" runat="server" 
                            AutoPostBack="true" ToolTip="Dependencys" 
                            onselectedindexchanged="CheckBoxListDependencys_SelectedIndexChanged">
                         <asp:ListItem Text="Layout" Value="Layout" Selected="True" />
                         <asp:ListItem Text="Structure" Value="Structure" Selected="True"/>
                         <asp:ListItem Text="Content" Value="Content" Selected="True" />
                         <asp:ListItem Text="Definition" Value="Definition" Selected="True"/>
                         <asp:ListItem Text="Default" Value="Default" Selected="True"/>
                         <asp:ListItem Text="Workflow" Value="Workflow" Selected="True"/>
                         <asp:ListItem Text="Security" Value="Security" Selected="True"/>
                     </asp:CheckBoxList></td>
                    <td><asp:CheckBoxList ID="CheckBoxListItemFilters" runat="server" AutoPostBack="true" ToolTip="Dependencys">
                         <asp:ListItem Text="TargetGroups" Value="TargetGroups"  />
                         <asp:ListItem Text="Schemas" Value="Structure" />
                         <asp:ListItem Text="Components" Value="Content"  />
                         <asp:ListItem Text="TemplateBuildingBlocks" Value="Definition" />
                         <asp:ListItem Text="ComponentTemplates" Value="Default" />
                         <asp:ListItem Text="PageTemplates" Value="Workflow" />
                         <asp:ListItem Text="Pages" Value="Security" />
                         <asp:ListItem Text="ApprovalStatuses" Value="Security" />
                         <asp:ListItem Text="ProcessDefinitions" Value="Security" />
                         <asp:ListItem Text="VirtualFolders" Value="Security" />
                         <asp:ListItem Text="MultimediaTypes" Value="Security" />
                         <asp:ListItem Text="Keywords" Value="Security" />
                         <asp:ListItem Text="Groups" Value="Security" />                    
                     </asp:CheckBoxList></td>
                </tr>
             </table>
        </asp:Panel>
    
    </div>
    </form>
</body>
</html>

