"use strict";

function populateLobby() {
    getNumRooms();
    $.ajax({
        url: "/Home/PopulateLobby",
        method: "GET",
        success: function (data) {
            data = JSON.parse(data);

            let table = document.getElementById("lobby-table");
            while (table.rows.length > 0) {
                table.deleteRow(0)
            };

            for (let i = 0; i < data.length; i++) {
                let row = table.insertRow(table.rows.length - 1);

                let cell0 = row.insertCell(0);
                let cell1 = row.insertCell(1);
                let cell2 = row.insertCell(2);
                let cell3 = row.insertCell(3);
                let cell4 = row.insertCell(4);
                let cell5 = row.insertCell(5);

                cell0.innerHTML = data[i].Started;
                cell1.innerHTML = data[i].West;
                cell2.innerHTML = data[i].North;
                cell3.innerHTML = data[i].East;
                cell4.innerHTML = data[i].South;
                let btn = document.createElement("button");
                btn.classList = "btn btn-primary py-0 ps-1 pe-2";
                const link = "/Room/Index/" + data[i].RoomId;
                btn.onclick = function () {
                    window.location.href = link;
                };
                let icon = document.createElement("i");
                icon.classList = "bi bi-box-arrow-in-right";
                btn.appendChild(icon);
                cell5.appendChild(btn);
            }
        }
    })
};

function getNumRooms() {
    $.ajax({
        url: "/Home/GetNumRooms",
        method: "GET",
        success: function (data) {
            document.getElementById("numRooms").innerHTML = "Belot rooms: " + data;
        }
    });
};