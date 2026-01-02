"use strict";

var room = new signalR.HubConnectionBuilder().withUrl("/belotroom/" + document.getElementById("roomId").innerHTML).build();

var declarations;

room.start();

room.onclose(() => {
    alert("Disconnected. Try refresh the page to reconnect.");
});

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
    let card = GetCardFromResource(this.src);
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
        room.invoke("HubThrowCards");
    };
});

room.on("throwCards", function (player, hand) {
    const pos = ["w", "n", "e", "s"];
    document.getElementById("throw-modal-title").innerHTML = player.concat(" throws the cards!");
    for (let i = 0; i < 8; i++) {
        for (let j = 0; j < 4; j++) {
            if (hand[j][i].played) {
                document.getElementById(pos[j].concat("throwcard").concat(i)).hidden = true;
            }
            else {
                document.getElementById(pos[j].concat("throwcard").concat(i)).src = GetResourceFromCard(hand[j][i]);
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

window.addEventListener('resize', rotateCards);

function rotateCards() {
    const container = document.getElementById("cardboard");
    const allCards = container.querySelectorAll(".belot-card2");
    const visibleCards = [...allCards].filter(c => !c.hidden);
    const visible = visibleCards.length;
    const max = 8;

    if (visible === 0) return;

    const cardWidth = visibleCards[0].offsetWidth;
    const width = Math.min(container.clientWidth, 600);
    //console.log("container: " + container.clientWidth);
    let radius = 400;

    // compute the full spread for 8 cards to fill the container width
    let maxArcLength = width - cardWidth;
    //console.log(maxArcLength);
    const spread = (maxArcLength / radius) * (180 / Math.PI);
    //console.log("spread: " + spread);
    container.style.setProperty("--spread", `${spread}deg`);
    container.style.setProperty("--radius", `${radius}px`);
    container.style.setProperty("--max", max);

    // center the visible cards within the 8 slots
    const offset = (max - visible) / 2;

    visibleCards.forEach((card, i) => {
        card.style.setProperty("--slot", i + offset);
    });
}

function unrotateCards() {
    const container = document.getElementById("cardboard");
    const allCards = container.querySelectorAll(".belot-card2");

    // flatten all transforms first so cards fan from center
    allCards.forEach(card => {
        card.style.transition = "none"; // disable animation for the reset
        card.style.setProperty("--slot", 3.5); // all in the middle
    });

    // force browser reflow so reset happens immediately
    void container.offsetWidth;

    allCards.forEach((card) => {
        card.style.transition = ""; // restore CSS transition
    });
}

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
    document.getElementById("cardboard").classList.remove("cardboard-pulse");
};

room.on("enableCards", function (validcards) {
    for (let i = 0; i < 8; i++) {
        if (validcards[i] == 1) {
            document.getElementById("card" + i).onclick = playCardRequest;
        }
        else {
            document.getElementById("card" + i).classList.add("belot-card2-invalid");
        }
    }
    document.getElementById("cardboard").classList.add("cardboard-pulse");
});

room.on("setTableCard", function (tableCardPosition, tableCard) {
    setTableCard(tableCardPosition, tableCard);
});

function setTableCard(tableCardPosition, tableCard) {
    document.getElementById("tablecard".concat(tableCardPosition)).src = GetResourceFromCard(tableCard);
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
    let seat = document.getElementById("bubble" + turn);
    seat.style.visibility = "hidden";

    let icon = seat.querySelector('.emote-icon');
    clearSuitIconClass(icon);
    clearSuitIconColourClass(icon);
    icon.classList.remove("emote-icon-suit");
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
        document.getElementById(`tablecard${i}`).src = path;
    }
};

function resetBoard() {
    for (let i = 0; i < 8; i++) {
        hideCard("card" + i);
    };
    unrotateCards();
};

function resetSuitSelection() {
    const selectedSuit = document.getElementById("selectedsuit");
    selectedSuit.innerHTML = "";
    clearSuitIconClass(selectedSuit);
    clearSuitIconColourClass(selectedSuit);
    selectedSuit.classList.remove("suit-shadow");
    selectedSuit.classList.add("bi", "bi-suit-spade-fill");
    document.getElementById("selectedmultiplier").innerHTML = "";
    setCallerIndicator(4);
};

$("#newGameBtn").on("click", function () {
    room.invoke("HubGameController");
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

room.on("showTrickWinner", function (winner) {

    document.getElementById("tablecard".concat(winner)).classList.add("winning-card-pulse");
    document.getElementById("tableCardSlot".concat(winner)).style.zIndex = 3;
    setTimeout(function () {
        document.getElementById("tablecard".concat(winner)).classList.remove("winning-card-pulse");
        document.getElementById("tableCardSlot".concat(winner)).style.zIndex = "auto";
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
    for (let i = 0; i < cards.length; i++) {
        document.getElementById("card".concat(i)).src = GetResourceFromCard(cards[i])
        showCard("card" + i);
    };
});

// -------------------- Declarations --------------------

room.on("declareExtras", function (newDeclarations) {
    declarations = newDeclarations;
    if (newDeclarations.length > 0) {
        for (let i = 0; i < newDeclarations.length; i++) {
            const dv = document.createElement('div');

            const box = document.createElement('input');
            box.classList.add("form-check-input");
            box.type = ("checkbox");
            box.onclick = () => toggleDeclared(newDeclarations[i]);
            if (!newDeclarations[i].isDeclarable || (newDeclarations[i].type == 2 && !newDeclarations[i].isValid)) {
                box.checked = false;
                box.disabled = true;
                newDeclarations[i].declared = false;
            }
            else {
                box.checked = true;
                newDeclarations[i].declared = true;
            }

            const lbl = document.createElement('label');
            lbl.classList.add("form-check-label");
            lbl.style = "margin-left: 10px";
            lbl.innerHTML = getDeclarationText(newDeclarations[i]);

            dv.appendChild(box);
            dv.appendChild(lbl);
            document.getElementById("extras").appendChild(dv);
        };
        $('#extras-modal').modal('show');
    }
    else {
        room.invoke("HubExtrasDeclared", declarations);
    }
});

function getDeclarationText(declaration) {
    switch (declaration.type) {
        case 0: // belot
            return "Belot: " + getSuitNameFromNumber(declaration.suit);
        case 1: // carre
            return "Carre: " + getRankNameFromNumber(declaration.rank);
        default: // 2: run
            return getRunNameFromLength(declaration.length) + ": " + getSuitNameFromNumber(declaration.suit) + " " + getRankNameFromNumber(declaration.rank - declaration.length + 1) + "→" + getRankNameFromNumber(declaration.rank);
    }
}

function toggleDeclared(declaration) {
    declaration.declared = !declaration.declared;
}

function closeExtrasModal() {
    $('#extras-modal').modal('hide');
    document.getElementById("extras").innerHTML = "";
    room.invoke("HubExtrasDeclared", declarations);
};

room.on("setExtrasEmote", function (extras, turn) {
    let bubble = document.getElementById("bubble" + turn);
    let icon = bubble.querySelector('.emote-icon');
    clearSuitIconClass(icon);
    clearSuitIconColourClass(icon);
    icon.classList.remove("emote-icon-suit");
    icon.innerHTML = "";
    for (let i = 0; i < extras.length; i++) {
        icon.append(extras[i] + "\n");
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
    if (fiveUnderNine) {
        document.getElementById("callBtn9").disabled = false;
    }
    else {
        document.getElementById("callBtn9").disabled = true;
    }
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
    setEmoteSuitContent(bubble, suit);
});

function setSuitIconOff(suit) {
    const callBtn = document.getElementById("callBtn" + suit);
    callBtn.onclick = "";
    callBtn.classList.add("call-btn-inactive");
};

function setSuitIconOn(suit) {
    const callBtn = document.getElementById("callBtn" + suit);
    callBtn.onclick = function () { nominateSuit(this); };
    callBtn.classList.remove("call-btn-inactive");
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

    const wnescallindicator = document.getElementById("wnescallindicator");

    wnescallindicator.classList.remove("bi-arrow-left", "bi-arrow-up", "bi-arrow-right", "bi-arrow-down", "bi-arrows-move", "call-icon-black", "call-icon-inactive");
    wnescallindicator.classList.add(icons[turn]);
    setCallTooltip();
};

// -------------------- Seat Management --------------------

const dialog = document.querySelector('#seatActionsDialogue');
let outsideClickListener;
let dialogActivator;

function openDialogue(activator, pos) {

    if (dialog.open && activator == dialogActivator) {
        closeDialog();
        return;
    }
    else if (dialog.open) {
        document.removeEventListener('click', outsideClickListener);
    }

    outsideClickListener = function (event) {
        if (dialog.contains(event.target) || activator.contains(event.target)) { // the 'or' here prevents the dialog closing on the same click that opens it
            return;
        }
        closeDialog();

    }
    document.addEventListener('click', outsideClickListener);
    dialogActivator = activator;

    room.invoke("HubGetSeatActions", pos);
}

function closeDialog() {
    document.removeEventListener('click', outsideClickListener);
    dialogActivator = null;
    dialog.close();
}

room.on("setSeatActions", function (actions, pos) {

    if (!actions.canOccupy && !actions.canAssignBot && !actions.canVacate) {
        closeDialog();
        return;
    }

    const occupySeat = document.getElementById("occupySeat");
    occupySeat.onclick = () => requestSeatBooking(pos);
    occupySeat.hidden = !actions.canOccupy;
    const assignBot = document.getElementById("assignBot");
    assignBot.onclick = () => requestSeatBooking(pos + 4);
    assignBot.hidden = !actions.canAssignBot;
    const vacate = document.getElementById("vacate");
    vacate.onclick = () => requestSeatBooking(8);
    vacate.hidden = !actions.canVacate;

    // set dialogue arrow direction
    // account for rotated seats: dialog arrow points to where clicked, but pos remains the global position irrespective of rotation
    let r = parseInt(getComputedStyle(document.getElementById("card-table")).getPropertyValue('--seat-rotation'));
    r = (pos + Math.round(r / 90) + 4) % 4;
    if (r < 0) r += 4;

    const arrow = document.getElementById("seatActionsDialogueArrow");
    arrow.className = "seat-actions-arrow"; // reset previous direction
    switch (r) {
        case 0: arrow.classList.add("west"); break;
        case 1: arrow.classList.add("north"); break;
        case 2: arrow.classList.add("east"); break;
        case 3: arrow.classList.add("south"); break;
    }

    document.getElementById("seatActionsDialogue").show();
});

function requestSeatBooking(seat) {
    closeDialog();
    room.invoke("HubBookSeat", seat);
};

room.on("disableRadios", function () {
    // disable team selection
    for (let i = 0; i < 4; i++) {
        document.getElementById("tableCardSlot".concat(i)).disabled = true;
        document.getElementById("fingerprint".concat(i)).hidden = true;
    }
});

room.on("enableRadios", function () {
    // enable team selection
    for (let i = 0; i < 4; i++) {
        document.getElementById("tableCardSlot".concat(i)).disabled = false;
        document.getElementById("fingerprint".concat(i)).hidden = false;
    }
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
    /*document.getElementById("newGameBtn").classList.add("deal-pulse");*/
    document.getElementById("newGameBtn").classList.add("btn-prompt");
    document.getElementById("newGameBtn").disabled = false;
});

room.on("disableNewGame", function () {
    document.getElementById("newGameBtn").classList.remove("btn-prompt");
    document.getElementById("newGameBtn").disabled = true;
});

function setSeatColour(position, colour) {
    document.getElementById("usernamelabel".concat(position)).style.backgroundColor = colour;
};

room.on("seatBooked", function (position, username, isSelf) {
    const tableCardSlot = document.getElementById(`tableCardSlot${position}`);
    tableCardSlot.classList.remove("player-self", "player-other");
    const colorClass = isSelf ? "player-self" : "player-other";
    tableCardSlot.classList.add(colorClass);
    const usernameLabel = document.getElementById(`usernamelabel${position}`);
    usernameLabel.innerHTML = username;
});

room.on("seatUnbooked", function (position) {
    const tableCardSlot = document.getElementById(`tableCardSlot${position}`);
    tableCardSlot.classList.remove("player-self", "player-other");
    let defaultSeatName = getSeatNameByNumber(position);
    const usernameLabel = document.getElementById(`usernamelabel${position}`);
    usernameLabel.innerHTML = defaultSeatName;
});

room.on("seatAlreadyBooked", function (occupier) {
    document.getElementById("seat-modal-body").innerHTML = "This seat is already occupied by ".concat(occupier).concat(".");
    $('#seat-modal').modal('show');
});

room.on("setBotBadge", function (pos, isBot) {
    document.getElementById("BotBadge".concat(pos)).hidden = !isBot;
});

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

    const cardTable = document.getElementById("card-table");

    let r = parseInt(getComputedStyle(cardTable).getPropertyValue('--seat-rotation'));

    cardTable.style.setProperty('--seat-rotation', `${r + 90 * direction}`);

    let tableCardSlotPos = ["inWest", "inNorth", "inEast", "inSouth"];

    for (let i = 0; i < 4; i++) {
        let card = document.getElementById("tableCardSlot" + i);
        for (let j = 0; j < 4; j++) {
            if (card.classList.contains(tableCardSlotPos[j])) {
                card.classList.remove(tableCardSlotPos[j]);
                var d = j + direction;
                d = (d + 4) % 4; // -1 -> 3 and 4 -> 0
                card.classList.add(tableCardSlotPos[d]);

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
    room.invoke("HubAnnounce", document.getElementById("messageToSend").value);
    document.getElementById("messageToSend").value = "";
});

$("#chatLogBtn").on("click", function () {
    document.getElementById("chatLogBadge").hidden = true;
});

$("#chatbtn").on("click", function () {
    document.getElementById("chatLogBadge").hidden = true;
});

//-------------