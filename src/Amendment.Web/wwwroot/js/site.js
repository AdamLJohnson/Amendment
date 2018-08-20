// Write your JavaScript code.
$(function () {
    const amendmentUpdatesConnection = new signalR.HubConnectionBuilder().withUrl("/amendmentHub").build();
    amendmentUpdatesConnection.on("AmendmentChange",
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
            toastr.info(message, "Amendment Changed");

            jQuery.event.trigger("AmendmentChange", results);
        });
    amendmentUpdatesConnection.start().catch(err => console.error(err.toString()));
});


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
            newObj[key] = ko.observableArray(obj[key]);
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

/*
console.log(arrayFirstIndexOf(viewModel.items(), function(item) {
   return item.id === id;
}));

viewModel.items()[1];

viewModel.items.replace( oldItem, newItem )
 */