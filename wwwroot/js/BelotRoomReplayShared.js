"use strict";

const NO_SUIT_CALL = "bi-ban";

function setEmoteSuitContent(elEmote, iCall) {
    const text = ["Pass", "", "", "", "", "A", "J", "×2!", "×4!!", "⁹⁄₅"];
    const classes = ["", "bi-suit-club-fill", "bi-suit-diamond-fill", "bi-suit-heart-fill", "bi-suit-spade-fill", "allNoTrumps", "allNoTrumps", "allNoTrumps", "allNoTrumps", ""];
    const colours = ["", "call-icon-black", "call-icon-red", "call-icon-red", "call-icon-black", "call-icon-purple", "call-icon-purple", "call-icon-red", "call-icon-red", "call-icon-black"];
    const icon = elEmote.querySelector('.emote-icon');

    resetSuitIcon(icon);
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

function setTurn(turn, turnActionType) {
    const turnIndicator = ["bi bi-arrow-left-circle-fill", "bi bi-arrow-up-circle-fill", "bi bi-arrow-right-circle-fill", "bi bi-arrow-down-circle-fill", "bi bi-arrows-move"];
    document.getElementById("turnIndicator").classList = turnIndicator[turn];
    if (turn < 4) {
        document.getElementById("turn-tooltip").innerHTML = `${document.getElementById("usernamelabel" + turn).innerHTML} to ${turnActionType}`;
    }
    else {
        document.getElementById("turn-tooltip").innerHTML = "Waiting for next game";
        return;
    }
}

function resetSuitIcon(icon) {
    clearSuitIconClass(icon);
    clearSuitIconColourClass(icon);
    icon.classList.remove("emote-icon-suit");
}

function clearSuitIconClass(el) {
    el.classList.remove("bi", NO_SUIT_CALL, "bi-suit-club-fill", "bi-suit-diamond-fill", "bi-suit-heart-fill", "bi-suit-spade-fill", "allNoTrumps", "allNoTrumps");
}

function clearSuitIconColourClass(el) {
    el.classList.remove("call-icon-red", "call-icon-black", "call-icon-purple", "call-icon-inactive");
}

function setTableCardSlotUserNameAndLabelColour(position, username, occupied, isSelf = false) {
    const tableCardSlot = document.getElementById(`tableCardSlot${position}`);
    tableCardSlot.classList.remove("player-self", "player-other");

    if (occupied) {
        const colorClass = isSelf ? "player-self" : "player-other";
        tableCardSlot.classList.add(colorClass);
    }

    const usernameLabel = document.getElementById(`usernamelabel${position}`);
    usernameLabel.innerHTML = username;
}

function setRoundSuit(suit) {
    const text = ["", "", "", "", "", "A", "J", "×2", "×4"];
    const suits = [NO_SUIT_CALL, "bi-suit-club-fill", "bi-suit-diamond-fill", "bi-suit-heart-fill", "bi-suit-spade-fill", "allNoTrumps", "allNoTrumps"];
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

function setCallerIndicator(turn) {
    const icons = ["bi-arrow-left", "bi-arrow-up", "bi-arrow-right", "bi-arrow-down", "bi-arrows-move"];

    const wnescallindicator = document.getElementById("wnescallindicator");

    wnescallindicator.classList.remove("bi-arrow-left", "bi-arrow-up", "bi-arrow-right", "bi-arrow-down", "bi-arrows-move", "call-icon-black", "call-icon-inactive");
    wnescallindicator.classList.add(icons[turn]);
    setCallTooltip();
};

function setCallTooltip() {
    const callers = ["bi-arrow-left", "bi-arrow-up", "bi-arrow-right", "bi-arrow-down", "bi-arrows-move"];

    const classList = document.getElementById("wnescallindicator").classList;
    const wnesPos = callers.findIndex(c => classList.contains(c));

    if (wnesPos === -1) {
        console.error("[setCallTooltip] unrecognised caller class");
        return;
    }
    else if (wnesPos === 4) {
        document.getElementById("callTooltip").innerHTML = "No call has been made";
        return;
    }

    const calls = ["bi-suit-club-fill", "bi-suit-diamond-fill", "bi-suit-heart-fill", "bi-suit-spade-fill"];
    const tooltip = ["Clubs", "Diamonds", "Hearts", "Spades", "No Trumps", "All Trumps"];
    const caller = document.getElementById(`usernamelabel${wnesPos}`).innerHTML;

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
        document.getElementById("callTooltip").innerHTML = caller + " doubled in " + suit;
    }
    else if (document.getElementById("selectedmultiplier").innerHTML == "×4") {
        document.getElementById("callTooltip").innerHTML = caller + " redoubled in " + suit;
    }
    else {
        document.getElementById("callTooltip").innerHTML = caller + " called " + suit;
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