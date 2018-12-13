var AmendmentModel = function (initData) {
    var amendmentUpdatesConnection = ManageAmendmentHub();
    var self = this;
    self.amendments = ko.observableArray();
    self.amendBody = ko.observable(initData.amendBody);
    self.page = ko.observable(-1);
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

    let editors = $(".markdown-editor");
    if (editors.length > 0) {
        self.editor = new SimpleMDE({ element: editors[0], forceSync: true, spellChecker: false, hideIcons: ["guide", "preview", "side-by-side", "fullscreen"] });
        self.editor.codemirror.on("change",
            function () {
                self.amendBody($("#AmendBody").val());
            });
    }

    $('#insertPage').click(function () {
        var pos = self.editor.codemirror.getCursor();
        self.editor.codemirror.setSelection(pos, pos);
        self.editor.codemirror.replaceSelection("**NEWSLIDE**");

        //insertAtCaret('AmendBody', '**NEWSLIDE**');
    });
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