(function () {
    var room = $.connection.room;

    // -------------------- Connection --------------------

    $.connection.hub.start() // connection to SignalR, not to any specific hub
        .done(function () {
            $.connection.hub.logging = true; // turn off in production so users can't see too much in the dev tools
            //writeToPage("Connection to SingalR suceeded.");
            $.connection.hub.log("Connected!");
            //room.server.getBookedSeats();
        })
        .fail(function () {
            //writeToPage("Connection to SignalR failed.");
        });

    room.client.connectedUsers = function (list) {
        var users = JSON.parse(list);
        document.getElementById("userList").innerHTML = "";
        for (i = 0; i < users.length; i++) {
            $("#userList").append("&#x25BB " + users[i] + "<br />");
        };
    };

    // -------------------- Play Card --------------------

    playCardRequest = function () {
        disableCards();
        document.getElementById(this.id).hidden = true;
        card = this.src.substr(this.src.length - 9, 5);
        room.server.declareExtras(card);
    };

    disableCards = function () {
        for (i = 0; i < 8; i++) {
            document.getElementById("card".concat(String(i))).onclick = "";
        }
        document.getElementById("cardboard").classList.remove("card-board-pulse");
    };

    room.client.enableCards = function (validcards) {
        for (i = 0; i < 8; i++) { // change to for each element in received integer array
            if (validcards[i] == 1) document.getElementById("card".concat(String(i))).onclick = playCardRequest;
        }
        document.getElementById("cardboard").classList.add("card-board-pulse");
    };

    room.client.setTableCard = function (tableCardPosition, tableCard) {
        document.getElementById("tablecard".concat(tableCardPosition)).src = document.URL.substring(0, document.URL.length - 11).concat("Images/Cards/").concat(tableCard).concat(".png");
    };

    // -------------------- Turn Indicator --------------------

    room.client.setTurnIndicator = function (turn) {
        const icons = ["bi bi-arrow-left-circle-fill", "bi bi-arrow-up-circle-fill", "bi bi-arrow-right-circle-fill", "bi bi-arrow-down-circle-fill", "bi bi-suit-spade-fill"];
        document.getElementById("turnIndicator").classList = icons[turn];
    };

    // -------------------- Emote --------------------

    room.client.showEmote = function (turn) {
        seat = turn.concat("bubble");
        document.getElementById(seat).style.visibility = "visible";
    };
    room.client.hideEmote = function (turn) {
        seat = turn.concat("bubble");
        document.getElementById(seat).style.visibility = "hidden";
    };

    // -------------------- Round Summary --------------------


    room.client.showRoundSummary = function (trickPoints, declarationPoints, belotPoints, result, ew, ns) {

        document.getElementById("summary-belot-EW").innerHTML = belotPoints[0];
        document.getElementById("summary-belot-NS").innerHTML = belotPoints[1];
        document.getElementById("summary-dec-EW").innerHTML = declarationPoints[0];
        document.getElementById("summary-dec-NS").innerHTML = declarationPoints[1];
        document.getElementById("summary-tr-EW").innerHTML = trickPoints[0];
        document.getElementById("summary-tr-NS").innerHTML = trickPoints[1];
        document.getElementById("summary-sum-EW").innerHTML = belotPoints[0] + declarationPoints[0] + trickPoints[0];
        document.getElementById("summary-sum-NS").innerHTML = belotPoints[1] + declarationPoints[1] + trickPoints[1];
        document.getElementById("summary-game-EW").innerHTML = result[0];
        document.getElementById("summary-game-NS").innerHTML = result[1];
        document.getElementById("summary-total-EW").innerHTML = ew;
        document.getElementById("summary-total-NS").innerHTML = ns;
        $('#summary-modal').modal('show');
    }

    room.client.hideRoundSummary = function () {
        $('#summary-modal').modal('hide');
    }


    // -------------------- Reset --------------------

    room.client.closeModalsAndButtons = function () {
        $('#extras-modal').modal('hide');
        $('#suit-modal').modal('hide');
        $('#summary-modal').modal('hide');
        document.getElementById("dealBtn").classList.remove("deal-pulse");
        document.getElementById("suit-selector").style.visibility = "hidden";
        document.getElementById("suit-selector").classList.remove("suit-selector-pulse");
        document.getElementById("suit-selector").onclick = "";
    }

    resetTable = function () {
        //alert(document.URL);
        for (i = 0; i < 4; i++) {
            document.getElementById("tablecard".concat(String(i))).src = document.URL.substring(0, document.URL.length - 11).concat("Images/Cards/c0-00.png");
        }
    };

    resetBoard = function () {
        for (i = 0; i < 8; i++) {
            document.getElementById("card".concat(String(i))).hidden = true;
        };
    };

    resetSuitSelection = function () {
        document.getElementById("wnescallindicator").classList = "bi bi-arrows-move";
        document.getElementById("wnescallindicator").style.color = "dimgrey";
        document.getElementById("selectedsuit").classList = "bi bi-suit-spade-fill";
        document.getElementById("selectedsuit").style.color = "dimgrey";
        document.getElementById("selectedmultiplier").innerHTML = "";
    };

    $("#newGameBtn").on("click", function () {
        room.client.disableNewGame();
        room.server.newGame();
    });

    room.client.newGame = function () {
        //resetTable();
        //resetBoard();
        //disableCards();
        //resetSuitSelection();

        var table = document.getElementById("scoreTable");
        while (table.rows.length > 2) {
            table.deleteRow(1)
        };
        table.rows[1].cells[1].innerHTML = 0;
        table.rows[1].cells[2].innerHTML = 0;

        document.getElementById("scoreTotals").rows[1].cells[0].innerHTML = 0;
        document.getElementById("scoreTotals").rows[1].cells[1].innerHTML = 0;

        document.getElementById("0winnermarker").hidden = true;
        document.getElementById("1winnermarker").hidden = true;
        document.getElementById("2winnermarker").hidden = true;
        document.getElementById("3winnermarker").hidden = true;
    };

    room.client.showWinner = function (winner) {

        var marker = document.getElementById(String(winner).concat("winnermarker"));
        marker.hidden = !marker.hidden;

        //var max = 4, i = 0;
        ////var myvalues = ["test", "TEST"];
        //(function loop() {
        //    if (i++ > max) return;
        //    //var index = i % 2;
        //    setTimeout(function () {
        //        marker.hidden = !marker.hidden;
        //        loop();
        //    }, 400)
        //}());
    };

    room.client.updateScoreTotals = function (ewPoints, nsPoints) {

        var table = document.getElementById("scoreTotals");

        table.rows[1].cells[0].innerHTML = nsPoints;
        table.rows[1].cells[1].innerHTML = ewPoints;

    };

    room.client.appendScoreTable = function (ewPoints, nsPoints) {

        var table = document.getElementById("scoreTable");

        var row = table.insertRow(table.rows.length - 1);

        var cell1 = row.insertCell(0);
        var cell2 = row.insertCell(1);
        var cell3 = row.insertCell(2);

        if (table.rows.length == 3) {
            cell1.innerHTML = 1;
        }
        else {
            cell1.innerHTML = parseInt(table.rows[table.rows.length - 3].cells[0].innerHTML) + 1;
        };

        cell2.innerHTML = nsPoints;
        cell3.innerHTML = ewPoints;

        var totalNS = 0;
        var totalEW = 0;
        for (i = 1; i < table.rows.length - 1; i++) {
            totalNS += parseInt(table.rows[i].cells[1].innerHTML);
            totalEW += parseInt(table.rows[i].cells[2].innerHTML);
        };
        table.rows[table.rows.length - 1].cells[1].innerHTML = totalNS;
        table.rows[table.rows.length - 1].cells[2].innerHTML = totalEW;

    };

    room.client.newTrick = function () {
        resetTable();
    };

    room.client.disableDealBtn = function () {
        document.getElementById("dealBtn").disabled = true;
    };

    room.client.enableDealBtn = function () {
        document.getElementById("dealBtn").disabled = false;
        document.getElementById("dealBtn").classList.add("deal-pulse");
    };

    room.client.newRound = function () {
        resetTable();
        resetBoard();
        disableCards();
        resetSuitSelection();
    };

    // -------------------- Deal Cards --------------------

    $("#dealBtn").on("click", function () {
        document.getElementById("dealBtn").disabled = true;
        document.getElementById("dealBtn").classList.remove("deal-pulse");
        resetTable();
        room.server.shuffle();
    });

    room.client.setDealerMarker = function (turn) {
        for (i = 0; i < 4; i++) {
            var seat = getSeatNameByNumber(i);
            if (i == turn) {
                setSeatColour(seat, "darkmagenta");
            }
            else setSeatColour(seat, "#0d6efd");
        };
    };

    room.client.disableRadios = function () {
        // disable team selection
        const radios = ["w", "n", "s", "e", "x"];
        for (i = 0; i < 5; i++) {
            document.getElementById(radios[i].concat("radio")).disabled = true;
        }
    };

    room.client.enableRadios = function () {
        // disable team selection
        const radios = ["w", "n", "s", "e", "x"];
        for (i = 0; i < 5; i++) {
            document.getElementById(radios[i].concat("radio")).disabled = false;
        }
    };

    room.client.deal = function (cards) {
        // show hand card images
        var card = JSON.parse(cards);
        for (i = 0; i < card.length; i++) {
            document.getElementById("card".concat(String(i))).src = document.URL.substring(0, document.URL.length - 11).concat("Images/Cards/").concat(card[i], ".png");
            document.getElementById("card".concat(String(i))).hidden = false;
        };
    };

    // -------------------- Extra Points --------------------

    room.client.extras = function (extras, tableCard, overlaps) {
        extras = JSON.parse(extras);
        for (i = 0; i < extras.length; i++) {
            const dv = document.createElement('div');
            dv.id = "extra-items".concat(i);

            const box = document.createElement('input');
            box.classList.add("form-check-input");
            box.type = ("checkbox");
            if (overlaps[i]) {
                box.checked = false;
                box.disabled = true;
            }
            else {
                box.checked = true;
            };
            box.id = extras[i];

            const lbl = document.createElement('label');
            lbl.classList.add("form-check-label");
            lbl.style = "margin-left: 10px";
            lbl.for = extras[i];
            lbl.innerHTML = extras[i];

            dv.appendChild(box);
            dv.appendChild(lbl);
            document.getElementById("extras").appendChild(dv);
        };
        document.getElementById("tablecardimage-placeholder").classList = tableCard;
        $('#extras-modal').modal('show');
    };

    closeExtrasModal = function () {
        $('#extras-modal').modal('hide');

        var declared = [];

        var dv = document.getElementById('extras');
        var boxes = dv.children;
        for (i = 0; i < boxes.length; i++) {
            var box = boxes[i].children;
            if (box[0].checked) {
                declared.push(box[0].id);
            }
        };

        document.getElementById("extras").innerHTML = "";
        room.server.extrasDeclared(String(document.getElementById("tablecardimage-placeholder").classList), declared);
    };

    room.client.emoteExtras = function (extras, turn) {
        extras = JSON.parse(extras);
        seat = turn.concat("bubble");
        document.getElementById(seat).innerHTML = "";
        document.getElementById(seat).append(extras[0]);
        for (i = 1; i < extras.length; i++) {
            document.getElementById(seat).append("\n");
            document.getElementById(seat).append(extras[i]);
        };
    };


    // -------------------- Suit Selection --------------------

    room.client.showSuitModal = function (validCalls) {
        for (i = 0; i < 8; i++) {
            if (validCalls[i] == 1) {
                setSuitIconOn(i + 1);
            }
            else {
                setSuitIconOff(i + 1);
            };
        };
        $('#lobby').offcanvas('hide');
        window.scrollTo(0, 99999);
        $('#suit-modal').modal('show');
    }

    minimiseSuitModal = function () {
        $('#suit-modal').modal('hide');
        document.getElementById("suit-selector").style.visibility = "visible";
        document.getElementById("suit-selector").classList.add("suit-selector-pulse");
        document.getElementById("suit-selector").onclick = function () {
            $('#suit-modal').modal('show');
            document.getElementById("suit-selector").style.visibility = "hidden";
            document.getElementById("suit-selector").classList.remove("suit-selector-pulse");
            document.getElementById("suit-selector").onclick = "";
        };
    };

    room.client.emoteSuit = function (suit, turn) {
        seat = turn.concat("bubble");
        document.getElementById(seat).innerHTML = "";
        const emote = document.createElement('i');
        document.getElementById(seat).appendChild(emote);
        emote.style.fontSize = "2em";

        switch (suit) {
            case 0:
                document.getElementById(seat).innerHTML = "Pass";
                break;
            case 1:
                emote.style.color = "black";
                emote.classList = "bi bi-suit-club-fill";
                break;
            case 2:
                emote.style.color = "red";
                emote.classList = "bi bi-suit-diamond-fill";
                break;
            case 3:
                emote.style.color = "red";
                emote.classList = "bi bi-suit-heart-fill";
                break;
            case 4:
                emote.style.color = "black";
                emote.classList = "bi bi-suit-spade-fill";
                break;
            case 5:
                emote.style.color = "darkmagenta";
                emote.classList = "bi bi-x-diamond";
                break;
            case 6:
                emote.style.color = "darkmagenta";
                emote.classList = "bi bi-x-diamond-fill";
                break;
            case 7:
                document.getElementById(seat).innerHTML = "Double!";
                break;
            case 8:
                document.getElementById(seat).innerHTML = "Redouble!!";
                break;
        };
    };

    setSuitIconOff = function (suit) {
        document.getElementById("suit".concat(String(suit))).onclick = "";
        document.getElementById("suit".concat(String(suit))).style.color = "dimgrey";
    };

    setSuitIconOn = function (suit) {
        if (suit == 1 || suit == 4 || suit == 7 || suit == 8) {
            document.getElementById("suit".concat(String(suit))).style.color = "black";
        }
        else if (suit == 2 || suit == 3) {
            document.getElementById("suit".concat(String(suit))).style.color = "red";
        }
        else if (suit == 5 || suit == 6) {
            document.getElementById("suit".concat(String(suit))).style.color = "darkmagenta";
        };

        document.getElementById("suit".concat(String(suit))).onclick = function () { nominateSuit(this) };
    };

    nominateSuit = function (el) {
        $('#suit-modal').modal('hide');
        room.server.nominateSuit(el.id.charAt(el.id.length - 1));
    };

    room.client.suitNominated = function (suit) {
        if (suit < 7) {
            document.getElementById("selectedmultiplier").innerHTML = "";
        }
        else {

            if (document.getElementById("selectedsuit").style.color == "black") {
                document.getElementById("selectedmultiplier").style.color = "red";
            }
            else {
                document.getElementById("selectedmultiplier").style.color = "black";
            };
        };
        switch (suit) {
            case 1:
                document.getElementById("selectedsuit").style.color = "black";
                document.getElementById("selectedsuit").classList = "bi bi-suit-club-fill";
                break;
            case 2:
                document.getElementById("selectedsuit").style.color = "red";
                document.getElementById("selectedsuit").classList = "bi bi-suit-diamond-fill";
                break;
            case 3:
                document.getElementById("selectedsuit").style.color = "red";
                document.getElementById("selectedsuit").classList = "bi bi-suit-heart-fill";
                break;
            case 4:
                document.getElementById("selectedsuit").style.color = "black";
                document.getElementById("selectedsuit").classList = "bi bi-suit-spade-fill";
                break;
            case 5:
                document.getElementById("selectedsuit").style.color = "darkmagenta";
                document.getElementById("selectedsuit").classList = "bi bi-x-diamond";
                break;
            case 6:
                document.getElementById("selectedsuit").style.color = "darkmagenta";
                document.getElementById("selectedsuit").classList = "bi bi-x-diamond-fill";
                break;
            case 7:
                document.getElementById("selectedmultiplier").innerHTML = "x2";
                break;
            case 8:
                document.getElementById("selectedmultiplier").innerHTML = "x4";
                break;
        };

    };

    room.client.setCallerIndicator = function (turn) {
        if (turn == 0) {
            document.getElementById("wnescallindicator").classList = "bi bi-arrow-left";
        }
        else if (turn == 1) {
            document.getElementById("wnescallindicator").classList = "bi bi-arrow-up";
        }
        else if (turn == 2) {
            document.getElementById("wnescallindicator").classList = "bi bi-arrow-right";
        }
        else {
            document.getElementById("wnescallindicator").classList = "bi bi-arrow-down";
        };
        document.getElementById("wnescallindicator").style.color = "black";
    };

    // -------------------- Seat Management --------------------

    requestSeatBooking = function (seat) {
        room.server.bookSeat(seat);
    };

    getSeatNameByNumber = function (number) {
        var seat = "";
        switch (number) {
            case 0:
                seat = "West";
                break;
            case 1:
                seat = "North";
                break;
            case 2:
                seat = "East";
                break;
            case 3:
                seat = "South";
                break;
            case 4:
                seat = "XSpectator";
                break;
        };
        return seat;
    };

    room.client.enableNewGame = function () {
        document.getElementById("newGameBtn").classList.add("deal-pulse");
        document.getElementById("newGameBtn").disabled = false;
    };

    room.client.disableNewGame = function () {
        document.getElementById("newGameBtn").classList.remove("deal-pulse");
        document.getElementById("newGameBtn").disabled = true;
    };

    setSeatColour = function (seat, colour) {
        document.getElementById(seat.charAt(0).toLowerCase().concat("labelbadge")).style.backgroundColor = colour;
    };

    room.client.seatBooked = function (position, username) {
        var seat = getSeatNameByNumber(position);
        document.getElementById(seat.charAt(0).toLowerCase().concat("label")).innerHTML = username;
        setSeatColour(seat, "#0d6efd");
    };

    room.client.seatUnbooked = function (position, username) {
        var seat = getSeatNameByNumber(position);
        document.getElementById(seat.charAt(0).toLowerCase().concat("label")).innerHTML = seat;
        setSeatColour(seat, "black");
    };

    room.client.seatAlreadyBooked = function (occupier) {
        document.getElementById("seat-modal-body").innerHTML = "This seat is already occupied by ".concat(occupier).concat(".");
        $('#seat-modal').modal('show');
    };

    room.client.setRadio = function (pos) {
        document.getElementById("wradio").classList.remove("active");
        document.getElementById("nradio").classList.remove("active");
        document.getElementById("sradio").classList.remove("active");
        document.getElementById("eradio").classList.remove("active");
        document.getElementById("xradio").classList.remove("active");
        document.getElementById(pos.charAt(0).toLocaleLowerCase().concat("radio")).classList.add("active");
    };

    room.client.setBotBadge = function (pos, isBot) {
        document.getElementById(pos.charAt(0).toLocaleLowerCase().concat("BotBadge")).hidden = !isBot;
    };

    // -------------------- Chat Log --------------------

    room.client.announce = function (message) {
        writeToPage(message);
    };

    room.client.showChatNotification = function () {
        chatOpen = document.getElementById("chatLogAccordian").classList.contains("show");
        lobbyOpen = document.getElementById("lobby").classList.contains("show");
        if (!lobbyOpen || (lobbyOpen && !chatOpen)) {
            document.getElementById("chatLogBadge").hidden = false
        }
    };

    writeToPage = function (message) {
        if (document.getElementById("chatLog").value != "") {
            $("#chatLog").append("\n");
        };
        $("#chatLog").append(message);
        var textarea = document.getElementById('chatLog');
        textarea.scrollTop = textarea.scrollHeight;
    };

    $("#messageToSend").keyup(function (event) {
        if (event.keyCode === 13) {
            $("#sendmessage").click();
        }
    });

    $("#sendmessage").on("click", function () {
        room.server.announce(document.getElementById("messageToSend").value);
        document.getElementById("messageToSend").value = "";
    });

    $("#chatLogBtn").on("click", function () {
        //alert(document.getElementById("chatLogAccordian").classList.contains("collapsing"));
        document.getElementById("chatLogBadge").hidden = true;
    });

    $("#chatbtn").on("click", function () {
        chatOpen = document.getElementById("chatLogAccordian").classList.contains("show");
        lobbyOpening = document.getElementById("lobby").classList.contains("showing");
        if (lobbyOpening && chatOpen) {
            document.getElementById("chatLogBadge").hidden = true;
        }
    });

})();