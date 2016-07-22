function dropXmlFieldsFromSubmit() {
    // If the form gets submitted (a button is pressed) without these items being disabled, ASP throws a validation error
    // because the contents of these boxes look threatening.
    var settingsArea = document.getElementById("txtExportSettings");
    $('#txtExportSettings,#txtImportSettings,#exportXml').attr('disabled', 'disabled');
    return true;
}

if(typeof(window.ADS) === 'undefined'){
    ADS = {};
}
if(typeof(window.ADS.Tridion) === 'undefined'){
    ADS.Tridion = {};
}


ADS.Tridion.ReleaseManager = {
    notesVal: '',
    serviceUrl: 'ReleaseManagerService.asmx',

    init: function () {
        var parent = ADS.Tridion.ReleaseManager;
        $('.viewItem').click(parent.handleViewItemClick);
        parent.adjustNotesSize();

        $('.refreshRenamedButton').click(parent.handleRefreshRenamedItemClick);

        $('.viewItems').on('click', parent.handleViewItemsClick);
        //$('#backButton').on('click', parent.handleBackToReleasesClick);

        // DJ
        //$('.bundlesButton').on('click', parent.handleCreateBundlesClick);

        //$('.createBundles').click(parent.handleBundlesClick);
        //$('.createBundles').on('click', parent.handleBundlesClick);

        $notesBox = $('#notesBox');
        if ($notesBox.length == 1) {
            $notesBox.on('focus', parent.handleNotesFocus);
            $notesBox.on('keyup', parent.handleNotesChange);
            $notesBox.on('blur', parent.handleNotesBlur);
            parent.notesVal = $notesBox.val();
            $('#saveNotes').on('click', parent.handleSaveNotesClick);
        }
        $('#continueButton').on('click', function (e) { window.location = 'manageReleases.aspx'; });
        $('#importButton').on('click', parent.handleImportClick);
    },

    handleImportClick: function (e) {
        $('#releaseXml').attr('disabled', 'disabled');
        $('.controls').hide();
        var xml = $('#releaseXml').val();
        //xml = xml.replace('<', '&lt;').replace('>', '&gt;');
        $('.status').hide();
        var data = { releaseXml: xml };
        $.ajax({
            method: 'POST',
            url: ADS.Tridion.ReleaseManager.serviceUrl + '/ImportRelease',
            data: JSON.stringify(data),
            success: ADS.Tridion.ReleaseManager.handleImportSuccess,
            error: ADS.Tridion.ReleaseManager.handleImportError,
            contentType: "application/json",
            dataType: 'json'
        });
    },

    handleImportError: function (jqXHR, textStatus, errorThrown) {
        $('#form1').append('There was an error importing this release.');
    },

    handleImportSuccess: function (data) {
        console.log(data);
        console.log(data.d.result);

        if (data.d.result == 'error') {
            $('#releaseXml').removeAttr('disabled');
            $('#resultMessage').removeClass('good').addClass('bad');
            $('.controls').show();
        } else {
            $('#continueButton').show();
            $('#resultMessage').removeClass('bad').addClass('good');
        }
        $('#resultMessage').text(data.d.message).show();
    },

    //handleBackToReleasesClick: function (e) {
    //window.location = '?';
    //},

    handleViewItemsClick: function (e) {
        window.location = "?showItemsInRelease=" + $(e.currentTarget).data('releaseid');
    },

    // DJ
    //handleCreateBundlesClick: function (e) {
    //    window.location = "?showItemsInRelease=" + $(e.currentTarget).data('releaseid');
    //},

    handleSaveNotesClick: function (e) {
        var parent = ADS.Tridion.ReleaseManager;
        var data = { releaseId: $('#releaseId').val(), note: ($('#notesBox').val() == 'Notes:' ? '' : $('#notesBox').val()) };
        $('#saveNotes').hide();
        $.ajax({
            method: 'POST',
            url: ADS.Tridion.ReleaseManager.serviceUrl + '/AddNote',
            data: JSON.stringify(data),
            success: ADS.Tridion.ReleaseManager.handleSaveNotesSuccess,
            contentType: "application/json",
            dataType: 'json'
        });
    },

    //handleCreateBundlesClick: function (e) {
    //    var parent = ADS.Tridion.ReleaseManager;
    //    var data = { releaseId: $('#releaseId').val() };
    //    $.ajax({
    //        method: 'POST',
    //        url: ADS.Tridion.ReleaseManager.serviceUrl + '/CreateBundles',
    //        data: JSON.stringify(data),
    //        //success: ADS.Tridion.ReleaseManager.handleCreateBundlesSuccess,
    //        contentType: "application/json",
    //        dataType: 'json'
    //    });
    //},

    handleBundlesClick: function (e) {
        window.location = "?showBundles=" + $(e.currentTarget).data('releaseid');
    },

    handleSaveNotesFail: function () {
        $('#notesPanel .status').text('Your note could not be saved.').addClass('bad').removeClass('good').show();
    },

    handleSaveNotesSuccess: function (data) {
        $('#notesPanel .status').text(data.d.message).addClass('good').removeClass('bad').show();
        setTimeout(ADS.Tridion.ReleaseManager.hideStatus, 2500);
    },

    hideStatus: function () {
        $('#notesPanel .status').fadeOut(1000);
    },

    handleNotesBlur: function (e) {
        var parent = ADS.Tridion.ReleaseManager;
        if ($('#notesBox').val() === '') {
            $('#notesBox').val('Notes:').addClass('empty');
        }

        parent.handleNotesChange(e);
        parent.notesVal = $('#notesBox').val();
    },

    handleNotesChange: function (e) {
        var parent = ADS.Tridion.ReleaseManager;
        $('#saveNotes').toggle($('#notesBox').val() != parent.notesVal);
        parent.adjustNotesSize();
    },

    handleNotesFocus: function (e) {
        if ($('#notesBox').val() === 'Notes:') {
            $('#notesBox').val('').removeClass('empty');
        }
    },

    adjustNotesSize: function () {
        var $box = $('#notesBox');
        if (typeof ($box.val()) !== 'undefined') {
            var cols = $box.attr('cols');
            var rows = $box.attr('rows')
            //var size = rows * cols;
            var lineBreaks = $box.val().match(/\n/g);
            var actualLength = $box.val().length - (lineBreaks != null ? lineBreaks.length : 0);
            rows = Math.ceil((actualLength + 10) / cols) + (lineBreaks != null ? lineBreaks.length : 0) + 1;
            $box.attr('rows', rows);
        }
    },

    handleViewItemClick: function (e) {
        var $el = $(e.currentTarget);
        var uri = $el.parent().parent().data('tcmuri');
        var parts = uri.split('-');
        var itemTypeIdentifier = 16;
        if (parts.length > 2) {
            itemTypeIdentifier = parts[parts.length - 1];
        }
        var openerUrl = "/WebUI/item.aspx?tcm=" + itemTypeIdentifier + "#id=" + uri;
        if (window.opener != null) {
            window.opener.open(openerUrl, "viewItem", "", false);
        } else {
            window.open(openerUrl, "viewItem", "", false);
        }
    }

    //handleRefreshRenamedItemClick: function (e) {
    //    $('.refreshRenamedButton').val("xxx");
    //}
};

$(function () {
    ADS.Tridion.ReleaseManager.init();
});