"use strict";

function setEmoteSuitContent(emote) {
    const text = ["Pass", "", "", "", "", "A", "J", "×2!", "×4!!", "⁹⁄₅"];
    const classes = ["", "bi bi-suit-club-fill", "bi bi-suit-diamond-fill", "bi bi-suit-heart-fill", "bi bi-suit-spade-fill", "allNoTrumps", "allNoTrumps", "allNoTrumps", "allNoTrumps", ""];
    const colours = ["black", "black", "red", "red", "black", "darkmagenta", "darkmagenta", "red", "red", "black"];
    const icon = document.createElement('i');

    icon.classList = classes[emote];
    icon.style.color = colours[emote];
    icon.style.fontStyle = "normal";
    icon.innerHTML = text[emote];

    if (emote > 0) {
        icon.style.fontSize = "2em";
    }

    return icon;
}
function setRoundSuit(suit) {
    const text = ["", "", "", "", "", "A", "J", "×2", "×4"];
    const classes = ["bi bi-suit-spade-fill", "bi bi-suit-club-fill", "bi bi-suit-diamond-fill", "bi bi-suit-heart-fill", "bi bi-suit-spade-fill", "allNoTrumps", "allNoTrumps"];
    const colours = ["dimgrey", "black", "red", "red", "black", "darkmagenta", "darkmagenta"];
    let selectedSuit = document.getElementById("selectedsuit");
    let selectedMultiplier = document.getElementById("selectedmultiplier");
    if (suit < 7) {
        selectedMultiplier.innerHTML = "";
        selectedSuit.style.color = colours[suit];
        selectedSuit.classList = classes[suit];
        selectedSuit.innerHTML = text[suit];

    }
    else if (suit < 9) {
        if (selectedSuit.style.color == "black") {
            selectedMultiplier.style.color = "red";
        }
        else {
            selectedMultiplier.style.color = "black";
        }
        selectedMultiplier.innerHTML = text[suit];
    }
}

function setCallTooltip() {
    const callers = ["bi-arrow-left", "bi-arrow-up", "bi-arrow-right", "bi-arrow-down", "bi-arrows-move"];
    const calls = ["bi bi-suit-club-fill", "bi bi-suit-diamond-fill", "bi bi-suit-heart-fill", "bi bi-suit-spade-fill"];
    const tooltip = ["Clubs", "Diamonds", "Hearts", "Spades", "No Trumps", "All Trumps"];

    let wnesClass = String(document.getElementById("wnescallindicator").classList).toLowerCase();
    let wnesPos = 4;
    for (let i = 0; i < 4; i++) {
        if (wnesClass.includes(callers[i])) {
            wnesPos = i;
            break;
        }
    }
    let caller;
    if (wnesPos < 4) {
        caller = document.getElementById("usernamelabel" + wnesPos).innerHTML;
    }
    else {
        document.getElementById("tooltiptext").innerHTML = "No call has been made";
        return;
    }

    let suitClass = String(document.getElementById("selectedsuit").classList);
    let suitNum = calls.indexOf(suitClass);
    let suit;
    if (suitNum > -1) {
        suit = tooltip[suitNum];
    }
    else if (document.getElementById("selectedsuit").innerHTML == "A") {
        suit = "No Trumps";
    }
    else {
        suit = "All Trumps";
    }

    if (document.getElementById("selectedmultiplier").innerHTML == "×2") {
        document.getElementById("tooltiptext").innerHTML = caller + " doubled in " + suit;
    }
    else if (document.getElementById("selectedmultiplier").innerHTML == "×4") {
        document.getElementById("tooltiptext").innerHTML = caller + " redoubled in " + suit;
    }
    else {
        document.getElementById("tooltiptext").innerHTML = caller + " called " + suit;
    }
}