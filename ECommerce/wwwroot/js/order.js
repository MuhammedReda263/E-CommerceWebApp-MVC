
//DataTables
$(document).ready(function () {

    var url = window.location.search;

    if (url.includes("inprocess")) {
        loadDataTable("inprocess");
    } else if (url.includes("completed")) {
        loadDataTable("completed");
    } else if (url.includes("pending")) {
        loadDataTable("pending");
    } else if (url.includes("approved")) {
        loadDataTable("approved");
    } else {
        loadDataTable("all");
    }
});

function loadDataTable(status) {
    $('#tablelData').DataTable({
        "ajax": {
            "url": "/admin/order/getall?status=" + status,
            "dataSrc": "data"
        },
        "columns": [
            { data: 'id', "width": "10%" },
            { data: 'name', "width": "15%" },
            { data: 'phoneNumber', "width": "15%" },
            { data: 'applicationUser.email', "width": "20%" },
            { data: 'orderStatus', "width": "15%" },
            { data: 'orderTotal', "width": "15%" },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="btn-group" role="group">
                            <a href = "/admin/order/detail?orderId=${data}" class="btn btn-primary btn-sm">
                                <i class="bi bi-pencil-fill me-1" style="font-size: 0.8rem;"></i>
                            </a>
                        </div>`;
                },
                "width": "10%"
            }
        ]
    });

}

