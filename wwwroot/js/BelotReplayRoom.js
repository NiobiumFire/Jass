"use strict";

let currentState = 0;
let maxState = 0;
let speed = 800;
let autoPlay = false;
let replay = null;

const prevBtn = document.getElementById("replay-prev");
const backBtn = document.getElementById("replay-back");
const playBtn = document.getElementById("pause-replay");
const fwdBtn = document.getElementById("replay-fwd");
const nextBtn = document.getElementById("replay-next");

let state = {};

document.getElementById("speed-slider").oninput = function () {
    speed = (11 - this.value) * 100 + 200;
    document.getElementById("speed-value").innerHTML = "Speed: " + this.value;
};

function getReplay(replayId = "") {
    if (replayId == "") {
        replayId = document.getElementById("gameGUID").value;
    }
    $.ajax({
        url: "/Replay/GetReplay",
        type: "POST",
        data: { "replayId": replayId },
        success: function (data) {
            if (data != null && data != undefined && data != "") {
                replay = data;
                for (let i = 0; i < 4; i++) {
                    document.getElementById("usernamelabel" + i).innerHTML = data.stateChanges[0].after.players[i];
                    document.getElementById("usernamelabel" + i).style.backgroundColor = "#0d6efd";
                }
                maxState = data.stateChanges.length - 1;
                currentState = 0;
                resetState();
                setState(data.stateChanges[currentState], true);
            }
            else {
                $("#replay-not-found-modal").modal("show");
            }
        }
    })
}

function getMyReplays() {
    $.ajax({
        url: "/Replay/GetMyReplays",
        type: "GET",
        success: function (data) {
            data = JSON.parse(data);
            if (data.length == 0) {
                document.getElementById("replay-table").hidden = true;
                document.getElementById("no-games").innerHTML = "No historic games found. Go play!"
                document.getElementById("no-games").hidden = false;
            }
            else {
                document.getElementById("no-games").hidden = true;
                let table = document.getElementById("replay-table-body");
                table.innerHTML = "";
                for (let i = 0; i < data.length; i++) {
                    let row2 = table.insertRow(table.rows.length);
                    let cell2 = row2.insertCell(table.rows[table.rows.length - 1].cells.length);
                    cell2.colSpan = 5;
                    cell2.innerHTML = data[i][4];
                    let cell3 = row2.insertCell(table.rows[table.rows.length - 1].cells.length);
                    cell3.rowSpan = 2;
                    let row = table.insertRow(table.rows.length);
                    for (let j = 0; j < 4; j++) {
                        row.insertCell(table.rows[table.rows.length - 1].cells.length);
                        row.cells[j].innerHTML = data[i][j];
                    };
                    let btn = document.createElement("button");
                    btn.classList = "bi bi-eyeglasses btn btn-primary py-0 px-1";
                    btn.style.fontSize = "1.1rem";
                    const s = data[i][5];
                    btn.onclick = function () {
                        //getReplay(s);
                        getReplay(s);
                        $('#my-games').offcanvas('hide');
                    };
                    cell3.appendChild(btn);
                }
                document.getElementById("replay-table").hidden = false;
            }
        }
    })
};

function play() {
    autoPlay = !autoPlay;
    setControls();
    if (autoPlay) {
        loopPlay();
    }
};

function loopPlay() {
    let timer = setInterval(function () {
        clearInterval(timer);
        if (autoPlay && currentState < maxState) {
            setState(replay.stateChanges[++currentState], true);
            loopPlay();
        }
    }, speed);
}

function prev() {
    do {
        back();
    }
    while (currentState > 0 && replay.stateChanges[currentState].before.dealer == replay.stateChanges[currentState].after.dealer);
}

function back() {
    // apply current frame's "before", then go back a frame. For setting controls, currentState must be decremented before setState executes
    const stateToProcess = currentState;
    currentState--;
    setState(replay.stateChanges[stateToProcess], false);
}

function fwd() {
    // progress to next frame, then apply that frame's "after"
    setState(replay.stateChanges[++currentState], true);
}

function next() {
    do {
        fwd();
    }
    while (currentState < maxState && replay.stateChanges[currentState].before.dealer == replay.stateChanges[currentState].after.dealer);
}

function resetState() {
    state = {
        "players": ["West", "North", "East", "South"],
        "scores": [0, 0],
        "dealer": 1,
        "roundCall": -1,
        "caller": 4,
        "emotes": [null, null, null, null],
        "tableCards": [{}, {}, {}, {}],
        "handCards": [[null, null, null, null, null, null, null, null], [null, null, null, null, null, null, null, null], [null, null, null, null, null, null, null, null], [null, null, null, null, null, null, null, null]]
    };
}

function setState(diff, after) {
    setControls();

    let newState;
    if (after) {
        newState = diff.after;
    }
    else {
        newState = diff.before;
    }

    if (newState.scores != null) {
        state.scores = newState.scores;
    }
    if (newState.dealer != null) {
        state.dealer = newState.dealer;
    }
    if (newState.roundCall != null) {
        if (newState.roundCall == -1) { // NoCall
            state.roundCall = 0;
        }
        else if (newState.roundCall > 0) { // not "Pass"
            state.roundCall = newState.roundCall;
        }
    }
    if (newState.caller != null) {
        state.caller = newState.caller;
    }
    if (newState.turn != null) {
        state.turn = newState.turn;
    }
    if (newState.emotes != null) {
        for (const emote of newState.emotes) {
            state.emotes[emote.player] = emote.emote;
        }
    }
    if (newState.tableCards != null) {
        for (const tableCard of newState.tableCards) {
            state.tableCards[tableCard.player] = tableCard.card;
        }
    }

    if (newState.handCards != null) {
        for (const handCard of newState.handCards) {
            state.handCards[handCard.player][handCard.index] = handCard.card;
        }
    }

    setScore(state.scores);
    setDealer(state.dealer);
    setRoundSuit(state.roundCall);
    setCaller(state.caller);
    setCallTooltip();
    setTurn(state.turn);

    for (let i = 0; i < 4; i++) {
        setEmote(state.emotes[i], i);
        setTableCard(state.tableCards[i], i);
        for (let j = 0; j < 8; j++) {
            setHandCard(state.handCards[i][j], i, j);
        }
    }

    showGameWinners(currentState == maxState);
}

function setControls() {
    if (currentState == 0) {
        prevBtn.disabled = true;
        backBtn.disabled = true;
        playBtn.disabled = false;
        fwdBtn.disabled = false;
        nextBtn.disabled = false;
    }
    else if (currentState == maxState) {
        autoPlay = false;
        prevBtn.disabled = false;
        backBtn.disabled = false;
        playBtn.disabled = true;
        fwdBtn.disabled = true;
        nextBtn.disabled = true;
    }
    else {
        prevBtn.disabled = false;
        backBtn.disabled = false;
        playBtn.disabled = false;
        fwdBtn.disabled = false;
        nextBtn.disabled = false;
    }
    if (autoPlay) {
        document.getElementById("pause-icon").classList = "bi bi-pause-circle-fill";
        prevBtn.disabled = true;
        backBtn.disabled = true;
        fwdBtn.disabled = true;
        nextBtn.disabled = true;
    }
    else {
        document.getElementById("pause-icon").classList = "bi bi-play-circle-fill";
    }
}

function setScore(scores) {
    document.getElementById("ns-score").innerHTML = scores[0];
    document.getElementById("ew-score").innerHTML = scores[1];
}

function setDealer(dealer) {
    for (let i = 0; i < 4; i++) {
        if (dealer == i) {
            document.getElementById("dealermarker" + i).hidden = false;
        }
        else {
            document.getElementById("dealermarker" + i).hidden = true;
        }
    };
}

function setCaller(caller) {
    const wnesCallIndicator = ["bi bi-arrow-left", "bi bi-arrow-up", "bi bi-arrow-right", "bi bi-arrow-down", "bi bi-arrows-move"];
    document.getElementById("wnescallindicator").classList = wnesCallIndicator[caller];
    if (caller == 4) {
        document.getElementById("wnescallindicator").style.color = "dimgrey";
    }
    else document.getElementById("wnescallindicator").style.color = "black";
}

function setTurn(turn) {
    const turnIndicator = ["bi bi-arrow-left-circle-fill", "bi bi-arrow-up-circle-fill", "bi bi-arrow-right-circle-fill", "bi bi-arrow-down-circle-fill", "bi bi-suit-spade-fill"];
    document.getElementById("turnIndicator").classList = turnIndicator[turn];
}

function setEmote(emote, i) {
    let bubble = document.getElementById("bubble" + i)
    if (emote == null || emote == "") {
        bubble.style.visibility = "hidden";
    }
    else if (["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"].some(c => emote.includes(c))) { // call
        setEmoteSuitContent(bubble, parseInt(emote));
        bubble.style.visibility = "visible";
    }
    else { // declaration
        bubble.querySelector('.emote-icon').innerHTML = emote;
        bubble.style.visibility = "visible";
    }
}

function setTableCard(card, i) {
    document.getElementById("tablecard" + i).src = GetResourceFromCard(card)
}

function setHandCard(card, i, j) {
    const pos = ["w", "n", "e", "s"];
    if (card == null || card.played) {
        document.getElementById(pos[i] + "card" + j).hidden = true;
    }
    else {
        document.getElementById(pos[i] + "card" + j).src = GetResourceFromCard(card)
        document.getElementById(pos[i] + "card" + j).hidden = false;
    };
}

function showGameWinners(gameEnded) {
    for (let i = 0; i < 4; i++) {
        document.getElementById(i + "winnermarker").hidden = true;
    }
    if (gameEnded && state.scores[0] > state.scores[1]) {
        document.getElementById("1winnermarker").hidden = false;
        document.getElementById("3winnermarker").hidden = false;
    }
    else if (gameEnded && state.scores[0] < state.scores[1]) {
        document.getElementById("0winnermarker").hidden = false;
        document.getElementById("2winnermarker").hidden = false;
    }
}