Type.registerNamespace("ReleaseManagerExtension");

ReleaseManagerExtension.AddToRelease = function ReleaseManagerExtension$AddToRelease(name, action) {
    Type.enableInterface(this, "ReleaseManagerExtension.AddToRelease");
    this.addInterface("Tridion.Cme.Command", ["AddToRelease", $const.AllowedActions.View]);
};

ReleaseManagerExtension.AddToRelease.prototype._execute = function AddToRelease$_execute(selection) {
	var uris = selection.getItems().join(',');
    var url = $config.expandEditorPath("/addToRelease.aspx?uris=" + uris, "ReleaseManagerExtension");
    var popup = $popup.create(url, "toolbar=no,width=300,height=500,resizable=yes,scrollbars=yes", null);
    popup.open();
};

ReleaseManagerExtension.ManageReleases = function ReleaseManagerExtension$ManageReleases(name, action) {
    Type.enableInterface(this, "ReleaseManagerExtension.ManageReleases");
    this.addInterface("Tridion.Cme.Command", ["ManageReleases", $const.AllowedActions.Enable]);
};

ReleaseManagerExtension.ManageReleases.prototype.isEnabled = function ManageReleases$isEnabled(selection) {
    return true;
};

ReleaseManagerExtension.ManageReleases.prototype._execute = function ManageReleases$_execute(selection) {
    var url = $config.expandEditorPath("/manageReleases.aspx", "ReleaseManagerExtension");
    var popup = $popup.create(url, "toolbar=no,width=1000,height=550,resizable=yes,scrollbars=yes", null);
    popup.open();
}; 