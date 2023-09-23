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