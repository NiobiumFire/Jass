"use strict";

function setEmoteSuitContent(elEmote, iCall) {
    const text = ["Pass", "", "", "", "", "A", "J", "×2!", "×4!!", "⁹⁄₅"];
    const classes = ["", "bi-suit-club-fill", "bi-suit-diamond-fill", "bi-suit-heart-fill", "bi-suit-spade-fill", "allNoTrumps", "allNoTrumps", "allNoTrumps", "allNoTrumps", ""];
    const colours = ["", "call-icon-black", "call-icon-red", "call-icon-red", "call-icon-black", "call-icon-purple", "call-icon-purple", "call-icon-red", "call-icon-red", "call-icon-black"];
    const icon = elEmote.querySelector('.emote-icon');

    clearSuitIconClass(icon);
    clearSuitIconColourClass(icon);
    icon.classList.remove("emote-icon-suit");
    if (iCall > 0) {
        if (iCall < 5) {
            icon.classList.add("bi");
        }
        icon.classList.add(classes[iCall]);
        icon.classList.add(colours[iCall]);
        icon.classList.add("emote-icon-suit");
    }
    
    icon.innerHTML = text[iCall];
}

function clearSuitIconClass(el) {
    el.classList.remove("bi", "bi-suit-spade-fill", "bi-suit-club-fill", "bi-suit-diamond-fill", "bi-suit-heart-fill", "bi-suit-spade-fill", "allNoTrumps", "allNoTrumps");
}

function clearSuitIconColourClass(el) {
    el.classList.remove("call-icon-red", "call-icon-black", "call-icon-purple", "call-icon-inactive");
}

function setRoundSuit(suit) {
    const text = ["", "", "", "", "", "A", "J", "×2", "×4"];
    const suits = ["bi-suit-spade-fill", "bi-suit-club-fill", "bi-suit-diamond-fill", "bi-suit-heart-fill", "bi-suit-spade-fill", "allNoTrumps", "allNoTrumps"];
    const colours = ["", "call-icon-black", "call-icon-red", "call-icon-red", "call-icon-black", "call-icon-purple", "call-icon-purple"];
    let selectedSuit = document.getElementById("selectedsuit");
    let selectedMultiplier = document.getElementById("selectedmultiplier");

    if (suit == 0) { // set selected suit to nothing/unset
        selectedSuit.classList.remove("suit-shadow");
    }
    // "Pass" is checked on hub
    if (suit < 7) { // C,D,H,S,A,J
        selectedMultiplier.innerHTML = "";
        clearSuitIconClass(selectedSuit);
        clearSuitIconColourClass(selectedSuit);
        if (suit > 0) { // 0 is reset to unset
            selectedSuit.classList.add(colours[suit]);
            selectedSuit.classList.add("suit-shadow");
        }
        if (suit < 5) {
            selectedSuit.classList.add("bi");
        }
        selectedSuit.innerHTML = text[suit];
        selectedSuit.classList.add(suits[suit]);
    }
    else if (suit < 9) {
        selectedMultiplier.innerHTML = text[suit];
    }
}

function setCallTooltip() {
    const callers = ["bi-arrow-left", "bi-arrow-up", "bi-arrow-right", "bi-arrow-down", "bi-arrows-move"];
    const calls = ["bi-suit-club-fill", "bi-suit-diamond-fill", "bi-suit-heart-fill", "bi-suit-spade-fill"];
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

    const selectedSuit = document.getElementById("selectedsuit");

    const classArray = Array.from(selectedSuit.classList);
    const suitNum = calls.findIndex(c => classArray.includes(c));
    let suit;
    if (suitNum > -1) {
        suit = tooltip[suitNum];
    }
    else if (selectedSuit.innerHTML == "A") {
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

function GetResourceFromCard(card) {
    //let path = document.URL.substring(0, document.URL.indexOf("Room"));

    if (card == null || card.suit == null || card.rank == null) {
        return "/images/Cards/c0-00.png";
    }

    let rank = card.rank + 6;
    if (rank < 10) {
        rank = "0" + rank;
    }
    let resource = "c" + card.suit + "-" + rank + ".png";

    return "/images/Cards/" + resource;
};

function GetCardFromResource(resource) {
    let cardText = resource.substr(resource.length - 9, 5);

    let suit = parseInt(cardText.substr(1, 1));
    let rank = parseInt((cardText.substr(3, 2) - 6));

    let card = {};

    card.suit = suit;
    card.rank = rank;

    return card
};

function getRunNameFromLength(length) {
    switch (length) {
        case 3:
            return "Tierce";
        case 4:
            return "Quarte";
        case 5:
            return "Quint";
    }
}

function getSuitNameFromNumber(suit) {
    switch (suit) {
        case 1:
            return "♣";
        case 2:
            return "♦";
        case 3:
            return "♥";
        case 4:
            return "♠";
    }
}

function getRankNameFromNumber(rank) {
    switch (rank) {
        case 0:
            return "7";
        case 1:
            return "8";
        case 2:
            return "9";
        case 3:
            return "10";
        case 4:
            return "J";
        case 5:
            return "Q";
        case 6:
            return "K";
        case 7:
            return "A";
    }
}