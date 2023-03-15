export function EnterFullScreen() {
    var fullscreenDiv = document.getElementById("fullscreen");
    var fullscreenFunc = fullscreenDiv.requestFullscreen;
    if (!fullscreenFunc) {
        ['mozRequestFullScreen',
            'msRequestFullscreen',
            'webkitRequestFullScreen'].forEach(function (req) {
                fullscreenFunc = fullscreenFunc || fullscreenDiv[req];
            });
    }

    if (document.addEventListener) {
        document.addEventListener('webkitfullscreenchange', screenChangeHandler, false);
        document.addEventListener('mozfullscreenchange', screenChangeHandler, false);
        document.addEventListener('fullscreenchange', screenChangeHandler, false);
        document.addEventListener('MSFullscreenChange', screenChangeHandler, false);
    }

    function screenChangeHandler() {
        if (!document.webkitIsFullScreen && !document.mozFullScreen && !document.msFullscreenElement) {
            fullscreenDiv.removeAttribute("fullscreen");
        } else {
            fullscreenDiv.setAttribute("fullscreen", "true");
        }
    }

    fullscreenFunc.call(fullscreenDiv);
}