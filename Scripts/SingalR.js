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

    room.client.connectedUsers = function (players, spectators) {
        document.getElementById("userList").innerHTML = "";
        for (i = 0; i < players.length; i++) {
            $("#userList").append("&#x25AA " + players[i] + "<br />");
        };
        for (i = 0; i < spectators.length; i++) {
            $("#userList").append("&#x25AB " + spectators[i] + "<br />");
        };
    };

    // -------------------- Play Card --------------------

    playCardRequest = function () {
        room.client.disableCards();
        hideThrowBtn();
        //document.getElementById(this.id).hidden = true;
        room.client.hideCard(this.id);
        card = this.src.substr(this.src.length - 9, 5);
        room.server.hubPlayCard(card);
    };

    room.client.playFinalCard = function () {
        room.client.disableCards();
        for (i = 0; i < 8; i++) {
            room.client.hideCard("card".concat(i));
        }
    };

    hideThrowBtn = function () {
        document.getElementById("throw-cards-btn").hidden = true;
        document.getElementById("throw-cards-btn").onclick = "";
    };

    room.client.showThrowBtn = function () {
        document.getElementById("throw-cards-btn").hidden = false;
        document.getElementById("throw-cards-btn").onclick = function () {
            hideThrowBtn();
            room.server.throwCards();
        };
    };

    room.client.throwCards = function (player, hand) {
        hand = JSON.parse(hand);
        pos = [ "w", "n", "e", "s"];
        document.getElementById("throw-modal-title").innerHTML = player.concat(" throws the cards!");
        for (i = 0; i < 8; i++) {
            for (j = 0; j < 4; j++) {
                if (hand[j][i] == "c0-00") {
                    document.getElementById(pos[j].concat("throwcard").concat(i)).hidden = true;
                }
                else {
                    var path = document.URL.substring(0, document.URL.indexOf("Room"));
                    document.getElementById(pos[j].concat("throwcard").concat(i)).src = path.concat("Images/Cards/", hand[j][i], ".png");
                    document.getElementById(pos[j].concat("throwcard").concat(i)).hidden = false;
                };
            };
        };
        $('#throw-modal').modal('show');
    };

    closeThrowModal = function () {
        $('#throw-modal').modal('hide');
    };

    room.client.closeThrowModal = function () {
        closeThrowModal();
    };

    rotateCards = function () {

        rotation = 8;

        var children = document.getElementById("cardboard").children;
        var visibleChildren = 8;
        for (var i = 0; i < children.length; i++) {
            if (children[i].hidden == true) visibleChildren--;
        }
        var count = 0;
        for (var i = 0; i < children.length; i++) {
            if (children[i].hidden == false) {
                //if (visibleChildren == 4) alert(i);
                var child = children[i];
                child.style.transform = "translate(-5%,".concat(0).concat("%) rotate(").concat(-3.5 * rotation + .5 * rotation * (8 - visibleChildren) + rotation * count + 1).concat("deg)");
                //if (visibleChildren == 4) alert(-35 + 5 * (8 - visibleChildren) + 10 * count);
                //alert(child.style.transform);
                count++;
            }
        }
    };

    room.client.hideCard = function (cardId) {
        document.getElementById(cardId).hidden = true;
        rotateCards();
    };

    room.client.showCard = function (cardId) {
        document.getElementById(cardId).hidden = false;
    };

    room.client.disableCards = function () {
        for (i = 0; i < 8; i++) {
            document.getElementById("card".concat(i)).onclick = "";
        }
        document.getElementById("cardboard").classList.remove("card-board-pulse");
    };

    room.client.enableCards = function (validcards) {
        for (i = 0; i < 8; i++) { // change to for each element in received integer array
            if (validcards[i] == 1) document.getElementById("card".concat(i)).onclick = playCardRequest;
        }
        document.getElementById("cardboard").classList.add("card-board-pulse");
    };

    room.client.setTableCard = function (tableCardPosition, tableCard) {
        var path = document.URL.substring(0, document.URL.indexOf("Room"));
        document.getElementById("tablecard".concat(tableCardPosition)).src = path.concat("Images/Cards/", tableCard, ".png");
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
        document.getElementById("make-call-btn").hidden = true;
        document.getElementById("make-call-btn").onclick = "";
    }

    resetTable = function () {
        //alert(document.URL);
        path = document.URL.substring(0, document.URL.indexOf("Room")).concat("Images/Cards/c0-00.png");
        for (i = 0; i < 4; i++) {
            document.getElementById("tablecard".concat(i)).src = path;
        }
    };

    resetBoard = function () {
        for (i = 0; i < 8; i++) {
            document.getElementById("card".concat(i)).hidden = true;
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
        room.server.gameController();
    });

    room.client.newGame = function () {

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

    room.client.showTrickWinner = function (winner) {

        document.getElementById("tablecard".concat(winner)).classList.add("winning-card-pulse");
        document.getElementById("tablecard".concat(winner)).style.zIndex = 2;
        setTimeout(function () {
            document.getElementById("tablecard".concat(winner)).classList.remove("winning-card-pulse", "z-2");
            document.getElementById("tablecard".concat(winner)).style.zIndex = 1;
        }, 1000)
    };

    room.client.showGameWinner = function (winner) {


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

    room.client.appendScoreHistory = function (ewPoints, nsPoints) {

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

    room.client.resetTable = function () {
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
        room.client.disableCards();
        resetSuitSelection();
    };

    // -------------------- Deal Cards --------------------

    $("#dealBtn").on("click", function () {
        document.getElementById("dealBtn").disabled = true;
        document.getElementById("dealBtn").classList.remove("deal-pulse");
        resetTable();
        room.server.hubShuffle();
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
        var path = document.URL.substring(0, document.URL.indexOf("Room"));
        for (i = 0; i < card.length; i++) {
            document.getElementById("card".concat(i)).src = path.concat("Images/Cards/", card[i], ".png");
            //document.getElementById("card".concat(i)).hidden = false;
            room.client.showCard("card".concat(i));
        };
        rotateCards();
    };

    // -------------------- Extra Points --------------------

    room.client.declareExtras = function (extras) {
        extras = JSON.parse(extras);
        if (extras.length > 0) {
            for (i = 0; i < extras.length; i++) {
                const dv = document.createElement('div');
                //dv.id = "extra-items".concat(i);

                const box = document.createElement('input');
                box.classList.add("form-check-input");
                box.type = ("checkbox");
                if (extras[i].charAt(0) == "#") {
                    box.checked = false;
                    box.disabled = true;
                    extras[i] = extras[i].substring(1, extras[i].length - 1);
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
            $('#extras-modal').modal('show');
        }
        else {
            var belot = false;
            var runs = [];
            var carres = [];
            room.server.hubExtrasDeclared(belot, runs, carres);
        };
    };

    closeExtrasModal = function () {
        $('#extras-modal').modal('hide');

        var belot = false;
        var runs = [];
        var carres = [];

        var dv = document.getElementById('extras');
        var boxes = dv.children;
        for (i = 0; i < boxes.length; i++) {
            var box = boxes[i].children;
            if (box[0].id.charAt(0) == "B") belot = box[0].checked;
            else if (box[0].id.charAt(0) == "C") carres.push(box[0].checked);
            else runs.push(box[0].checked);
        };

        document.getElementById("extras").innerHTML = "";
        room.server.hubExtrasDeclared(belot, runs, carres);
    };

    room.client.setExtrasEmote = function (extras, turn) {
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
        //window.scrollTo(0, 99999);
        $('#suit-modal').modal('show');
    }

    minimiseSuitModal = function () {
        $('#suit-modal').modal('hide');
        document.getElementById("make-call-btn").hidden = false;
        document.getElementById("make-call-btn").onclick = function () {
            $('#suit-modal').modal('show');
            document.getElementById("make-call-btn").hidden = true;
            document.getElementById("make-call-btn").onclick = "";
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
            document.getElementById("suit".concat(String(suit))).style.color = "darkmagenta";
        };

        document.getElementById("suit".concat(String(suit))).onclick = function () { nominateSuit(this) };
    };

    nominateSuit = function (el) {
        $('#suit-modal').modal('hide');
        room.server.hubNominateSuit(el.id.charAt(el.id.length - 1));
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

    moveWest = function () {
        const w = document.getElementById("west");
        w.style.top = 0;
        w.style.left = 0;
    };


    // -------------------- Chat Log --------------------

    copyGameInvite = function () {
        navigator.clipboard.writeText(document.URL);
    }

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