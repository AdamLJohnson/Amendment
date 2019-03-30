﻿var AmendmentModel = function (initData) {
    var amendmentUpdatesConnection = ManageAmendmentHub();
    var self = this;
    self.amendments = ko.observableArray();
    self.amendBody = ko.observable(initData.amendBody);
    self.amendBodyId = amendBodyId;
    self.ShowDeafSigner = ko.observable("0");
    self.ShowDeafSignerBox = ko.observable("0");
    self.page = ko.observable(-1);
    var saveClicked = false;
    self.amendBodyPages = ko.computed(function () {
        if (!self.amendBody()) {
            return { amendBodyPages: [""] };
        }
        return {
            amendBodyPages: self.amendBody().split("**NEWSLIDE**").map(function (b) {
                return marked(b, { breaks: true });
            })
        };
    });

    amendmentUpdatesConnection.on("amendment.amendmentReturn", function (results) {
        newFunction(results, self);
    });

    $(document).on("amendment.amendmentChange", function (evt, results) {
        newFunction(results.data, self);
    });

    $(document).on("amendment.ready", function (evt) {
        amendmentUpdatesConnection.invoke("getAmendment", amendmentId);
        amendmentUpdatesConnection.invoke("getSystemSetting", "ShowDeafSigner");
        amendmentUpdatesConnection.invoke("getSystemSetting", "ShowDeafSignerBox");
    });

    $(document).on("amendment.reconnect", function (evt) {
        amendmentUpdatesConnection.invoke("getAmendment", amendmentId);
        amendmentUpdatesConnection.invoke("getSystemSetting", "ShowDeafSigner");
        amendmentUpdatesConnection.invoke("getSystemSetting", "ShowDeafSignerBox");
    });

    amendmentUpdatesConnection.on("RefreshSetting", function (results) {
        updateSetting(self, results);
    });

    self.onSaveClicked = function () {
        saveClicked = true;
        return true;
    };

    $(document).on("amendment.amendmentBodyChange", function (evt, results) {
        var amendmentId = results.data.amendId;

        if (results.data.id === self.amendBodyId && !saveClicked) {
            $("#concurrencyWarning").removeClass("hidden");
        }

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

    let editors = $(".markdown-editor");
    if (editors.length > 0) {
        self.editor = new SimpleMDE({ element: editors[0], forceSync: true, spellChecker: false, hideIcons: ["guide", "preview", "side-by-side", "fullscreen"], showIcons: ["strikethrough"] });
        self.editor.codemirror.on("change",
            function () {
                self.amendBody($("#AmendBody").val());
            });
    }

    $('#insertPage').click(function () {
        var pos = self.editor.codemirror.getCursor();
        self.editor.codemirror.setSelection(pos, pos);
        self.editor.codemirror.replaceSelection("**NEWSLIDE**");
    });


    //var o = self.amendBody() || "";
    //const diffConnection = new signalR.HubConnectionBuilder().withUrl("/diffHub?pageUrlHash=" + window.PageUrlHash).build();
    //diffConnection.on("receivePatch",
    //    (patch) => {
    //        var dmp = new diff_match_patch();
    //        var p = dmp.patch_fromText(patch);
    //        o = dmp.patch_apply(p, o)[0];
    //        console.log("testre", p);
    //        console.log("receivePatch", patch);

    //        var c = self.editor.codemirror.getCursor();

    //        self.amendBody(o);
    //        self.editor.value(o);
    //        self.editor.codemirror.setCursor(c);
    //    });
    //diffConnection.start();
    
    //self.amendBody.subscribe(function (newValue) {
    //    var dmp = new diff_match_patch();
    //    var diff = dmp.diff_main(o, newValue);
    //    var p = dmp.patch_make(diff);
    //    var t = dmp.patch_toText(p);
    //    if (t) {
    //        console.log("p", p);
    //        console.log("xmitPatch", t);
    //        diffConnection.invoke("xmitPatch", window.PageUrlHash, t);
    //    }
    //    o = newValue;
    //});
};


function newFunction(results, self) {
    if (results.id !== amendmentId) {
        return;
    }
    var upIx = arrayFirstIndexOf(self.amendments(),
        function (item) {
            return item.id() === results.id;
        });
    if (upIx > -1) {
        var oldItem = self.amendments()[upIx];
        var newItem = convertToObservable(results);
        self.amendments.replace(oldItem, newItem);
    } else {
        self.amendments.push(convertToObservable(results));
    }
}


ko.applyBindings(new AmendmentModel(initialData));