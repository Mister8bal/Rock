
function pageLoad() {

    // configure date picker
    $('#dpVisitDate').datepicker({
        beforeShowDay: function (date) {
            var show = false;
            if (date.getDay() == 0 || date.getDay() == 6) {
                show = true;
            };

            return show;
        }
    });

    $('#ddlServiceTime').on('change', function () {
        $('#divSpouse').removeClass('hidden');
    });
}

function ShowChildrenForm() {
    $('#divChildrenQuestion').toggleClass('hidden');
    $('#divChildrenForm').toggleClass('hidden');
    $('#btnChildrenAddAnother').toggleClass('hidden');
    $('#btnChildrenNext').toggleClass('hidden');
}