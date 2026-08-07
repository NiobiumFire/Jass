"use strict";

let refreshButton = null;
let lobbyTable = null;

document.addEventListener("DOMContentLoaded", function () {

    /* Casual Games Modal */

    refreshButton = document.getElementById("refreshBtn");

    refreshButton.addEventListener("animationend", () => {
        refreshButton.classList.remove("spinning");
    });

    lobbyTable = document.getElementById("lobby-table");

    /* Create Game Modal */

    document.getElementById("create-room-form").addEventListener("submit", async e => {
        e.preventDefault();

        const response = await fetch(e.target.action, {
            method: "POST",
            body: new FormData(e.target)
        });

        const result = await response.json();

        if (!response.ok) {
            document.getElementById("room-creation-error").textContent = result.error;
            return;
        }

        window.location.href = result.redirectUrl;
    });
});

/* Casual Games Modal */

let lobbyActive = false;
let lobbyTimer = null; // schedule an automated refresh for 5 seconds after the current refresh (manual or automated) finishes
let isLoading = false; // prevent sending a second update request while waiting for the server to return one / processing and applying one

function populateLobby() {

    if (!lobbyActive) return;

    if (isLoading) return;

    if (lobbyTimer) {
        clearTimeout(lobbyTimer);
        lobbyTimer = null;
    }

    isLoading = true;

    $.ajax({
        url: "/Home/PopulateLobbyPartial",
        method: "GET",
        success: function (html) {
            if (!lobbyActive) return;

            lobbyTable.innerHTML = html;
        },
        complete: function () {
            isLoading = false;

            if (lobbyActive) {
                lobbyTimer = setTimeout(populateLobby, 5000);
            }
        }
    });
}

function startLobbyRefresh() {
    if (lobbyActive) return;

    lobbyActive = true;
    populateLobby(); // initial trigger and start loop
}

function stopLobbyRefresh() {
    lobbyActive = false;

    if (lobbyTimer) {
        clearTimeout(lobbyTimer);
        lobbyTimer = null;
    }
}

$('#join-game-modal').on('shown.bs.modal', function () {
    startLobbyRefresh();
});

$('#join-game-modal').on('hidden.bs.modal', function () {
    stopLobbyRefresh();
});

function refreshLobby() { // manual refresh clicked
    populateLobby();

    refreshButton.classList.remove("spinning");
    void refreshButton.offsetWidth;
    refreshButton.classList.add("spinning");

    document.getElementById("room-join-error").textContent = "";
};

async function validateJoinRoom(roomId) {
    const response = await fetch(`/Room/ValidateJoin/${roomId}`);

    const result = await response.json();

    if (!response.ok) {

        document.getElementById("room-join-error").textContent = result.error;
        return;
    }

    window.location.href = result.redirectUrl;
}