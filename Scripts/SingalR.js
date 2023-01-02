(function () {
    $.connection.hub.start()
        .done(function () {
            console.log("It Worked");
            $.connection.room.server.announce("howdy");
        })
        .fail(function () { alert("fail"); });

    $.connection.room.client.announce = function (message) {
        $("#chatLog").append(message + "<br />");
    };
})();