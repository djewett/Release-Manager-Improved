<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="importRelease.aspx.cs" Inherits="ReleaseManager.importRelease" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Import a Release</title>
    <link rel="Stylesheet" type="text/css" href="css/ReleaseManager.css" />
    <script type="text/javascript" src="js/jquery.min.js"></script>
    <script type="text/javascript" src="js/rmMain.js"></script>
</head>
<body>
    <form id="importReleaseForm" runat="server">
        <h3>Import a Release</h3>
        <p>Paste the XML from the export here:</p>
        <asp:TextBox TextMode="MultiLine" ID="releaseXml" runat="server" Columns="60" Rows="5" />
        <div class="controls">
            <input type="button" value="Import" id="importButton" />
            <a href="manageReleases.aspx">Cancel</a>
        </div>
        <div id="resultMessage" class="status"></div>
        <input type="button" class="primary" class="primary" value="Continue" id="continueButton" />
    </form>
</body>
</html>
