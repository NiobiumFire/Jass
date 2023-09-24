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
            if (data != "") {
                replay = JSON.parse(data);
                for (let i = 0; i < 4; i++) {
                    document.getElementById("usernamelabel" + i).innerHTML = replay.Players[i];
                    document.getElementById("usernamelabel" + i).style.backgroundColor = "#0d6efd";
                }
                maxState = replay.States.length - 1;
                currentState = 0;
                setState(replay.States[currentState]);
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
            setState(replay.States[++currentState]);
            loopPlay();
        }
    }, speed);
}

function prev() {
    if (currentState > 1) {
        do {
            currentState--;
        }
        while (currentState > 0 && replay.States[currentState - 1].Dealer == replay.States[currentState].Dealer);
    }
    if (currentState == 1) {
        currentState--;
    }
    setState(replay.States[currentState]);
}
function back() {
    if (replay.States[currentState - 1].ShowTrickWinner) currentState--;
    setState(replay.States[--currentState]);
}
function fwd() {
    if (replay.States[currentState + 1].ShowTrickWinner) currentState++;
    setState(replay.States[++currentState]);
}
function next() {
    if (currentState < maxState - 1) {
        do {
            currentState++;
        }
        while (currentState < maxState && replay.States[currentState + 1].Dealer == replay.States[currentState].Dealer);
    }
    if (currentState < maxState) {
        currentState++;
    }
    setState(replay.States[currentState]);
}

function setState(state) {
    setControls();
    setScore(state.Scores);
    setDealer(state.Dealer);
    setRoundSuit(state.RoundSuit);
    setCaller(state.Caller);
    setCallTooltip();
    setTurn(state.Turn);
    for (let i = 0; i < 4; i++) {
        setEmote(state.Emotes[i], i);
        setTableCard(state.TableCards[i], i);
        for (let j = 0; j < 8; j++) {
            setHand(state.Hand[i][j], i, j);
        }
    }
    if (state.ShowTrickWinner) animateTrickWinner(state.Turn);
    showGameWinners(currentState == maxState, state.Scores[0] > state.Scores[1]);
};
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
};
function setDealer(dealer) {
    for (let i = 0; i < 4; i++) {
        if (dealer == i) {
            document.getElementById("dealermarker" + i).hidden = false;
        }
        else {
            document.getElementById("dealermarker" + i).hidden = true;
        }
    };
};
function setCaller(caller) {
    const wnesCallIndicator = ["bi bi-arrow-left", "bi bi-arrow-up", "bi bi-arrow-right", "bi bi-arrow-down", "bi bi-arrows-move"];
    document.getElementById("wnescallindicator").classList = wnesCallIndicator[caller];
    if (caller == 4) document.getElementById("wnescallindicator").style.color = "dimgrey";
    else document.getElementById("wnescallindicator").style.color = "black";
};
function setTurn(turn) {
    const turnIndicator = ["bi bi-arrow-left-circle-fill", "bi bi-arrow-up-circle-fill", "bi bi-arrow-right-circle-fill", "bi bi-arrow-down-circle-fill", "bi bi-suit-spade-fill"];
    document.getElementById("turnIndicator").classList = turnIndicator[turn];
};
function setEmote(emote, i) {
    let bubble = document.getElementById("bubble" + i)
    bubble.innerHTML = "";
    if (["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"].some(c => emote.includes(c))) {
        bubble.appendChild(setEmoteSuitContent(parseInt(emote)));
        bubble.style.visibility = "visible";
    }
    else if (emote != "") {
        bubble.innerHTML = emote;
        bubble.style.visibility = "visible";
    }
    else {
        bubble.style.visibility = "hidden";
    };
};
function setTableCard(card, i) {
    document.getElementById("tablecard" + i).src = "/images/Cards/" + card + ".png";
};
function setHand(hand, i, j) {
    const pos = ["w", "n", "e", "s"];
    if (hand == "c0-00") {
        document.getElementById(pos[i] + "card" + j).hidden = true;
    }
    else {
        document.getElementById(pos[i] + "card" + j).src = "/images/Cards/" + hand + ".png";
        document.getElementById(pos[i] + "card" + j).hidden = false;
    };
};
function animateTrickWinner(winner) {
    let winnerCard = document.getElementById("tablecard" + winner);
    document.getElementById("tablecardslot" + winner).style.zIndex = 3;
    winnerCard.style.animationDuration = 0.9 * speed / 1000 + "s";
    winnerCard.classList.add("winning-card-pulse");
    setTimeout(function () {
        winnerCard.classList.remove("winning-card-pulse");
        document.getElementById("tablecardslot" + winner).style.zIndex = "auto";
        for (let i = 0; i < 4; i++) {
            setTableCard("c0-00", i);
        }
    }, 0.9 * speed)
};
function showGameWinners(gameEnded, nsWon) {
    for (let i = 0; i < 4; i++) {
        document.getElementById(i + "winnermarker").hidden = true;
    }
    if (gameEnded && nsWon) {
        document.getElementById("1winnermarker").hidden = false;
        document.getElementById("3winnermarker").hidden = false;
    }
    else if (gameEnded && !nsWon) {
        document.getElementById("0winnermarker").hidden = false;
        document.getElementById("2winnermarker").hidden = false;
    }
};