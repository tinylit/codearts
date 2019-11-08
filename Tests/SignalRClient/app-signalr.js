(function ($) {
    var
        core_arr = [],
        core_slice = core_arr.slice;
    function init(url, hubName, option) {
        if (url[url.length - 1] === "/") {
            url = url.slice(0, -1);
        }
        if (url.toLowerCase().indexOf("signalr") < (url.length - 7)) {
            url += "/signalr";
        }
        //管理员:yep m6hxsjQkEikKLycHd1lWpdMMSGa69rBkpmWXd/+g2yDG8oMR6RgZSA3umJWOvYTbKQWkvi+7NiEta550Jvs8qRQzG7GDg8PAHUrQZTuT+25kw/XuptqTrBvLj2xKHOYivRbyfCZhWmfSiWlHKSFkpG5AtoQ+x68WDWNBjHLJ6IQGkA2FYrvzj7jd59FdK3fFec2393zUIS81cfrqH177wFMBL+NJS9MM
        //用户: yep j08UGa9MWLNjQW2n6vYC92rIxDat20JVmxCyjg3yB0FBonmlEJJg9XDxx8S9DBZg47uR/nTXPTXBdCsdxrn9Si09DC4gsR5q4ReKuQ0qvYkgwDR0SVRNTiYsBrl2heOMoIDv+XWziH8VP3LHDl1QdKeE2VxjgjCPWOPiMDvGv0t/PZ8aesnIFCemJ9XTp2JOBLt9+UMIR/4Fj0FDRHK/c5LBi0lUw4+c
        var connection = $.hubConnection(url, { useDefaultPath: false, qs: { token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJhcGkiLCJpc3MiOiJ5ZXAiLCJuYW1laWQiOiIxMDAwMDAiLCJpZCI6MTAwMDAwLCJ1bmlxdWVfbmFtZSI6Imh5bCIsIm5hbWUiOiJoeWwiLCJuYmYiOjE1NzMxOTA5OTksImV4cCI6MTU3MzI3NzM5OSwiaWF0IjoxNTczMTkwOTk5fQ.A24Ar4Puwqmsf8Yshvs3Qz2QF5-n8k2vbkVDP84cVJc" } });
        var hub = connection.createHubProxy(hubName);
        var type = $.type(option);
        if (type === "function") {
            option.call(connection, hub);
        }
        if (type === "object") {
            $.each(option, function (method, callback) {
                hub.on(method, callback);
            });
        }
        var core_invoke = hub.invoke;
        return $.extend(connection, {
            restart: function (callback) {
                var state = connection.state;
                if (state < 4) return true;
                var option = connection.start();
                if (callback) {
                    option.done(callback);
                }
                return option;
            },
            invoke: function () {
                return core_invoke.apply(hub, core_slice.call(arguments));
            },
            on: function (method, callback) {
                return hub.on(hub, method, callback);
            }
        });
    }
    $.hubYep = init;
})(jQuery);