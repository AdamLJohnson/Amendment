var AmendmentModel = function (amendments) {
    var amendmentUpdatesConnection = ManageAmendmentHub();
    var self = this;
    self.hub = amendmentUpdatesConnection;
    self.search_field = ko.observable('');
    self.clearSearch = function() {
        self.search_field('');
    };
    self.checkForEscapeKey = function(data, event) {
        if (event.key === "Escape") {
            self.clearSearch();
        }
        return true;
    };
    self.amendments = ko.observableArray(convertArrayToObservable(amendments));
    self.filteredAmendmentIds = ko.computed(function () {
        var searchVal = self.search_field();
        if (searchVal.length === 0) {
            return self.amendments().map(v => v.id());
        }

        return ko.utils.arrayFilter(self.amendments(), function (rec) {
            //https://www.c-sharpcorner.com/article/knockoutjs-filter-search-sort/
            searchVal = searchVal.toLowerCase();
            
            if (rec.motion() && rec.motion().toLowerCase().indexOf(searchVal) > -1) {
                return true;
            }
            if (rec.author() && rec.author().toLowerCase().indexOf(searchVal) > -1) {
                return true;
            }
            if (rec.amendTitle() && rec.amendTitle().toLowerCase().indexOf(searchVal) > -1) {
                return true;
            }
            if (rec.legisId() && rec.legisId().toLowerCase().indexOf(searchVal) > -1) {
                return true;
            }
            return false;
        }).map(v => v.id());
    });  
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
            var upIx2 = arrayFirstIndexOf(amendment.amendmentBodies(),
                function (item) {
                    return item.id() === results.id;
                });
            if (upIx2 > -1) {
                var oldItem = amendment.amendmentBodies()[upIx2];
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

    $(document).keyup(function (e) {
        self.checkForEscapeKey(null, e);
    });

    $(document).on("amendment.reconnect", function (evt) {
        self.hub.invoke("GetAllAmendments");
    });

    self.hub.on("amendment.getAllAmendmentsReturn", function (results) {
        self.amendments(convertArrayToObservable(results));
    });
};

ko.applyBindings(new AmendmentModel(initialData), document.getElementById("amendmentList"));