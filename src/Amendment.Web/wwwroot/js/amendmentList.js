var AmendmentModel = function (amendments) {
    var amendmentUpdatesConnection = ManageAmendmentHub();
    var self = this;
    self.amendments = ko.observableArray(convertArrayToObservable(amendments));
    self.userRoles = _usersRoles;
    self.liveAmendment = ko.computed(function() {
        var liveIx = arrayFirstIndexOf(self.amendments(),
            function (item) {
                return item.isLive();
            });
        if (liveIx > -1) {
            return self.amendments()[liveIx].id();
        }
        return -1;
    });
    self.languageData = languageData;
    self.clearScreens = function () {
        amendmentUpdatesConnection.invoke("clearScreens");
    };
    self.amendmentGoLive = function(amendment) {
        amendmentUpdatesConnection.invoke("amendmentGoLive", amendment.id(), true);
    };

    $(document).on("amendment.amendmentChange", function (evt, results) {
        console.log(results);
        switch (results.results.operationType) {
        case 1:
            self.amendments.push(convertToObservable(results.data));
            break;
        case 2:
            var upIx = arrayFirstIndexOf(self.amendments(),
                function (item) {
                    return item.id() === results.id;
                });
            if (upIx > -1) {
                var oldItem = self.amendments()[upIx];
                var newItem = convertToObservable(results.data);
                self.amendments.replace(oldItem, newItem);
            }
            break;
        case 3:
            self.amendments.remove(function (item) { return item.id() === results.id; });
            break;
        default:
        }
    });

    $(document).on("amendment.amendmentBodyChange", function (evt, results) {
        console.log(results);
        var amendmentId = results.data.amendId;

        var upIx = arrayFirstIndexOf(self.amendments(),
            function (item) {
                return item.id() === amendmentId;
            });
        if (upIx === -1) {
            return;
        }

        var amendment = self.amendments()[upIx];


        switch (results.results.operationType) {
        case 1:
            amendment.amendmentBodies.push(convertToObservable(results.data));
            break;
        case 2:
            var upIx = arrayFirstIndexOf(amendment.amendmentBodies(),
                function (item) {
                    return item.id() === results.id;
                });
            if (upIx > -1) {
                var oldItem = amendment.amendmentBodies()[upIx];
                var newItem = convertToObservable(results.data);
                amendment.amendmentBodies.replace(oldItem, newItem);
            }
            break;
        case 3:
            amendment.amendmentBodies.remove(function (item) { return item.id() === results.id; });
            break;
        default:
        }
    });
};

ko.applyBindings(new AmendmentModel(initialData), document.getElementById("amendmentList"));