<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="addToRelease.aspx.cs" Inherits="ReleaseManager.AddToRelease" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
    <%--<link href="style/releasemanager.css" rel="stylesheet" type="text/css" />--%>
    <link rel="Stylesheet" type="text/css" href="css/ReleaseManager.css" />
     <title>Add to Release</title>
</head>
<body style="background-image:url(images/splash_gradient.png)" id="addToRelease">
    <form id="form1" runat="server">
        <div class="body">
            <h4>Select releases for <asp:PlaceHolder ID="itemLabelPlaceholder" runat="server">this item</asp:PlaceHolder>:</h4>
            <asp:Panel ID="allReleases" runat="server"></asp:Panel>
            <asp:Panel ID="panelReleasesItemIsIn" runat="server"></asp:Panel>
            <asp:Panel ID="panelReleasesItemIsNotIn" runat="server"></asp:Panel>
        </div>
        <asp:LinkButton OnClientClick="window.close();" runat="server" ID="cancelAdd" Text="Cancel" />

        <asp:Panel ID="PanelMessages" runat="server">
            <asp:Label ID="lblMessage" runat="server" Text=""></asp:Label>
        </asp:Panel>
    
    </form>
</body>
</html>
