"use strict";

var SystemSettingsModel = function SystemSettingsModel(initialData) {
    var self = this;
    self.settings = ko.observableArray(convertArrayToObservable(initialData));
    self.saveSetting = function (setting) {
        $.ajax({
            type: "POST",
            url: "",
            data: ko.toJS(setting),
            dataType: 'json',
            success: function success() {}
        });
    };
};

ko.applyBindings(new SystemSettingsModel(initialData), document.getElementById("viewModel"));

