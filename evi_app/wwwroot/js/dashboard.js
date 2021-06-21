"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/messagesHub").build();

document.getElementById("connection_status").innerHTML = "Connection status: offline";

connection.on("ReceiveMessage", function (user, message, fiscal_id) {

    const audio = new Audio("/sounds/new.mp3");
    audio.play();

    var today = new Date();
    var date = today.getDate() + '-' + (today.getMonth() + 1) + '-' + today.getFullYear();
    var time = today.getHours() + ":" + today.getMinutes() + ":" + today.getSeconds();
    var dateTime = time + ' | ' + date;

    $('#tbody').prepend(`<tr>
          <td class="row-index text-center">
                <p>${dateTime}</p></td>
          <td class="row-index text-center">
                <p>${user}</p></td>
           <td class="text-center">
            <a class="btn btn-primary open-XmlDialog"
                data-toggle="modal" href="#ShowXmlModal" data-id="${message}">Open message</a>
            </td>
           </tr>`);
    
    return;
});

connection.start().then(function () {
    document.getElementById("connection_status").innerHTML = "Connection status: established";
}).catch(function (err) {
    return console.error(err.toString());
});

$(document).on("click", ".open-XmlDialog", function () {
    var payload = $(this).data('id');
    $(".modal-body #xml").val(payload);
});