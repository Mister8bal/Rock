
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



}

