"use strict";

initScreenViewModel("viewModel", initialData.language.id, initialData.amendment, initialData.amendmentBody, initialData.language.languageName);

var fullscreenButton = document.getElementById("fullscreen-button");
var fullscreenDiv = document.getElementById("fullscreen");
var fullscreenFunc = fullscreenDiv.requestFullscreen;

if (!fullscreenFunc) {
    ['mozRequestFullScreen', 'msRequestFullscreen', 'webkitRequestFullScreen'].forEach(function (req) {
        fullscreenFunc = fullscreenFunc || fullscreenDiv[req];
    });
}

function enterFullscreen() {
    fullscreenFunc.call(fullscreenDiv);
}

function exitFullScreen() {
    var requestMethod = document.exitFullscreen || document.mozCancelFullScreen || document.webkitExitFullscreen || document.msExitFullscreen;
    if (requestMethod) {
        var q = requestMethod.bind(document);
        q();
    }
}

fullscreenButton.addEventListener('click', enterFullscreen);

if (document.addEventListener) {
    document.addEventListener('webkitfullscreenchange', exitHandler, false);
    document.addEventListener('mozfullscreenchange', exitHandler, false);
    document.addEventListener('fullscreenchange', exitHandler, false);
    document.addEventListener('MSFullscreenChange', exitHandler, false);
}

function exitHandler() {
    if (!document.webkitIsFullScreen && !document.mozFullScreen && !document.msFullscreenElement) {
        $("#fullscreen").removeClass("fullscreen");
    } else {
        $("#fullscreen").addClass("fullscreen");
    }
}

$(document).on("connection.error", function () {
    exitFullScreen();
});

