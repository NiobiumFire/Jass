"use strict";

function populateLobby() {
    getNumRooms();
    $.ajax({
        url: "/Home/PopulateLobbyPartial",
        method: "GET",
        success: function (html) {
            document.getElementById("lobby-table").innerHTML = html;
        }
    });
}

function getNumRooms() {
    $.ajax({
        url: "/Home/GetNumRooms",
        method: "GET",
        success: function (data) {
            document.getElementById("gameCount").innerHTML = data;
        }
    });
};

function refreshLobby() {
    populateLobby();
    const button = document.getElementById("refreshBtn");

    button.classList.add('spinning');

    // Remove the class after animation ends (so it can spin again next time)
    button.addEventListener('animationend', () => {
        button.classList.remove('spinning');
    }, { once: true });
};