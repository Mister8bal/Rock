const CCV_countdown = (function() {

    let hours, intervalId, endIntervalId, minutes, seconds, $countDownEl, $streamEl, startDate, endDate;
    
    const that = {

        goLive:function() {
            const _this = this;
            $countDownEl.slideUp(function(){
                $streamEl.slideDown();
            });
            let n = Date.now();
            return (endIntervalId = setInterval(function(){
                if(Date.now() >= _this.endDate.getTime()){
                    _this.endLive();
                }
            },1000));
        },

        endLive:function(){
            $streamEl.slideUp(function(){
                $countDownEl.slideDown();
            });
            clearInterval(endIntervalId);
            this.loadCountdown();
        },

        /**
         * Loads the countdown timer found at $el.
         *
         * @param {Object jQuery} $el: A jquery element representing the parent countdown element
         * @param {Object jQuery} $el: A jquery element representing the parent stream element
         * representing the countdown timer markup.
         */
        loadCountdown:function($el, $sEl) {

            $countDownEl = $countDownEl || $el;
            $streamEl = $streamEl || $sEl

            const _this = this;
            let seconds_till;
            let d = this.getCountdownDate($countDownEl);
            let current = Date.now();

            seconds_till = (d.getTime() - current) / 1000;

            if(seconds_till > 0){
                $streamEl.hide();
                
            }else{
                $countDownEl.hide();
            }

            days = Math.floor(seconds_till / 86400);
            hours = Math.floor((seconds_till % 86400) / 3600);
            minutes = Math.floor((seconds_till % 3600) / 60);
            seconds = Math.floor(seconds_till % 60);

            return (intervalId = setInterval(function() {
            if (--seconds < 0) {
                seconds = 59;
                if (--minutes < 0) {
                minutes = 59;
                if (--hours < 0) {
                    hours = 23;
                    if (--days < 0) {
                    days = 0;
                    }
                }
                }
            }

            $countDownEl
                .find(".countdown-days")
                .html(days.toString().length < 2 ? "0" + days : days);
            $countDownEl
                .find(".countdown-hours")
                .html(hours.toString().length < 2 ? "0" + hours : hours);
            $countDownEl
                .find(".countdown-mins")
                .html(minutes.toString().length < 2 ? "0" + minutes : minutes);
            $countDownEl
                .find(".countdown-secs")
                .html(seconds.toString().length < 2 ? "0" + seconds : seconds);
            if (seconds === 0 && minutes === 0 && hours === 0 && days === 0) {
                _this.goLive();
                return clearInterval(intervalId);
            }
            }, 1000));
        },

        /**
         * Gets a Date object from data attribuites set
         * on a jquery object.  If no data attributes are
         * set, then a date string representing the current
         * time and date will be returned.
         *
         * @param {Object jQuery} $el: A jquery element with
         * data attributes containing values for the date,
         * hours, minutes and seconds.
         */
        getCountdownDate:function($el) {

            let out = null;
            
            let dateStr = $el.data("countdown-date") || new Date().toLocaleDateString();
            let startTime = $el.data("stream-start") || '';
            let endTime = $el.data("stream-end") || '';
            let current = Date.now();

            this.startDate = new Date(dateStr + ' ' + startTime + ' UTC-0700') || new Date();
            this.endDate = new Date(dateStr + ' ' + endTime + ' UTC-0700') || new Date();

            /**
             * If the start date is in the future,
             * return the start date. 
             */
            if( current < this.startDate.getTime() ) {
                return this.startDate
            }

            /**
             * If we are past the end time, return the start time for tomorrow.
             */
            if( current > this.endDate.getTime() ) {
                out = new Date();
                out.setTime( this.startDate.getTime() + 86400000 );
                return out;
            }

            /**
             * If we get here, we are between the start time 
             * and the end time.  Just return the start time.
             */
            return this.startDate;

        }

  
    }
  
    days = void 0;
    hours = void 0;
    minutes = void 0;
    seconds = void 0;
    intervalId = void 0;

    return that;Î

}());
