'use strict';

define(
    [
        'app',
        'marionette'
    ], function (App, Marionette) {
        return Marionette.ItemView.extend({
            template: 'Update/UpdateItemViewTemplate'
        });
    });
