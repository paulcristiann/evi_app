"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/messagesHub").build();

document.getElementById("connection_status").innerHTML = "Connection status: offline";

document.getElementById("filter_button").addEventListener("click", apply_filter);

function apply_filter() {
    var filter = document.getElementById("filter_box").value;
    if (filter != "") {
        document.getElementById("filters").innerHTML = filter;
    } else {
        document.getElementById("filters").innerHTML = "";
    }
}

connection.on("ReceiveMessage", function (user, message, fiscal_id) {

    const audio = new Audio("/sounds/new.mp3");
    audio.play();

    var today = new Date();
    var date = today.getDate() + '-' + (today.getMonth() + 1) + '-' + today.getFullYear();
    var time = today.getHours() + ":" + today.getMinutes() + ":" + today.getSeconds();
    var dateTime = time + ' | ' + date;

    document.getElementById("template").style.display = "none";

    var template = document.getElementById("template");
    var clone = template.cloneNode(true);
    clone.id = user;
    clone.style.display = "block";
    clone.querySelector("#title").style.display = "none";

    clone.querySelector("#fiscal-number").innerHTML = `Fiscal number: ${fiscal_id}`;
    clone.querySelector("#fiscal-number").style.display = "block";

    clone.querySelector("#date").innerHTML = `Received at: ${dateTime}`;
    clone.querySelector("#date").style.display = "block";

    clone.querySelector("#tester").innerHTML = `Technician account: ${user}`;
    clone.querySelector("#tester").style.display = "block";

    clone.querySelector("#content").textContent = htmlDecode(message);
    clone.querySelector("#button").style.display = "block";

    var space = document.createElement("br");
    document.getElementById("content").prepend(space);
    document.getElementById("content").prepend(clone);
    return;
});

function htmlDecode(input) {
    var e = document.createElement('textarea');
    e.innerHTML = input;
    // handle case of empty input
    return e.childNodes.length === 0 ? "" : e.childNodes[0].nodeValue;
}

connection.start().then(function () {
    document.getElementById("connection_status").innerHTML = "Connection status: established";
}).catch(function (err) {
    return console.error(err.toString());
});