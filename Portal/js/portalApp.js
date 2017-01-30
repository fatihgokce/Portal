﻿/// <reference path="C:\Projects\Portal\Portal\Scripts/_references.js" />
//
var portalApp = {
    mesajGoster:function(msj){
        $.bootstrapGrowl(msj, {
            ele: 'body', // which element to append to
            type: 'info', // (null, 'info', 'danger', 'success')
            offset: { from: 'top', amount: 50 }, // 'top', or 'bottom'
            align: 'right', // ('left', 'right', or 'center')
            width: 250, // (integer, or 'auto')
            delay: 1500, // Time while the message will be displayed. It's not equivalent to the *demo* timeOut!
            allow_dismiss: true, // If true then will display a cross to close the popup.
            stackup_spacing: 10 // spacing between consecutively stacked growls.
        });
    
    }
   
}