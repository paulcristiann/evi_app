"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/messagesHub").build();

document.getElementById("connection_status").innerHTML = "Connection status: offline";

connection.on("ReceiveMessage", function (user, message, fiscal_id) {

    var nr_m = document.getElementById("nr_m").value;
    var fiscal = document.getElementById("fiscal_number").value;

    if (fiscal != fiscal_id) {
        return;
    }

    add_message(user, message, fiscal_id);
    if (timer.getTimeValues().minutes == nr_m) {
        add_lap($("#timer").text(), 'green');
    } else {
        add_lap($("#timer").text(), 'red');
    }

    timer.reset();

    const audio = new Audio("/sounds/new.mp3");
    audio.play();

    timer.start();

    return;
});

function add_lap(lap, color) {

    document.getElementById("lap_template").style.display = "none";

    var template = document.getElementById("lap_template");
    var clone = template.cloneNode(true);
    clone.id = lap;
    clone.style.display = "block";
    clone.querySelector("#title").style.display = "block";
    if (lap == '00:00:00') {
        clone.querySelector("#title").innerHTML = `First message arrived`;
    } else {
        clone.querySelector("#title").innerHTML = `Message received after: +${lap}`;
        clone.querySelector("#title").style.color = color;
    }
    var space = document.createElement("br");
    document.getElementById("lap_content").append(clone);
    document.getElementById("lap_content").append(space);
    return;
}

function add_message(user, message, fiscal_id) {

    var today = new Date();
    var date = today.getDate() + '-' + (today.getMonth() + 1) + '-' + today.getFullYear();
    var time = today.getHours() + ":" + today.getMinutes() + ":" + today.getSeconds();
    var dateTime = time + ' | ' + date;

    document.getElementById("message_template").style.display = "none";

    var template = document.getElementById("message_template");
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

    var space = document.createElement("br");
    document.getElementById("message_content").append(clone);
    document.getElementById("message_content").append(space);
    return;
}

connection.start().then(function () {
    document.getElementById("connection_status").innerHTML = "Connection status: established";
}).catch(function (err) {
    return console.error(err.toString());
});

var timer = new easytimer.Timer();

timer.addEventListener('secondsUpdated', function (e) {
    $('#timer').html(timer.getTimeValues().toString());
    var nr_m = document.getElementById("nr_m").value;
    if (timer.getTimeValues().minutes == nr_m) {
        $('#timer').css('color', 'green');
    } else {
        $('#timer').css('color', 'red');
    }
});

timer.addEventListener('started', function (e) {
    $('#timer').html(timer.getTimeValues().toString());
});

timer.addEventListener('reset', function (e) {
    $('#timer').html(timer.getTimeValues().toString());
});

function htmlDecode(input) {
    var e = document.createElement('textarea');
    e.innerHTML = input;
    // handle case of empty input
    return e.childNodes.length === 0 ? "" : e.childNodes[0].nodeValue;
}