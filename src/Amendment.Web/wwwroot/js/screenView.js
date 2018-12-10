
initScreenViewModel("viewModel", initialData.language.id, initialData.amendment, initialData.amendmentBody, initialData.language.languageName);


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