function ManageAmendmentHub() {
    const amendmentUpdatesConnection = new signalR.HubConnectionBuilder().withUrl("/amendmentHub").build();

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

    function start(reconnect) {
        if (reconnect) {
            console.log("Attempting reconnect");
        }
        amendmentUpdatesConnection.start().then(() => {
            hideConnectionError(reconnect);
            console.log("Connected");
            if (reconnect) {
                jQuery.event.trigger("amendment.reconnect");
            } else {
                jQuery.event.trigger("amendment.ready");
            }
        }, (err) => {
            console.log(err);
            setTimeout(() => start(reconnect), 5000);
        });
    }

    amendmentUpdatesConnection.onclose((e) => {
        showConnectionError();
        start(true);
    });
    start(false);
    testconn = amendmentUpdatesConnection;
    return amendmentUpdatesConnection;
}

function ScreenViewHub(languageId)
{
    const screenUpdatesConnection = new signalR.HubConnectionBuilder().withUrl("/screenHub?languageId=" + languageId).build();
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

    screenUpdatesConnection.on(_clientNotifierMethods.refreshLanguage,
        (results) => {
            jQuery.event.trigger("screen.refreshLanguage." + languageId, results);
        });

    function start(reconnect) {
        if (reconnect) {
            console.log("Attempting reconnect");
        }
        screenUpdatesConnection.start().then(() => {
            hideConnectionError(reconnect);
            console.log("Connected");
            if (reconnect) {
                jQuery.event.trigger("screen.reconnect." + languageId);
            } else {
                jQuery.event.trigger("screen.ready." + languageId);
            }
        }, (err) => {
            console.log(err);
            setTimeout(() => start(reconnect), 5000);
        });
    }

    screenUpdatesConnection.onclose((e) => {
        showConnectionError();
        start(true);
    });
    start(false);
    return screenUpdatesConnection;
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
        self.ShowDeafSigner = ko.observable("0");
        self.ShowDeafSignerBox = ko.observable("0");
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

        $(document).on("screen.refreshLanguage." + self.languageId, function (evt, results) {
            console.log("screen.refreshLanguage." + self.languageId, results);
            if (results.amendmentBody) {
                self.amendBodyPagedHtml(results.amendmentBody.amendBodyPagedHtml);
                self.amendmentBodyIsLive(results.amendmentBody.isLive);
                self.page(results.amendmentBody.page);
                self.pages(results.amendmentBody.pages);
            } else {
                self.amendmentBodyIsLive(false);
            }
            self.amendmentIsLive(results.amendment.isLive);
        });

        $(document).on("screen.reconnect." + languageId, function (evt) {
            console.log("screen.reconnect." + self.languageId);
            self.hub.invoke("refreshLanguage", languageId);
            self.hub.invoke("getSystemSetting", "ShowDeafSigner");
            self.hub.invoke("getSystemSetting", "ShowDeafSignerBox");
        });

        $(document).on("screen.ready." + languageId, function (evt) {
            self.hub.invoke("getSystemSetting", "ShowDeafSigner");
            self.hub.invoke("getSystemSetting", "ShowDeafSignerBox");
        });

        self.hub.on("RefreshSetting", function (results) {
            updateSetting(self, results);
        });
    };
    ko.applyBindings(new ScreenViewModel(), document.getElementById(htmlId));
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
    $(".fullscreen .connection-error").removeClass("hidden");
    jQuery.event.trigger("connection.error");
}

function hideConnectionError(reconnect) {
    $("#connection-error").addClass("hidden");
    $(".fullscreen .connection-error").addClass("hidden");

    if (reconnect) {
        jQuery.event.trigger("connection.reconnect");
    }
}

function updateSetting(self, setting) {
    if (self[setting.key]) {
        self[setting.key](setting.value);
    } else {
        self[setting.key] = ko.observable(setting.value);
    }
}