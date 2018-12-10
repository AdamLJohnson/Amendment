function ManageAmendmentHub() {
    const amendmentUpdatesConnection = new signalR.HubConnectionBuilder().withUrl("/amendmentHub").build();
    amendmentUpdatesConnection.onclose((e) => { showConnectionError(); });

    amendmentUpdatesConnection.on(_clientNotifierMethods.amendmentChange,
        (results) => {
            toastr.options.escapeHtml = true;
            var message = "Amendment " + results.id;
            switch (results.results.operationType) {
            case 1:
                message += " Created";
                break;
            case 2:
                message += " Updated";
                break;
            case 3:
                message += " Deleted";
                break;
            default:
            }
            toastr.info(message);

            jQuery.event.trigger("amendment.amendmentChange", results);
        });

    amendmentUpdatesConnection.on(_clientNotifierMethods.amendmentBodyChange,
        (results) => {
            toastr.options.escapeHtml = true;
            var message = "Amendment Body " + results.id;
            switch (results.results.operationType) {
            case 1:
                message += " Created";
                break;
            case 2:
                message += " Updated";
                break;
            case 3:
                message += " Deleted";
                break;
            default:
            }
            toastr.info(message);

            jQuery.event.trigger("amendment.amendmentBodyChange", results);
        });
    
    amendmentUpdatesConnection.start().catch(err => console.error(err.toString())).then(() => { jQuery.event.trigger("amendment.ready");});
    return amendmentUpdatesConnection;
}

function initScreenViewModel(htmlId, languageId, amendment, amendmentBody, languageName) {
    var ScreenViewModel = function () {
        var self = this;
        self.hub = ScreenViewHub(languageId);
        self.amendmentIsLive = ko.observable(amendment.isLive);
        self.amendmentBodyIsLive = ko.observable(amendmentBody.isLive);
        self.amendBodyPagedHtml = ko.observable(amendmentBody.amendBodyPagedHtml);
        self.page = ko.observable(amendmentBody.page);
        self.pages = ko.observable(amendmentBody.pages);
        self.language = languageName;
        self.languageId = languageId;
        self.isLive = ko.computed(function () {
            return self.amendmentIsLive() && self.amendmentBodyIsLive();
        });

        $(document).on("screen.amendmentBodyChange." + self.languageId, function (evt, results) {
            self.amendBodyPagedHtml(results.amendBodyPagedHtml);
            self.amendmentBodyIsLive(results.isLive);
            self.page(results.page);
            self.pages(results.pages);
        });

        $(document).on("screen.amendmentChange." + self.languageId, function (evt, results) {
            self.amendmentIsLive(results.isLive);
        });

        $(document).on("screen.clearScreens." + self.languageId, function (evt) {
            self.amendmentIsLive(false);
            self.amendmentBodyIsLive(false);
        });
    };
    ko.applyBindings(new ScreenViewModel(), document.getElementById(htmlId));
}

function ScreenViewHub(languageId)
{
    const screenUpdatesConnection = new signalR.HubConnectionBuilder().withUrl("/screenHub?languageId=" + languageId).build();
    screenUpdatesConnection.onclose((e) => { showConnectionError(); });
    screenUpdatesConnection.on(_clientNotifierMethods.clearScreens,
        () => {
            if (_usersRoles.indexOf('System Administrator') > -1) {
                toastr.options.escapeHtml = true;
                var message = "";
                toastr.info(message, _clientNotifierMethods.clearScreens);
            }

            jQuery.event.trigger("screen.clearScreens." + languageId);
        });

    screenUpdatesConnection.on(_clientNotifierMethods.amendmentBodyChange,
        (results) => {
            if (_usersRoles.indexOf('System Administrator') > -1) {
                toastr.options.escapeHtml = true;
                var message = "screen " + results.id;
                toastr.info(message, _clientNotifierMethods.amendmentBodyChange);
            }

            jQuery.event.trigger("screen.amendmentBodyChange." + languageId, results);
        });

    screenUpdatesConnection.on(_clientNotifierMethods.amendmentChange,
        (results) => {
            if (_usersRoles.indexOf('System Administrator') > -1) {
                toastr.options.escapeHtml = true;
                var message = "screen " + results.id;
                toastr.info(message, _clientNotifierMethods.amendmentChange);
            }

            jQuery.event.trigger("screen.amendmentChange." + languageId, results);
        });

    screenUpdatesConnection.start().catch(err => console.error(err.toString()));
    return screenUpdatesConnection;
}

function convertArrayToObservable(list) {
    var newList = [];
    $.each(list, function (i, obj) {
        var newObj = convertToObservable(obj);
        newList.push(newObj);
    });;
    return newList;
}

function convertToObservable(obj) {
    var newObj = {};
    if (typeof obj === 'object' && obj !== null) {
        Object.keys(obj).forEach(function (key) {
            if (Array.isArray(obj[key])) {
                newObj[key] = ko.observableArray(convertArrayToObservable(obj[key]));
            } else {
                newObj[key] = ko.observable(obj[key]);
            }
        });
    }
    else {
        return ko.observable(obj);
    }

    return newObj;

    
}

function arrayFirstIndexOf(array, predicate, predicateOwner) {
    for (var i = 0, j = array.length; i < j; i++) {
        if (predicate.call(predicateOwner, array[i])) {
            return i;
        }
    }
    return -1;
}

function userIsInRole() {
    if (arguments.length === 0) return false;

    for (var i = 0; i < arguments.length; i++) {
        var r = _usersRoles.indexOf(arguments[i]);
        if (r > -1) {
            return true;
        }
    }
    return false;
}

function showConnectionError() {
    $("#connection-error").removeClass("hidden");
}

function insertAtCaret(areaId, text) {
    var txtarea = document.getElementById(areaId);
    if (!txtarea) {
        return;
    }

    var scrollPos = txtarea.scrollTop;
    var strPos = 0;
    var br = ((txtarea.selectionStart || txtarea.selectionStart == '0') ?
        "ff" : (document.selection ? "ie" : false));
    if (br == "ie") {
        txtarea.focus();
        var range = document.selection.createRange();
        range.moveStart('character', -txtarea.value.length);
        strPos = range.text.length;
    } else if (br == "ff") {
        strPos = txtarea.selectionStart;
    }

    var front = (txtarea.value).substring(0, strPos);
    var back = (txtarea.value).substring(strPos, txtarea.value.length);
    txtarea.value = front + text + back;
    strPos = strPos + text.length;
    if (br == "ie") {
        txtarea.focus();
        var ieRange = document.selection.createRange();
        ieRange.moveStart('character', -txtarea.value.length);
        ieRange.moveStart('character', strPos);
        ieRange.moveEnd('character', 0);
        ieRange.select();
    } else if (br == "ff") {
        txtarea.selectionStart = strPos;
        txtarea.selectionEnd = strPos;
        txtarea.focus();
    }

    txtarea.scrollTop = scrollPos;
}