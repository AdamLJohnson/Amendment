var SystemSettingsModel = function(initialData) {
    var self = this;
    self.settings = ko.observableArray(convertArrayToObservable(initialData));
    self.saveSetting = function (setting) {
        $.ajax({
            type: "POST",
            url: "",
            data: ko.toJS(setting),
            dataType: 'json',
            success: function() {}
        });
    };
};

ko.applyBindings(new SystemSettingsModel(initialData), document.getElementById("viewModel"));