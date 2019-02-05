var CCV = CCV || {};

CCV.modals = (function(){

	let modalWrapperTemplate = '<div class="modal fade" id="" tabindex="-1" role="dialog" aria-hidden="true"><div class="modal-dialog modal-dialog-centered" role="document"></div></div>';
	let modalContentTemplate = '<div class="modal-content"></div>';
    let modalHeaderTemplate = '<div class="modal-header">';
    let modalFooterTemplate = '<div class="modal-footer"></div>';
    let modalBodyTemplate = '<div class="modal-body"></div>';
    let modalButtonsTemplate = '<div class="ccv-modal-buttons"></div>';
    let modalDismissButtonTemplate = '<button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>';
    let modalButtonTemplate = '<button type="button" class="btn btn-secondary"></button>';
    let $modalButtons = $(".ccv-modal");

    let _that = {};

    _that.modals = [];

    var init = function(){
	    if(!$modalButtons.length){
	    	return _that;
	    }
    	$modalButtons.each(createModalElements);
        generateModals();
    }
      
    var createModalElements = function(i, el){
    	
        var $el = $(el);
        var $modalWrapper = $(modalWrapperTemplate);
        var $modalContent = $(modalContentTemplate);
        var $modalBody = $(modalBodyTemplate);
        var $modalButtons = $(modalButtonsTemplate)
        var modalId = 'ccvModal'+i;

        _that.modals[i] = {
            wrapper:$modalWrapper,
            content:$modalContent,
            body:$modalBody,
            buttonsWrapper:$modalButtons,
            buttons:[],
            activationButton: $el
        }


        $modalWrapper.attr('id','ccvModal'+i);

        if($el.data('modal-title')){
            $modalBody.append('<h3>'+$el.data('modal-title'));
        }
        if($el.data('modal-content')){
            $modalBody.append('<p>'+$el.data('modal-content')+'</p>')
        }

        $modalContent.append($modalBody);
        $modalWrapper.append($modalContent);
        
        if(!$el.data('modal-type')){

            $modalButtons.append($(modalDismissButtonTemplate));
            return;
        }

        switch($el.data('modal-type')){
            case 'outside-navigation' :
                $cancelBtn = $(modalDismissButtonTemplate).text('Cancel').data('dismiss', 'modal');
                $continueBtn = $(modalButtonTemplate).text("continue").data('dismiss', 'modal');
                $modalButtons.append($continueBtn).append($cancelBtn);
                $modalBody.append($modalButtons);
                $continueBtn.click(function(e){
                    window.open($el.attr('href'),'_blank');
                });
            break;
        }

    }

    var generateModals = function(){
        for(let j=0; j<_that.modals.length; j++){
            $('body').append(_that.modals[j].wrapper);
            _that.modals[j].activationButton.click(function(e){
                e.preventDefault();
                console.log('showing modal:', _that.modals);
                console.log('modal index', j);
                _that.modals[j].wrapper.modal();
            });
        }
    }

    $(window).bind('load', init);

})()