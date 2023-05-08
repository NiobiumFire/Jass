(function () {
    var room = $.connection.trainingroom;

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

    // -------------------- Play Card --------------------

    room.client.setTableCard = function (tableCardPosition, tableCard) {

        seat = ["w", "n", "e", "s"];
        for (i = 0; i < 4; i++) {
            for (j = 0; j < 8; j++) {
                var src = document.getElementById(seat[i].concat("card").concat(j)).src;
                if (src.indexOf(tableCard) > -1) {
                    document.getElementById(seat[i].concat("card").concat(j)).hidden = true;
                    j = 8;
                    i = 4;
                }
            };
        };

        var path = document.URL.substring(0, document.URL.indexOf("Training"));
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


    resetTable = function () {
        //alert(document.URL);
        path = document.URL.substring(0, document.URL.indexOf("Training")).concat("Images/Cards/c0-00.png");
        for (i = 0; i < 4; i++) {
            document.getElementById("tablecard".concat(String(i))).src = path;
        }
    };

    resetBoard = function () {
        seat = ["w", "n", "e", "s"];
        for (i = 0; i < 4; i++) {
            for (j = 0; j < 8; j++) {
                document.getElementById(seat[i].concat("card").concat(j)).hidden = true;
            };
        };
    };

    resetSuitSelection = function () {
        document.getElementById("wnescallindicator").classList = "bi bi-arrows-move";
        document.getElementById("wnescallindicator").style.color = "dimgrey";
        document.getElementById("selectedsuit").classList = "bi bi-suit-spade-fill";
        document.getElementById("selectedsuit").style.color = "dimgrey";
        document.getElementById("selectedmultiplier").innerHTML = "";
    };


    room.client.newGame = function () {
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

    room.client.resetTable = function () {
        resetTable();
    };

    room.client.newRound = function () {
        resetTable();
        resetBoard();
        resetSuitSelection();
    };

    // -------------------- Deal Cards --------------------

    room.client.setDealerMarker = function (turn) {
        for (i = 0; i < 4; i++) {
            var seat = getSeatNameByNumber(i);
            if (i == turn) {
                setSeatColour(seat, "darkmagenta");
            }
            else setSeatColour(seat, "#0d6efd");
        };
    };

    room.client.deal = function (cards, player) {
        // show hand card images
        cards = JSON.parse(cards);
        var path = document.URL.substring(0, document.URL.indexOf("Training"));
        seat = ["w", "n", "e", "s"];
        for (i = 0; i < cards.length; i++) {
            document.getElementById(seat[player].concat("card").concat(i)).src = path.concat("Images/Cards/", cards[i], ".png");
            document.getElementById(seat[player].concat("card").concat(i)).hidden = false;
        }
    };

    // -------------------- Extra Points --------------------

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
        };
        return seat;
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


    // -------------------- Load Game --------------------

    $("#gameGUID").keyup(function (event) {
        if (event.keyCode === 13) {
            $("#load-game").click();
        }
    });

    $("#load-game").on("click", function () {
        room.server.loadGame(document.getElementById("gameGUID").value);
        document.getElementById("gameGUID").value = "";
    });

    // -------------------- Game Speed --------------------

    $("#game-speed-btn").on("click", function () {
        var delay = 600;

        if (!isNaN(parseInt(document.getElementById("speed-value").value))) delay = parseInt(document.getElementById("speed-value").value);
        if (delay > 5000 || delay < 0) delay = 600;

        document.getElementById("speed-value").value = delay;
        room.server.setGameSpeed(delay);
    });

    // -------------------- Pause --------------------

    $("#pause-game").on("click", function () {
        if (document.getElementById("pause-icon").classList.contains("bi-pause-circle-fill")) {
            document.getElementById("pause-icon").classList = "bi bi-play-circle-fill";
            room.server.pauseGame(true);
        }
        else {
            document.getElementById("pause-icon").classList = "bi-pause-circle-fill";
            room.server.pauseGame(false);
        };
    });

    room.client.togglePauseEnabled = function () {
        document.getElementById("pause-game").disabled = !document.getElementById("pause-game").disabled;
    };

    // -------------------- Generate Game --------------------

    $("#gameBtn").on("click", function () {
        room.server.generateGame(document.getElementById("num-games").value);
    });

    // -------------------- Append Metrics Table --------------------

    room.client.appendTrainingTable = function (metrics, bestGUID) {
        var table = document.getElementById("training-metrics-table");
        var row = table.insertRow(table.rows.length);
        for (i = 0; i < metrics.length; i++) {
            row.insertCell(i).innerHTML = metrics[i];
        }
        row.insertCell(metrics.length).innerHTML = bestGUID;
        document.getElementById("training-metrics").scrollTop = document.getElementById("training-metrics").scrollHeight;
    };

})();