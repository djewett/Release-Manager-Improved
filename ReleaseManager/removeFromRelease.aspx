<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="removeFromRelease.aspx.cs" Inherits="ReleaseManager.removeFromRelease" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Are you sure you want to remove this item?</title>
    <link rel="Stylesheet" type="text/css" href="css/ReleaseManager.css" />
</head>
<body>
    <form id="removeFromRelease" runat="server">
    Are you sure you want to remove <asp:PlaceHolder ID="itemName" runat="server">item</asp:PlaceHolder>
     from <asp:PlaceHolder ID="releaseName" runat="server">release</asp:PlaceHolder>?<br /><br />

    <asp:Button ID="noButton" Text="No" runat="server" onclick="noButton_Click" />
    <asp:LinkButton ID="yesButton" Text="Yes" runat="server" 
        onclick="yesButton_Click" />
    
    </form>
</body>
</html>
