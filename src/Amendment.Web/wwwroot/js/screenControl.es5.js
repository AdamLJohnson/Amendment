"use strict";

var ScreenControlModel = function ScreenControlModel(initialData) {
    self.amendments = ko.observableArray(convertArrayToObservable([initialData]));
    //self., showDeafSigner: $parent.ShowDeafSigner, showDeafSignerBox: $parent.ShowDeafSignerBox
    self.ShowDeafSigner = ko.observable("0");
    self.ShowDeafSignerBox = ko.observable("0");
    self.amendment = ko.computed(function () {
        var upIx = arrayFirstIndexOf(self.amendments(), function (item) {
            return item.isLive();
        });
        if (upIx > -1) {
            return self.amendments()[upIx];
        }
        return self.amendments()[0];
    });
    self.isLive = ko.computed(function () {
        var upIx = arrayFirstIndexOf(self.amendments(), function (item) {
            return item.isLive();
        });
        return upIx > -1;
    });
    self.primaryLanguageId = ko.computed(function () {
        var upIx = arrayFirstIndexOf(self.amendments(), function (item) {
            return item.isLive();
        });
        if (upIx > -1) {
            return self.amendments()[upIx].primaryLanguageId();
        }
        return -1;
    });
    self.toggleAmendmentBody = function (amendmentBody) {
        if (amendmentBody.isLive()) {
            self.hub.invoke("amendmentBodyGoLive", amendmentBody.amendId(), amendmentBody.id(), false);
        } else {
            self.hub.invoke("amendmentBodyGoLive", amendmentBody.amendId(), amendmentBody.id(), true);
        }
    };
    self.changePage = function (direction, amendmentBody) {
        //next, prev
        var dir = direction === 'next' ? 1 : -1;
        self.hub.invoke("amendmentBodyChangePage", amendmentBody.id(), dir);
    };
    self.changeAllPages = function (direction) {
        //next, prev
        var dir = direction === 'next' ? 1 : -1;
        self.hub.invoke("amendmentBodyChangeAllPages", amendment().id(), dir);
    };
    self.resetAllPages = function () {
        self.hub.invoke("amendmentBodyResetAllPages", amendment().id());
    };

    self.previewEnabled = ko.observable(true);

    self.togglePreview = function () {
        self.previewEnabled(!self.previewEnabled());
    };

    self.hub = ManageAmendmentHub();
    self.bodies = ko.computed(function () {
        if (!self.amendment().amendmentBodies) {
            return [];
        }
        var output = [];
        var currentRow = [];
        var arr = self.amendment().amendmentBodies.sort(function (left, right) {
            return left.languageId() === right.languageId() ? 0 : left.languageId() < right.languageId() ? -1 : 1;
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
    self.anyBodiesLive = ko.computed(function () {
        var upIx = arrayFirstIndexOf(self.amendment().amendmentBodies(), function (item) {
            return item.isLive();
        });
        return upIx !== -1;
    });
    self.toggleAmendmentBodyAll = function () {
        for (var i = 0; i < self.amendment().amendmentBodies().length; i++) {
            var body = self.amendment().amendmentBodies()[i];
            if (body.isLive() === self.anyBodiesLive()) {
                self.hub.invoke("amendmentBodyGoLive", body.amendId(), body.id(), !self.anyBodiesLive());
            }
        }
    };

    $(document).on("amendment.reconnect", function (evt) {
        self.hub.invoke("getLiveAmendment");
        self.hub.invoke("getSystemSetting", "ShowDeafSigner");
        self.hub.invoke("getSystemSetting", "ShowDeafSignerBox");
    });

    $(document).on("amendment.ready", function (evt) {
        self.hub.invoke("getSystemSetting", "ShowDeafSigner");
        self.hub.invoke("getSystemSetting", "ShowDeafSignerBox");
    });

    self.hub.on("RefreshSetting", function (results) {
        updateSetting(self, results);
    });

    self.hub.on("amendment.getLiveAmendmentReturn", function (results) {
        self.amendments(convertArrayToObservable([results]));
    });

    $(document).on("amendment.amendmentChange", function (evt, results) {
        updateAmendment(results);
    });

    function updateAmendment(results) {
        var upIx = arrayFirstIndexOf(self.amendments(), function (item) {
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
                self.amendments.remove(function (item) {
                    return item.id() === results.id;
                });
                break;
            default:
        }
    }

    $(document).on("amendment.amendmentBodyChange", function (evt, results) {
        var amendmentId = results.data.amendId;

        var upIx = arrayFirstIndexOf(self.amendments(), function (item) {
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
                var upIx = arrayFirstIndexOf(amendment.amendmentBodies(), function (item) {
                    return item.id() === results.id;
                });
                if (upIx > -1) {
                    var oldItem = amendment.amendmentBodies()[upIx];
                    var newItem = convertToObservable(results.data);
                    amendment.amendmentBodies.replace(oldItem, newItem);
                }
                break;
            case 3:
                amendment.amendmentBodies.remove(function (item) {
                    return item.id() === results.id;
                });
                break;
            default:
        }
    });
};

ko.applyBindings(new ScreenControlModel(initialData), document.getElementById("screenControl"));

