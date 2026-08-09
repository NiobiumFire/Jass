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

/* Welcome text info ticker */

(function () {
    const wrap = document.getElementById('ticker-wrap');
    const track = document.getElementById('ticker-track');

    // --- state ---
    // "offset" stores where the track is scrolled to, autoplay and dragging modify it, painted with CSS transform
    let offset = -50;

    let isPaused = false;
    let isDragging = false;

    let dragStartX = 0;
    let dragStartOffset = 0;
    let movedDuringDrag = false; // distinguish "click" from "drag"

    const DRAG_CLICK_THRESHOLD = 5; // movement in px before it counts as a drag, not a click
    const SPEED = 0.25;              // px per animation frame for autoplay

    // half the track's width, width of one copy of the content
    function getHalfWidth() {
        return track.scrollWidth / 2;
    }

    // keep offset inside [0, half) so the loop never runs out of track
    function render() {
        const half = getHalfWidth();
        if (offset >= half) offset -= half;
        if (offset < 0) offset += half;
        track.style.transform = 'translateX(' + (-offset) + 'px)';
    }

    // autoplay loop
    function tick() {
        if (!isPaused && !isDragging) {
            offset += SPEED;
            render();
        }
        requestAnimationFrame(tick);
    }

    // click/tap to toggle pause
    function setPaused(next) {
        isPaused = next;
        wrap.setAttribute('aria-pressed', String(!isPaused));
    }

    // drag to scroll
    wrap.addEventListener('pointerdown', (e) => {
        isDragging = true;
        movedDuringDrag = false;
        dragStartX = e.clientX;
        dragStartOffset = offset;
        wrap.classList.add('dragging');
        wrap.setPointerCapture(e.pointerId);
    });

    wrap.addEventListener('pointermove', (e) => {
        if (!isDragging) return;
        const delta = e.clientX - dragStartX;
        if (Math.abs(delta) > DRAG_CLICK_THRESHOLD) {
            movedDuringDrag = true;
        }
        offset = dragStartOffset - delta;
        render();
    });

    function endDrag(e) {
        if (!isDragging) return;
        isDragging = false;
        wrap.classList.remove('dragging');
        try { wrap.releasePointerCapture(e.pointerId); } catch (_) { }

        // if barely any drag, treat it as a click
        if (!movedDuringDrag) {
            setPaused(!isPaused);
        }
    }
    wrap.addEventListener('pointerup', endDrag);
    wrap.addEventListener('pointercancel', endDrag);

    requestAnimationFrame(tick);
})();

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