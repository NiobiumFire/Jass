"use strict";

function setEmoteSuitContent(iEmote) {
    const text = ["Pass", "", "", "", "", "A", "J", "×2!", "×4!!", "⁹⁄₅"];
    const classes = ["", "bi bi-suit-club-fill", "bi bi-suit-diamond-fill", "bi bi-suit-heart-fill", "bi bi-suit-spade-fill", "allNoTrumps", "allNoTrumps", "allNoTrumps", "allNoTrumps", ""];
    const colours = ["black", "black", "red", "red", "black", "darkmagenta", "darkmagenta", "red", "red", "black"];
    const icon = document.createElement('i');

    icon.classList = classes[iEmote];
    icon.style.color = colours[iEmote];
    icon.style.fontStyle = "normal";
    icon.innerHTML = text[iEmote];

    if (iEmote > 0) {
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
    // "Pass" is checked on hub
    if (suit < 7) { // C,D,H,S,A,J
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