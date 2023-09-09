(function () {
    var room = $.connection.replayroom;

    // -------------------- Connection --------------------

    $.connection.hub.start() // connection to SignalR, not to any specific hub
        .done(function () {
            $.connection.hub.logging = true; // turn off in production so users can't see too much in the dev tools
            $.connection.hub.log("Connected!");
        })
        .fail(function () {
        });


    // -------------------- Round --------------------
    // -------------------- Scores --------------------
    setScore = function (scores) {

        var table = document.getElementById("scoreTotals");

        table.rows[1].cells[0].innerHTML = scores[0];
        table.rows[1].cells[1].innerHTML = scores[1];

    };
    // -------------------- Dealer --------------------
    setDealer = function (dealer) {
        for (i = 0; i < 4; i++) {
            document.getElementById("dealermarker" + i).hidden = true;
        };
        if (dealer < 4) document.getElementById("dealermarker" + dealer).hidden = false;
    };
    // -------------------- RoundSuit --------------------
    setRoundSuit = function (suit) {
        suits = ["bi bi-suit-spade-fill", "bi bi-suit-club-fill", "bi bi-suit-diamond-fill", "bi bi-suit-heart-fill",
            "bi bi-suit-spade-fill", "i-notrumps", "i-alltrumps"];
        colours = ["dimgrey", "black", "red", "red", "black", "darkmagenta", "darkmagenta"];
        if (suit < 7) {
            document.getElementById("selectedmultiplier").innerHTML = "";
            document.getElementById("selectedsuit").style.color = colours[suit];
            document.getElementById("selectedsuit").classList = suits[suit];
        }
        else {
            if (document.getElementById("selectedsuit").style.color == "black") {
                document.getElementById("selectedmultiplier").style.color = "red";
            }
            else {
                document.getElementById("selectedmultiplier").style.color = "black";
            };
            if (suit == 7) document.getElementById("selectedmultiplier").innerHTML = "x2";
            else document.getElementById("selectedmultiplier").innerHTML = "x4";
        };
    };
    // -------------------- Caller --------------------
    setCaller = function (caller) {
        callers = ["bi bi-arrow-left", "bi bi-arrow-up", "bi bi-arrow-right", "bi bi-arrow-down", "bi bi-arrows-move"];
        document.getElementById("wnescallindicator").classList = callers[caller];
        if (caller == 4) document.getElementById("wnescallindicator").style.color = "dimgrey";
        else document.getElementById("wnescallindicator").style.color = "black";
    };
    // -------------------- Turn --------------------
    setTurn = function (turn) {
        const icons = ["bi bi-arrow-left-circle-fill", "bi bi-arrow-up-circle-fill", "bi bi-arrow-right-circle-fill", "bi bi-arrow-down-circle-fill", "bi bi-suit-spade-fill"];
        document.getElementById("turnIndicator").classList = icons[turn];
    };
    // -------------------- Emotes --------------------
    setEmote = function (emotes) {
        seats = ["w", "n", "e", "s"];
        for (i = 0; i < 4; i++) {
            bubble = document.getElementById(seats[i] + "bubble")
            if (emotes[i] != "") {
                setEmoteContent(emotes[i], bubble);
                setEmoteVisibility("visible", bubble);
            }
            else {
                setEmoteVisibility("hidden", bubble);
            };
        };
    };
    setEmoteContent = function (content, bubble) {
        bubble.innerHTML = "";
        const icon = document.createElement('i');
        bubble.appendChild(icon);
        icon.style.fontSize = "2em";

        if (content == "Clubs") {
            icon.style.color = "black";
            icon.classList = "bi bi-suit-club-fill";
        }
        else if (content == "Diamonds") {
            icon.style.color = "red";
            icon.classList = "bi bi-suit-diamond-fill";
        }
        else if (content == "Hearts") {
            icon.style.color = "red";
            icon.classList = "bi bi-suit-heart-fill";
        }
        else if (content == "Spades") {
            icon.style.color = "black";
            icon.classList = "bi bi-suit-spade-fill";
        }
        else if (content == "No Trumps") {
            icon.style.color = "darkmagenta";
            icon.classList = "i-notrumps";
        }
        else if (content == "All Trumps") {
            icon.style.color = "darkmagenta";
            icon.classList = "i-alltrumps";
        }
        else {
            bubble.innerHTML = content;
        };
    };
    setEmoteVisibility = function (setting, bubble) {
        bubble.style.visibility = setting;
    };
    // -------------------- TableCards --------------------
    setTableCards = function (cards) {
        var path = document.URL.substring(0, document.URL.indexOf("Replay"));
        for (i = 0; i < 4; i++) {
            document.getElementById("tablecard" + i).src = path + "Images/Cards/" + cards[i] + ".png";
        };
    };
    animateTrickWinner = function (winner, speed) {
        winnerCard = document.getElementById("tablecard" + winner);
        document.getElementById("tablecardslot" + winner).style.zIndex = 3;
        winnerCard.style.animationDuration = 0.9 * speed / 1000 + "s";
        winnerCard.classList.add("winning-card-pulse");
        setTimeout(function () {
            winnerCard.classList.remove("winning-card-pulse");
            document.getElementById("tablecardslot" + winner).style.zIndex = "auto";
            setTableCards(["c0-00", "c0-00", "c0-00", "c0-00"]);
        }, 0.9 * speed)
    };
    // -------------------- Hand --------------------
    setHands = function (hand) {

        var path = document.URL.substring(0, document.URL.indexOf("Replay"));
        seat = ["w", "n", "e", "s"];

        for (i = 0; i < 4; i++) {
            for (j = 0; j < 8; j++) {
                if (hand[i][j] == "c0-00") {
                    document.getElementById(seat[i] + "card" + j).hidden = true;
                }
                else {
                    document.getElementById(seat[i] + "card" + j).src = path + "Images/Cards/" + hand[i][j] + ".png";
                    document.getElementById(seat[i] + "card" + j).hidden = false;
                };
            };
        };
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
    // -------------------- Show Game Winner --------------------
    room.client.showGameWinner = function (winner) {
        var marker = document.getElementById(String(winner).concat("winnermarker"));
        marker.hidden = !marker.hidden;
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

    // -------------------- Players --------------------

    setSeatColour = function (seat, colour) {
        document.getElementById(seat.charAt(0).toLowerCase().concat("labelbadge")).style.backgroundColor = colour;
    };

    room.client.setPlayers = function (usernames) {
        for (i = 0; i < 4; i++) {
            document.getElementById("usernamelabel" + i).innerHTML = usernames[i];
            document.getElementById("usernamelabel" + i).style.backgroundColor = "#0d6efd";
        }
    };

    // -------------------- Set State --------------------

    room.client.setState = function (state, speed = 500) {
        //alert(state);
        state = JSON.parse(state);
        setScore(state.Scores);
        setDealer(state.Dealer);
        setRoundSuit(state.RoundSuit);
        setCaller(state.Caller);
        setTurn(state.Turn);
        setEmote(state.Emotes);
        setTableCards(state.TableCards);
        setHands(state.Hand);
        if (state.ShowTrickWinner) animateTrickWinner(state.Turn, speed);
    };

    // -------------------- Find Replays --------------------

    room.client.appendReplayList = function (list) {
        list = JSON.parse(list);

        var table = document.getElementById("replays-table");
        if (list.length == 0) {
            document.getElementById("no-games").innerHTML = "No historic games found. Go play!"
        };
        for (i = 0; i < list.length; i++) {
            var row = table.insertRow(table.rows.length);
            for (j = 0; j < 5; j++) {
                row.insertCell(table.rows[table.rows.length-1].cells.length);
                row.cells[j].innerHTML = list[i][j];
            };
            var cell = row.insertCell(table.rows[table.rows.length - 1].cells.length);
            var btn = document.createElement("button");
            btn.classList = "bi bi-eyeglasses btn btn-sm btn-primary py-0";
            const s = list[i][5];
            btn.onclick = function () {
                room.server.loadGame(s);
                $('#my-games').offcanvas('hide');
            };
            cell.appendChild(btn);
        };
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

    document.getElementById("speed-slider").oninput = function () {
        speed = (11 - this.value) * 100 + 200;
        document.getElementById("speed-value").innerHTML = "Speed: " + this.value;
        room.server.setReplaySpeed(speed);
    };

    $("#replay-speed-btn").on("click", function () {
        var delay = 600;

        if (!isNaN(parseInt(document.getElementById("speed-value").value))) delay = parseInt(document.getElementById("speed-value").value);
        if (delay > 5000 || delay < 0) delay = 600;

        document.getElementById("speed-value").value = delay;
        room.server.setReplaySpeed(delay);
    });

    // -------------------- Pause --------------------

    pauseReplay = function () {
        room.client.pauseReplay();
    };

    replayFwd = function () {
        room.server.replayFwd();
    };

    replayBack = function () {
        //pauseBtn.disabled = false;
        room.server.replayBack();
    };

    room.client.pauseReplay = function () {

        pause = "bi bi-pause-circle-fill";
        play = "bi bi-play-circle-fill";
        paused = false;
        pauseBtn = document.getElementById("pause-icon");
        if (pauseBtn.classList.contains("bi-play-circle-fill")) paused = true;

        if (!paused) {
            pauseBtn.classList = play;
            room.server.pauseReplay(true);
        }
        else if (paused) {
            pauseBtn.classList = pause;
            room.server.pauseReplay(false);
        };
    };

    room.client.setPausedTrue = function () {
        document.getElementById("pause-icon").classList = "bi bi-play-circle-fill";
    };

    room.client.enablePauseBtn = function (setting) {
        document.getElementById("pause-replay").disabled = !setting;
    };

    room.client.enableFwdBtn = function (setting) {
        document.getElementById("replay-fwd").disabled = !setting;
    };

    room.client.enableBackBtn = function (setting) {
        document.getElementById("replay-back").disabled = !setting;
    };

})();