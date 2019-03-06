
function pageLoad() {

    
    $('#dpVisitDate').on('change', function () {
        // show campus picker
        $('#campusPicker').toggleClass('hidden');
    });


    $('#cpCampus').on('change', function () {

        $('#serviceTime').toggleClass('hidden');
    });

    $('#ddlServiceTime').on('change', function () {
        $('#spouse').toggleClass('hidden');
    });
}