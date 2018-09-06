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
    });
    return newList;
}

function convertToObservable(obj) {
    var newObj = {};
    Object.keys(obj).forEach(function (key) {
        if (Array.isArray(obj[key])) {
            newObj[key] = ko.observableArray(convertArrayToObservable(obj[key]));
        } else {
            newObj[key] = ko.observable(obj[key]);
        }
    });
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

/*
console.log(arrayFirstIndexOf(viewModel.items(), function(item) {
   return item.id === id;
}));

viewModel.items()[1];

viewModel.items.replace( oldItem, newItem )
 */