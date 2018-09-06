// text herer


var ScreenViewModel = function(initialData) {
    var self = this;
    self.hub = ScreenViewHub(initialData.language.id);
    self.amendmentIsLive = ko.observable(initialData.amendment.isLive);
    self.amendmentBodyIsLive = ko.observable(initialData.amendmentBody.isLive);
    self.amendmentBody = ko.observable(initialData.amendmentBody.amendBodyHtml);
    self.isLive = ko.computed(function () {
        return self.amendmentIsLive() && self.amendmentBodyIsLive();
    });
    var languageId = initialData.language.id;

    $(document).on("screen.amendmentBodyChange." + languageId, function (evt, results) {
        console.log(results);
        self.amendmentBody(results.amendBodyHtml);
        self.amendmentBodyIsLive(results.isLive);
    });

    $(document).on("screen.amendmentChange." + languageId, function (evt, results) {
        console.log(results);
        self.amendmentIsLive(results.isLive);
    });

    $(document).on("screen.clearScreens." + languageId, function (evt) {
        self.amendmentIsLive(false);
        self.amendmentBodyIsLive(false);
    });
};
ko.applyBindings(new ScreenViewModel(initialData), document.getElementById("viewModel"));


var fullscreenButton = document.getElementById("fullscreen-button");
var fullscreenDiv = document.getElementById("fullscreen");
var fullscreenFunc = fullscreenDiv.requestFullscreen;

if (!fullscreenFunc) {
    ['mozRequestFullScreen',
        'msRequestFullscreen',
        'webkitRequestFullScreen'].forEach(function (req) {
        fullscreenFunc = fullscreenFunc || fullscreenDiv[req];
    });
}

function enterFullscreen() {
    fullscreenFunc.call(fullscreenDiv);
}

fullscreenButton.addEventListener('click', enterFullscreen);