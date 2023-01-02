(function () {
    var room = $.connection.room;
    $.connection.hub.start() // connection to SignalR, not to any specific hub
        .done(function () {
            writeToPage("Connection suceeded.");
            room.server.announce("howdy");
        })
        .fail(function () { writeToPage("Connection to SignalR failed."); });

        room.client.announce = function (message) {
        writeToPage(message);
    };

    var writeToPage = function (message) {
        $("#chatLog").append(message + "<br />");
    }
})();