﻿var ScreenControlModel = function (initialData) {
    self.amendments = ko.observableArray(convertArrayToObservable([initialData]));
    self.amendment = ko.computed(function() {
        var upIx = arrayFirstIndexOf(self.amendments(),
            function (item) {
                return item.isLive();
            });
        if (upIx > -1) {
            return self.amendments()[upIx];
        }
        return {};
    });
    console.log(self.amendment());
    self.hub = ManageAmendmentHub();
    self.bodies = ko.computed(function () {
        if (!self.amendment().amendmentBodies) {
            return [];
        }
        var output = [];
        var currentRow = [];
        var arr = self.amendment().amendmentBodies.sort(function(left, right) {
            return left.languageId() === right.languageId() ? 0 : (left.languageId() < right.languageId() ? -1 : 1);
        });
        for (var i = 0; i < arr().length; i++) {
            var item = arr()[i];
            if (i % 3 === 0) {
                currentRow = [];
                output.push(currentRow);
            }
            currentRow.push(item);
        }
        return output;
    });

    $(document).on("amendment.amendmentChange", function (evt, results) {
        console.log(results);
        var upIx = arrayFirstIndexOf(self.amendments(),
            function (item) {
                return item.id() === results.id;
            });
        switch (results.results.operationType) {
            case 1:
                self.amendments.push(convertToObservable(results.data));
                break;
            case 2:
                if (upIx > -1) {
                    var oldItem = self.amendments()[upIx];
                    var newItem = convertToObservable(results.data);
                    self.amendments.replace(oldItem, newItem);
                } else {
                    self.amendments.push(convertToObservable(results.data));
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

ko.applyBindings(new ScreenControlModel(initialData), document.getElementById("screenControl"));