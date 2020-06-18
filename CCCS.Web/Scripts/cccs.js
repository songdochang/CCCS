var CCCS = {
    loadWaiting: false,
    documentlistselector: '',

    init: function (documentlistselector) {
        this.loadWaiting = false;
        this.documentlistselector = documentlistselector;
    },

    setLoadWaiting: function (display) {
        displayAjaxLoading(display);
        this.loadWaiting = display;
    },

    document_received: function (url, formselector) {
        if (this.loadWaiting != false) {
            return;
        }
        this.setLoadWaiting(true);

        $.ajax({
            cache: false,
            url: url,
            data: $(formselector).serialize(),
            type: 'post',
            success: this.success_process,
            complete: this.resetLoadWaiting,
            error: this.ajaxFailure
        });
    },

    success_process: function (response) {
        $(CCCS.documentlistselector).html(response.listhtml);
        return false;
    },

    resetLoadWaiting: function () {
        CCCS.setLoadWaiting(false);
    },

    ajaxFailure: function () {
        alert('Please refresh the page and try one more time.');
    }
};

function displayAjaxLoading(display) {
    if (display) {
        $('.ajax-loading-block-window').show();
    }
    else {
        $('.ajax-loading-block-window').hide('slow');
    }
}