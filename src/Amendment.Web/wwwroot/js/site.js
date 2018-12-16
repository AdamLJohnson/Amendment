function ManageAmendmentHub() {
    const amendmentUpdatesConnection = new signalR.HubConnectionBuilder().withUrl("/amendmentHub").build();
    amendmentUpdatesConnection.onclose((e) => { showConnectionError(); });

    amendmentUpdatesConnection.on(_clientNotifierMethods.amendmentChange,
        (results) => {
            toastr.options.escapeHtml = true;
            var message = "Amendment " + results.id;
            switch (results.results.operationType) {
            case 1:
                message += " Created";
                break;
            case 2:
                message += " Updated";
                break;
            case 3:
                message += " Deleted";
                break;
            default:
            }
            toastr.info(message);

            jQuery.event.trigger("amendment.amendmentChange", results);
        });

    amendmentUpdatesConnection.on(_clientNotifierMethods.amendmentBodyChange,
        (results) => {
            toastr.options.escapeHtml = true;
            var message = "Amendment Body " + results.id;
            switch (results.results.operationType) {
            case 1:
                message += " Created";
                break;
            case 2:
                message += " Updated";
                break;
            case 3:
                message += " Deleted";
                break;
            default:
            }
            toastr.info(message);

            jQuery.event.trigger("amendment.amendmentBodyChange", results);
        });
    
    amendmentUpdatesConnection.start().catch(err => console.error(err.toString())).then(() => { jQuery.event.trigger("amendment.ready");});
    return amendmentUpdatesConnection;
}

function initScreenViewModel(htmlId, languageId, amendment, amendmentBody, languageName) {
    var ScreenViewModel = function () {
        var self = this;
        self.hub = ScreenViewHub(languageId);
        self.amendmentIsLive = ko.observable(amendment.isLive);
        self.amendmentBodyIsLive = ko.observable(amendmentBody.isLive);
        self.amendBodyPagedHtml = ko.observable(amendmentBody.amendBodyPagedHtml);
        self.page = ko.observable(amendmentBody.page);
        self.pages = ko.observable(amendmentBody.pages);
        self.language = languageName;
        self.languageId = languageId;
        self.isLive = ko.computed(function () {
            return self.amendmentIsLive() && self.amendmentBodyIsLive();
        });

        $(document).on("screen.amendmentBodyChange." + self.languageId, function (evt, results) {
            self.amendBodyPagedHtml(results.amendBodyPagedHtml);
            self.amendmentBodyIsLive(results.isLive);
            self.page(results.page);
            self.pages(results.pages);
        });

        $(document).on("screen.amendmentChange." + self.languageId, function (evt, results) {
            self.amendmentIsLive(results.isLive);
        });

        $(document).on("screen.clearScreens." + self.languageId, function (evt) {
            self.amendmentIsLive(false);
            self.amendmentBodyIsLive(false);
        });
    };
    ko.applyBindings(new ScreenViewModel(), document.getElementById(htmlId));
}

function ScreenViewHub(languageId)
{
    const screenUpdatesConnection = new signalR.HubConnectionBuilder().withUrl("/screenHub?languageId=" + languageId).build();
    screenUpdatesConnection.onclose((e) => { showConnectionError(); });
    screenUpdatesConnection.on(_clientNotifierMethods.clearScreens,
        () => {
            if (_usersRoles.indexOf('System Administrator') > -1) {
                toastr.options.escapeHtml = true;
                var message = "";
                toastr.info(message, _clientNotifierMethods.clearScreens);
            }

            jQuery.event.trigger("screen.clearScreens." + languageId);
        });

    screenUpdatesConnection.on(_clientNotifierMethods.amendmentBodyChange,
        (results) => {
            if (_usersRoles.indexOf('System Administrator') > -1) {
                toastr.options.escapeHtml = true;
                var message = "screen " + results.id;
                toastr.info(message, _clientNotifierMethods.amendmentBodyChange);
            }

            jQuery.event.trigger("screen.amendmentBodyChange." + languageId, results);
        });

    screenUpdatesConnection.on(_clientNotifierMethods.amendmentChange,
        (results) => {
            if (_usersRoles.indexOf('System Administrator') > -1) {
                toastr.options.escapeHtml = true;
                var message = "screen " + results.id;
                toastr.info(message, _clientNotifierMethods.amendmentChange);
            }

            jQuery.event.trigger("screen.amendmentChange." + languageId, results);
        });

    screenUpdatesConnection.start().catch(err => console.error(err.toString()));
    return screenUpdatesConnection;
}

function convertArrayToObservable(list) {
    var newList = [];
    $.each(list, function (i, obj) {
        var newObj = convertToObservable(obj);
        newList.push(newObj);
    });;
    return newList;
}

function convertToObservable(obj) {
    var newObj = {};
    if (typeof obj === 'object' && obj !== null) {
        Object.keys(obj).forEach(function (key) {
            if (Array.isArray(obj[key])) {
                newObj[key] = ko.observableArray(convertArrayToObservable(obj[key]));
            } else {
                newObj[key] = ko.observable(obj[key]);
            }
        });
    }
    else {
        return ko.observable(obj);
    }

    return newObj;

    
}

function arrayFirstIndexOf(array, predicate, predicateOwner) {
    for (var i = 0, j = array.length; i < j; i++) {
        if (predicate.call(predicateOwner, array[i])) {
            return i;
        }
    }
    return -1;
}

function userIsInRole() {
    if (arguments.length === 0) return false;

    for (var i = 0; i < arguments.length; i++) {
        var r = _usersRoles.indexOf(arguments[i]);
        if (r > -1) {
            return true;
        }
    }
    return false;
}

function showConnectionError() {
    $("#connection-error").removeClass("hidden");
    jQuery.event.trigger("connection.error");
}

//"use strict"; var _createClass = function () { function e(e, t) { for (var n = 0; n < t.length; n++) { var i = t[n]; i.enumerable = i.enumerable || !1, i.configurable = !0, "value" in i && (i.writable = !0), Object.defineProperty(e, i.key, i) } } return function (t, n, i) { return n && e(t.prototype, n), i && e(t, i), t } }(); function _classCallCheck(e, t) { if (!(e instanceof t)) throw new TypeError("Cannot call a class as a function") } var zoomFactor = function () { function e(t) { _classCallCheck(this, e), this.el = this.q(t, document), this.b(), this.u() } return _createClass(e, [{ key: "q", value: function (e) { return (arguments.length > 1 && void 0 !== arguments[1] ? arguments[1] : this.el).querySelector(e) } }, { key: "b", value: function () { var e = this.el.innerHTML, t = document.createElement("z-1"), n = document.createElement("z-2"), i = document.createElement("z-3"), l = document.createElement("style"); this.el.innerHTML = "", this.el.appendChild(t), t.appendChild(n), n.appendChild(i), i.innerHTML = e, l.appendChild(document.createTextNode("z-1,z-2,z-3,zoom-factor{display:block}z-1,zoom-factor{position:relative}z-1,z-2{width:100%}z-1,z-2,z-3{color:#fff}z-1{float:left;overflow:hidden}z-2{position:absolute}z-3{transform-origin:left top;width:0}")), document.getElementsByTagName("head")[0].appendChild(l) } }, { key: "v", value: function () { return this.q("input") ? this.q("input").value : parseFloat(this.el.dataset.scale) || 1 } }, { key: "u", value: function () { var e = this.v(), t = this.el, n = this.q("z-1"), i = this.q("z-2"), l = this.q("z-3"); n.style = i.style = l.style = t.style = "", i.style.width = n.clientWidth * e + "px", l.style.transform = "scale(" + e + ")", n.style.height = l.clientHeight * e + "px", t.style.height = n.style.height } }]), e }();

function myPostProcessingLogic(elements) {

    
    //new zoomFactor("zoom-factor");

    //$(elements[1]);
    //console.log(elements);
    //testZf = elements[1].querySelector(".zoom-factor");
    //testZF = new zoomFactor(elements[1].querySelector(".zoom-factor"));
    //sddsaf = testZF.update();
}

//class zoomFactor {
//    constructor(el) {
//        console.log(el);
//        this.el = el;
//        //this.query('input').addEventListener('input', () => this.update());
//        //window.addEventListener('resize', () => this.update());
//    }

//    query(s, el = this.el) {
//        return el.querySelector(s);
//    }

//    value() {
//        return .16;
//    }

//    update() {
//        var val = this.value();
//        var z1 = this.query('.z-1');
//        var z2 = this.query('.z-2');
//        var z3 = this.query('.z-3');

//        console.log(z1);
//        console.log(z2);
//        console.log(z3);
//        //z1.style = z2.style = z3.style = '';
//        //z2.style.width = z1.clientWidth * val + 'px';
//        //z1.style.width = z2.style.width;
//        z3.style.transform = 'scale(' + val + ')';
//        //z3.style.width = z2.clientWidth / val + 'px';
//        //z1.style.height = z3.clientHeight * val + 'px';

//        return { z1, z2, z3 };
//    }
//}