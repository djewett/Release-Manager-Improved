Type.registerNamespace("ReleaseManagerExtension");

//Release
ReleaseManagerExtension.Release = function ReleaseManagerExtension$Release(name, action) {
    Type.enableInterface(this, "ReleaseManagerExtension.Release");
    this.addInterface("Tridion.Cme.Command", ["Release", $const.AllowedActions.View]);
};

ReleaseManagerExtension.Release.prototype._execute = function Release$_execute(selection) {
    var id = selection.getItem(0);
    //var item = $models.getItem(id);
    //item.load();
    
    var url = $config.expandEditorPath("/releaseManager.aspx?uri=" + id, "ReleaseManagerExtension");
    var popup = $popup.create(url, "toolbar=no,width=626,height=436,resizable=yes,scrollbars=yes", null);
    popup.open();
    //console.log("Popup opened");

};

//Releasemanager
//Release
ReleaseManagerExtension.ManageReleases = function ReleaseManagerExtension$ManageReleases(name, action) {
    Type.enableInterface(this, "ReleaseManagerExtension.ManageReleases");
    this.addInterface("Tridion.Cme.Command", ["ManageReleases", $const.AllowedActions.Enable]);
};

ReleaseManagerExtension.ManageReleases.prototype.isEnabled = function ManageReleases$isEnabled(selection) {
    return true;
};

ReleaseManagerExtension.ManageReleases.prototype._execute = function ManageReleases$_execute(selection) {
    var url = $config.expandEditorPath("/manageReleases.aspx", "ReleaseManagerExtension");
    var popup = $popup.create(url, "toolbar=no,width=626,height=436,resizable=yes,scrollbars=yes", null);
    popup.open();
    console.log("Popup3 opened");
}; 