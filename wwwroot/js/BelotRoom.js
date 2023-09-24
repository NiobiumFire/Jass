"use strict";

var room = new signalR.HubConnectionBuilder().withUrl("/belotroom/" + document.getElementById("roomId").innerHTML).build();

room.start();

room.on("connectedUsers", function (players, spectators) {
    document.getElementById("userList").innerHTML = "";
    for (let i = 0; i < players.length; i++) {
        document.getElementById("userList").innerHTML += "&#x25AA " + players[i] + "<br />";
    };
    for (let i = 0; i < spectators.length; i++) {
        document.getElementById("userList").innerHTML += "&#x25AB " + spectators[i] + "<br />";
    };
});

// -------------------- Play Card --------------------

function playCardRequest() {
    disableCards();
    hideThrowBtn();
    hideCard(this.id);
    rotateCards();
    let card = this.src.substr(this.src.length - 9, 5);
    room.invoke("HubPlayCard", card);
};

room.on("playFinalCard", function () {
    disableCards();
    for (let i = 0; i < 8; i++) {
        hideCard("card" + i);
    }
});

function hideThrowBtn() {
    document.getElementById("throw-cards-btn").hidden = true;
    document.getElementById("throw-cards-btn").onclick = "";
};

room.on("showThrowBtn", function () {
    document.getElementById("throw-cards-btn").hidden = false;
    document.getElementById("throw-cards-btn").onclick = function () {
        hideThrowBtn();
        room.invoke("ThrowCards");
    };
});

room.on("throwCards", function (player, hand) {
    hand = JSON.parse(hand);
    const pos = ["w", "n", "e", "s"];
    document.getElementById("throw-modal-title").innerHTML = player.concat(" throws the cards!");
    for (let i = 0; i < 8; i++) {
        for (let j = 0; j < 4; j++) {
            if (hand[j][i] == "c0-00") {
                document.getElementById(pos[j].concat("throwcard").concat(i)).hidden = true;
            }
            else {
                let path = document.URL.substring(0, document.URL.indexOf("Room"));
                document.getElementById(pos[j].concat("throwcard").concat(i)).src = path.concat("Images/Cards/", hand[j][i], ".png");
                document.getElementById(pos[j].concat("throwcard").concat(i)).hidden = false;
            };
        };
    };
    $('#throw-modal').modal('show');
});

function closeThrowModal() {
    $('#throw-modal').modal('hide');
};

room.on("closeThrowModal", function () {
    closeThrowModal();
});

room.on("rotateCards", function () {
    rotateCards();
});

function rotateCards() {
    let offsets = document.getElementById('cardboard').getBoundingClientRect();
    let top = offsets.top;
    let height = document.getElementById("cardboard").clientHeight;
    let left = offsets.left;
    let width = Math.min(document.getElementById("cardboard").clientWidth, 600);
    let Cx = left;
    let Cy = top + height / 2.0;
    let Ax = left + width / 2.0;
    let Ay = top;
    let Bx = left + width;
    let By = top + height / 2.0;
    let b = Math.sqrt(Math.pow((Ax - Cx), 2.0) + Math.pow((Ay - Cy), 2.0));

    let c = Math.sqrt(Math.pow((Ax - Bx), 2.0) + Math.pow((Ay - By), 2.0));//= b
    let a = (width);

    let h = height / 2.0;

    let r = (b * c / (2 * h));

    let theta = Math.round(180.0 - (2.0 * Math.atan((2.0 * (r - h)) / a) * 180.0 / Math.PI));

    // subtract half of the card width from the arc length, convert to angle and remove this from theta, for the start and end card (x2)
    let arc = document.getElementById("tablecard0").clientWidth;

    let phi = (arc / r) * 180 / Math.PI;
    theta = theta - phi;

    let rotation = theta / 7;

    let children = document.getElementById("cardboard").children;
    let visibleChildren = 8;
    for (let i = 0; i < children.length; i++) {
        if (children[i].hidden == true) visibleChildren--;
    }
    let count = 0;
    for (let i = 0; i < children.length; i++) {
        if (children[i].hidden == false) {
            let child = children[i];
            child.style.transformOrigin = "center " + Math.round(r) + "px";
            child.style.transform = "rotate(" + (-3.5 * rotation + 0.5 * rotation * (8 - visibleChildren) + rotation * count) + "deg)";
            count++;
        }
    }
};

room.on("hideCard", function (cardId) {
    hideCard(cardId);
});

function hideCard(cardId) {
    document.getElementById(cardId).hidden = true;
    document.getElementById(cardId).style.transform = "rotate(0deg)";
};

room.on("showCard", function (cardId) {
    showCard(cardId);
});

function showCard(cardId) {
    document.getElementById(cardId).hidden = false;
};

room.on("disableCards", function () {
    disableCards();
});

function disableCards() {
    for (let i = 0; i < 8; i++) {
        document.getElementById("card" + i).onclick = "";
        //document.getElementById("card" + i).classList.remove("belot-card2-valid");
        document.getElementById("card" + i).classList.remove("belot-card2-invalid");
    }
    document.getElementById("cardboard").classList.remove("card-board-pulse");
};

room.on("enableCards", function (validcards) {
    for (let i = 0; i < 8; i++) { // change to for each element in received integer array
        if (validcards[i] == 1) {
            //alert("");
            document.getElementById("card" + i).onclick = playCardRequest;
            //document.getElementById("card" + i).classList.add("belot-card2-valid");
        }
        else {
            document.getElementById("card" + i).classList.add("belot-card2-invalid");
        }
    }
    document.getElementById("cardboard").classList.add("card-board-pulse");
});

room.on("setTableCard", function (tableCardPosition, tableCard) {
    setTableCard(tableCardPosition, tableCard);
});

function setTableCard(tableCardPosition, tableCard) {
    let path = document.URL.substring(0, document.URL.indexOf("Room"));
    document.getElementById("tablecard".concat(tableCardPosition)).src = path.concat("Images/Cards/", tableCard, ".png");
};

// -------------------- Turn Indicator --------------------

room.on("setTurnIndicator", function (turn) {
    const icons = ["bi-arrow-left-circle-fill", "bi-arrow-up-circle-fill", "bi-arrow-right-circle-fill", "bi-arrow-down-circle-fill", "bi-suit-spade-fill"];
    document.getElementById("turnIndicator").classList.remove("bi-arrow-left-circle-fill", "bi-arrow-up-circle-fill", "bi-arrow-right-circle-fill", "bi-arrow-down-circle-fill", "bi-suit-spade-fill");
    document.getElementById("turnIndicator").classList.add(icons[turn]);
});

// -------------------- Emote --------------------

room.on("showEmote", function (turn) {
    let seat = "bubble" + turn;
    document.getElementById(seat).style.visibility = "visible";
});

room.on("hideEmote", function (turn) {
    let seat = "bubble" + turn;
    document.getElementById(seat).style.visibility = "hidden";
});

// -------------------- Round Summary --------------------


room.on("showRoundSummary", function (trickPoints, declarationPoints, belotPoints, result, ew, ns) {

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
});

room.on("hideRoundSummary", function () {
    $('#summary-modal').modal('hide');
});

// -------------------- Reset --------------------

room.on("closeModalsAndButtons", function () {
    $('#extras-modal').modal('hide');
    $('#suit-modal').modal('hide');
    $('#summary-modal').modal('hide');
    document.getElementById("make-call-btn").hidden = true;
    document.getElementById("make-call-btn").onclick = "";
});

function resetTable() {
    let path = document.URL.substring(0, document.URL.indexOf("Room")).concat("Images/Cards/c0-00.png");
    for (let i = 0; i < 4; i++) {
        document.getElementById("tablecard".concat(i)).src = path;
    }
};

function resetBoard() {
    for (let i = 0; i < 8; i++) {
        hideCard("card" + i);
    };
};

function resetSuitSelection() {
    document.getElementById("selectedsuit").innerHTML = "";
    document.getElementById("selectedsuit").classList = "bi bi-suit-spade-fill";
    document.getElementById("selectedsuit").style.color = "dimgrey";
    document.getElementById("selectedmultiplier").innerHTML = "";
    setCallerIndicator(4);
};

$("#newGameBtn").on("click", function () {
    room.invoke("GameController");
});

room.on("newGame", function (gameId) {

    let table = document.getElementById("scoreTable");
    while (table.rows.length > 2) {
        table.deleteRow(1)
    };
    table.rows[1].cells[1].innerHTML = 0;
    table.rows[1].cells[2].innerHTML = 0;

    document.getElementById("ns-score").innerHTML = 0;
    document.getElementById("ew-score").innerHTML = 0;

    document.getElementById("0winnermarker").hidden = true;
    document.getElementById("1winnermarker").hidden = true;
    document.getElementById("2winnermarker").hidden = true;
    document.getElementById("3winnermarker").hidden = true;

    //room.client.setGameId(gameId);
});

//room.on("setRoomId", function (roomId) {
//    document.getElementById("roomId").innerHTML = "Room Id: " + roomId;
//});

//room.on("setGameId", function (gameId) {
//    setGameId(gameId);
//});

//setGameId = function (gameId) {
//    document.getElementById("gameId").innerHTML = "Game Id: " + gameId;
//};

room.on("showTrickWinner", function (winner) {

    document.getElementById("tablecard".concat(winner)).classList.add("winning-card-pulse");
    document.getElementById("tablecardslot".concat(winner)).style.zIndex = 3;
    setTimeout(function () {
        document.getElementById("tablecard".concat(winner)).classList.remove("winning-card-pulse", "z-2");
        document.getElementById("tablecardslot".concat(winner)).style.zIndex = "auto";
    }, 1000);
});

room.on("showGameWinner", function (winner) {
    let marker = document.getElementById(String(winner).concat("winnermarker"));
    marker.hidden = !marker.hidden;
});

room.on("updateScoreTotals", function (ewPoints, nsPoints) {
    document.getElementById("ns-score").innerHTML = nsPoints;
    document.getElementById("ew-score").innerHTML = ewPoints;
});

room.on("setScoreTitles", function (nsTitle, ewTitle) {
    document.getElementById("scoreSummaryNSTitle").innerHTML = nsTitle;
    document.getElementById("scoreSummaryEWTitle").innerHTML = ewTitle;
    document.getElementById("roundSummaryNSTitle").innerHTML = nsTitle;
    document.getElementById("roundSummaryEWTitle").innerHTML = ewTitle;
    document.getElementById("scoreHistoryNSTitle").innerHTML = nsTitle;
    document.getElementById("scoreHistoryEWTitle").innerHTML = ewTitle;
});

room.on("appendScoreHistory", function (ewPoints, nsPoints) {
    let table = document.getElementById("scoreTable");

    let row = table.insertRow(table.rows.length - 1);

    let cell1 = row.insertCell(0);
    let cell2 = row.insertCell(1);
    let cell3 = row.insertCell(2);

    if (table.rows.length == 3) {
        cell1.innerHTML = 1;
    }
    else {
        cell1.innerHTML = parseInt(table.rows[table.rows.length - 3].cells[0].innerHTML) + 1;
    };

    cell2.innerHTML = nsPoints;
    cell3.innerHTML = ewPoints;

    let totalNS = 0;
    let totalEW = 0;
    for (let i = 1; i < table.rows.length - 1; i++) {
        totalNS += parseInt(table.rows[i].cells[1].innerHTML);
        totalEW += parseInt(table.rows[i].cells[2].innerHTML);
    };
    table.rows[table.rows.length - 1].cells[1].innerHTML = totalNS;
    table.rows[table.rows.length - 1].cells[2].innerHTML = totalEW;
});

room.on("resetTable", function () {
    resetTable();
});

room.on("hideDeck", function (hidden) {
    hideDeck(hidden);
});

function hideDeck(hidden) {
    document.getElementById("deck").hidden = hidden;
};

room.on("disableDealBtn", function () {
    hideDeck(true);
    document.getElementById("deck-shimmer").hidden = true;
});

room.on("enableDealBtn", function () {
    document.getElementById("deck").onclick = beginDeal;
    hideDeck(false);
    document.getElementById("deck-shimmer").hidden = false;
});

room.on("newRound", function () {
    resetTable();
    resetBoard();
    disableCards();
    resetSuitSelection();
});

// -------------------- Deal Cards --------------------

function beginDeal() {
    document.getElementById("deck").onclick = "";
    hideDeck(true);
    document.getElementById("deck-shimmer").hidden = true;
    room.invoke("HubShuffle");
};

room.on("setDealerMarker", function (dealer) {
    for (let i = 0; i < 4; i++) {
        if (i == dealer) document.getElementById("dealermarker".concat(i)).hidden = false;
        else document.getElementById("dealermarker".concat(i)).hidden = true;
    };
});

room.on("deal", function (cards) {
    // show hand card images
    let card = JSON.parse(cards);
    let path = document.URL.substring(0, document.URL.indexOf("Room"));
    for (let i = 0; i < card.length; i++) {
        document.getElementById("card".concat(i)).src = path.concat("Images/Cards/", card[i], ".png");
        showCard("card" + i);
    };
});

// -------------------- Extra Points --------------------

room.on("declareExtras", function (extras) {
    extras = JSON.parse(extras);
    if (extras.length > 0) {
        for (let i = 0; i < extras.length; i++) {
            const dv = document.createElement('div');

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
            }
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
        let belot = false;
        let runs = [];
        let carres = [];
        room.invoke("HubExtrasDeclared", belot, runs, carres);
    }
});

function closeExtrasModal() {
    $('#extras-modal').modal('hide');

    let belot = false;
    let runs = [];
    let carres = [];

    let dv = document.getElementById('extras');
    let boxes = dv.children;
    for (let i = 0; i < boxes.length; i++) {
        let box = boxes[i].children;
        if (box[0].id.charAt(0) == "B") belot = box[0].checked;
        else if (box[0].id.charAt(0) == "C") carres.push(box[0].checked);
        else runs.push(box[0].checked);
    };

    document.getElementById("extras").innerHTML = "";
    room.invoke("HubExtrasDeclared", belot, runs, carres);
};

room.on("setExtrasEmote", function (extras, turn) {
    extras = JSON.parse(extras);
    let seat = "bubble" + turn;
    document.getElementById(seat).innerHTML = "";
    for (let i = 0; i < extras.length; i++) {
        document.getElementById(seat).append(extras[i] + "\n");
    };
});

// -------------------- Suit Selection --------------------

room.on("showSuitModal", function (validCalls, fiveUnderNine = false) {
    for (let i = 0; i < 8; i++) {
        if (validCalls[i] == 1) {
            setSuitIconOn(i + 1);
        }
        else {
            setSuitIconOff(i + 1);
        };
    };
    if (fiveUnderNine) document.getElementById("suitBtn9").disabled = false;
    else document.getElementById("suitBtn9").disabled = true;
    $('#lobby').offcanvas('hide');
    //window.scrollTo(0, 99999);
    $('#suit-modal').modal('show');
});

function minimiseSuitModal() {
    $('#suit-modal').modal('hide');
    document.getElementById("make-call-btn").hidden = false;
    document.getElementById("make-call-btn").onclick = function () {
        $('#suit-modal').modal('show');
        document.getElementById("make-call-btn").hidden = true;
        document.getElementById("make-call-btn").onclick = "";
    };
};

room.on("emoteSuit", function (suit, turn) {
    let bubble = document.getElementById("bubble" + turn);
    bubble.innerHTML = "";
    bubble.appendChild(setEmoteSuitContent(suit));
});

function setSuitIconOff(suit) {
    document.getElementById("suitBtn" + suit).onclick = "";
    document.getElementById("suit" + suit).style.color = "lightgrey";
};

function setSuitIconOn(suit) {
    if (suit == 1 || suit == 4) {
        document.getElementById("suit" + suit).style.color = "black";
    }
    else if (suit == 2 || suit == 3 || suit == 7 || suit == 8) {
        document.getElementById("suit" + suit).style.color = "red";
    }
    else if (suit == 5 || suit == 6) {
        document.getElementById("suit" + suit).style.color = "darkmagenta";
    };

    document.getElementById("suitBtn" + suit).onclick = function () { nominateSuit(this) };
};

function nominateSuit(el) {
    $('#suit-modal').modal('hide');
    room.invoke("HubNominateSuit", parseInt(el.id.charAt(el.id.length - 1)));
};

room.on("suitNominated", function (suit) {
    setRoundSuit(suit);
});

room.on("setCallerIndicator", function (turn) {
    setCallerIndicator(turn);
});

function setCallerIndicator(turn) {
    const icons = ["bi-arrow-left", "bi-arrow-up", "bi-arrow-right", "bi-arrow-down", "bi-arrows-move"];

    document.getElementById("wnescallindicator").classList.remove("bi-arrow-left", "bi-arrow-up", "bi-arrow-right", "bi-arrow-down", "bi-arrows-move");
    document.getElementById("wnescallindicator").classList.add(icons[turn]);

    if (turn < 4) {
        document.getElementById("wnescallindicator").style.color = "black";
    }
    else {
        document.getElementById("wnescallindicator").style.color = "dimgrey";
    }
    setCallTooltip();
};

// -------------------- Seat Management --------------------

function requestSeatBooking(seat) {
    room.invoke("BookSeat", seat);
};

room.on("disableRadios", function () {
    // disable team selection
    for (let i = 0; i < 4; i++) {
        document.getElementById("tablecardslot".concat(i)).disabled = true;
        document.getElementById("fingerprint".concat(i)).hidden = true;
    }
});

room.on("enableRadios", function () {
    // enable team selection
    for (let i = 0; i < 4; i++) {
        document.getElementById("tablecardslot".concat(i)).disabled = false;
        document.getElementById("fingerprint".concat(i)).hidden = false;
    }
});

room.on("enableSeatOptions", function (pos, setting) {
    if (document.getElementById("fingerprint" + pos).classList.contains("show"))
        $("#fingerprint" + pos).dropdown("toggle");
    document.getElementById("teamselector" + pos).hidden = !setting;
});

room.on("enableOccupySeat", function (pos, setting) {
    document.getElementById("occupy".concat(pos)).hidden = !setting;
});

room.on("enableAssignBotToSeat", function (pos, setting) {
    document.getElementById("assignbot".concat(pos)).hidden = !setting;
});

room.on("enableVacateSeat", function (pos, setting) {
    document.getElementById("vacate".concat(pos)).hidden = !setting;
});

// is this required? seat id in html may need renming to convene e.g. seat0
function getSeatNameByNumber(number) {
    let seat = "";
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

room.on("enableNewGame", function () {
    document.getElementById("newGameBtn").classList.add("deal-pulse");
    document.getElementById("newGameBtn").disabled = false;
});

room.on("disableNewGame", function () {
    document.getElementById("newGameBtn").classList.remove("deal-pulse");
    document.getElementById("newGameBtn").disabled = true;
});

room.on("setSeatColour", function (position, colour) {
    setSeatColour(position, colour);
});

function setSeatColour(position, colour) {
    document.getElementById("usernamelabel".concat(position)).style.backgroundColor = colour;
};

room.on("seatBooked", function (position, username, isSelf) {
    document.getElementById("usernamelabel".concat(position)).innerHTML = username;
    let colour = "#0d6efd";
    if (isSelf) colour = "darkmagenta";
    setSeatColour(position, colour);
});

room.on("seatUnbooked", function (position) {
    let seat = getSeatNameByNumber(position);
    document.getElementById("usernamelabel".concat(position)).innerHTML = seat;
    setSeatColour(position, "black");
});

room.on("seatAlreadyBooked", function (occupier) {
    document.getElementById("seat-modal-body").innerHTML = "This seat is already occupied by ".concat(occupier).concat(".");
    $('#seat-modal').modal('show');
});

room.on("setBotBadge", function (pos, isBot) {
    document.getElementById("BotBadge".concat(pos)).hidden = !isBot;
});

// required?
//moveWest = function () {
//    const w = document.getElementById("west");
//    w.style.top = 0;
//    w.style.left = 0;
//};

// -------------------- Seat Rotation --------------------

room.on("enableRotation", function (setting) {
    document.getElementById("rotateSeatsClockwiseBtn").disabled = !setting;
    document.getElementById("rotateSeatsAntiClockwiseBtn").disabled = !setting;
});

$("#rotateSeatsClockwiseBtn").on("click", function () {
    rotateSeats(1);
});

$("#rotateSeatsAntiClockwiseBtn").on("click", function () {
    rotateSeats(-1);
});

function rotateSeats(direction) {

    let r = getRotation(document.getElementById("wnescallindicator"));

    document.getElementById("wnescallindicator").style.transform = "rotate(" + (r + 90 * direction) + "deg)";
    document.getElementById("turnIndicator").style.transform = "rotate(" + (r + 90 * direction) + "deg)";

    let tableCardSlotPos = ["inWest", "inNorth", "inEast", "inSouth"];

    let tableCardTranslate = [["0px, 0px", "100%, -50%", "200%, 0px", "100%, 50%"],
    ["-100%, 50%", "0px, 0px", "100%, 50%", "0px, 100%"],
    ["-200%, 0px", "-100%, -50%", "0px, 0px", "-100%, 50%"],
    ["-100%, -50%", "0px, -100%", "100%, -50%", "0px, 0px"]];

    let markerTranslate = ["translate(-57%, 0px)", "translate(0px, -56%)", "translate(58%, 0px)", "translate(0px, 55%)"];

    let dropdowns = [" dropend", " dropdown-center", " dropstart", " dropup dropup-center"];

    let emotes = ["w", "n", "e", "s"];

    for (let i = 0; i < 4; i++) {
        let card = document.getElementById("tablecardslot" + i);
        let marker = document.getElementById("player-marker" + i);
        let dealer = document.getElementById("dealermarker" + i);
        let fingerprint = document.getElementById("fingerprint" + i);
        if (fingerprint.classList.contains("show")) $("#fingerprint" + i).dropdown("toggle");
        for (let j = 0; j < 4; j++) {
            if (card.classList.contains(tableCardSlotPos[j])) {
                card.classList.remove(tableCardSlotPos[j]);
                var d = j + direction;
                if (d == 4) d = 0;
                if (d == -1) d = 3;
                card.classList.add(tableCardSlotPos[d]);
                card.style.transform = "translate(" + tableCardTranslate[i][d] + ")";

                marker.style.transform = markerTranslate[d] + " rotate(" + (getRotation(marker) + 90 * direction) + "deg)";
                if (d == 1) marker.classList.remove("pnm-reverse");
                else marker.classList.add("pnm-reverse");

                dealer.classList.remove("dealer0", "dealer1", "dealer2", "dealer3");
                dealer.classList.add("dealer" + d);

                fingerprint.classList.remove("fingerprint0", "fingerprint1", "fingerprint2", "fingerprint3");
                fingerprint.classList.add("fingerprint" + d);
                fingerprint.parentNode.classList = "dropdown" + dropdowns[d];

                document.getElementById("bubble" + i).classList = "emote bubble" + d;

                document.getElementById("throwBoard" + i).classList = "throw throw" + d;
                break;
            };
        };
    };
};

function getRotation(el) {
    let r = el.style.transform;
    if (r == "") r = 0;
    else {
        let s = r.search("rotate") + "rotate(".length;
        let e = r.search("deg");
        r = r.substring(s, e);
    }
    return parseInt(r);
};

// -------------------- Chat Log --------------------

/*copyGameInvite = function () {*/
function copyGameInvite() {
    var inputEl = document.createElement("input");
    inputEl.type = "text";
    inputEl.value = document.URL;
    document.getElementById("lobby").appendChild(inputEl);
    inputEl.select();
    inputEl.setSelectionRange(0, inputEl.value.length);
    document.execCommand('copy');
    document.getElementById("lobby").removeChild(inputEl);
    document.getElementById("copyGameIdBtn").disabled = true;
    document.getElementById("copyGameIdBtn").value = "Copied!";
    setTimeout(function () {
        document.getElementById("copyGameIdBtn").value = "Copy Game Invite";
        document.getElementById("copyGameIdBtn").disabled = false;
    }, 600);
};

room.on("announce", function (message) {
    writeToPage(message);
});

room.on("showChatNotification", function () {
    let chatOpen = document.getElementById("chatLogAccordian").classList.contains("show");
    let lobbyOpen = document.getElementById("lobby").classList.contains("show");
    if (!lobbyOpen || (lobbyOpen && !chatOpen)) {
        document.getElementById("chatLogBadge").hidden = false
    }
});

function writeToPage(message) {
    if (document.getElementById("chatLog").value != "") {
        $("#chatLog").append("\n");
    };
    $("#chatLog").append(message);
    let textarea = document.getElementById('chatLog');
    textarea.scrollTop = textarea.scrollHeight;
};

$("#messageToSend").keyup(function (event) {
    if (event.keyCode === 13) {
        $("#sendmessage").click();
    }
});

$("#sendmessage").on("click", function () {
    room.invoke("Announce", document.getElementById("messageToSend").value);
    document.getElementById("messageToSend").value = "";
});

$("#chatLogBtn").on("click", function () {
    document.getElementById("chatLogBadge").hidden = true;
});

$("#chatbtn").on("click", function () {
    let chatOpen = document.getElementById("chatLogAccordian").classList.contains("show");
    let lobbyOpening = document.getElementById("lobby").classList.contains("showing");
    if (lobbyOpening && chatOpen) {
        document.getElementById("chatLogBadge").hidden = true;
    }
});