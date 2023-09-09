populateLobby = function () {
    getNumRooms();
    $.ajax({
        url: "/Home/PopulateLobby",
        method: "GET",
        success: function (data) {
            data = JSON.parse(data);

            var table = document.getElementById("lobby-table");
            while (table.rows.length > 0) {
                table.deleteRow(0)
            };

            for (i = 0; i < data.length; i++) {
                var row = table.insertRow(table.rows.length - 1);

                var cell0 = row.insertCell(0);
                var cell1 = row.insertCell(1);
                var cell2 = row.insertCell(2);
                var cell3 = row.insertCell(3);
                var cell4 = row.insertCell(4);
                var cell5 = row.insertCell(5);

                cell0.innerHTML = data[i].Started;
                cell1.innerHTML = data[i].West;
                cell2.innerHTML = data[i].North;
                cell3.innerHTML = data[i].East;
                cell4.innerHTML = data[i].South;
                var btn = document.createElement("button");
                btn.innerHTML = "Join"
                btn.classList = "btn btn-sm btn-primary py-0";
                const link = "/Room/Index/" + data[i].RoomId;
                btn.onclick = function () {
                    window.location.href = link;
                };
                cell5.appendChild(btn);
            }
        }
    })
};

getNumRooms = function () {
    $.ajax({
        url: "/Home/GetNumRooms",
        method: "GET",
        success: function (data) {
            document.getElementById("numRooms").innerHTML = "Klaverjassen rooms: " + data;
        }
    });
};