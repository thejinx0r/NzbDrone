'use strict';
define(
    [
        'signalR'
    ], function () {

        _.extend(Backbone.Collection.prototype, {
            bindSignalR: function () {

                var collection = this;

                var processMessage = function (options) {

                    var model = new collection.model(options.resource, {parse: true});
                    collection.add(model, {merge: true});
                    console.log(options.action + ": %O", options.resource);
                };

                require(
                    [
                        'app'
                    ], function (app) {
                        collection.listenTo(app.vent, 'server:' + collection.url.replace('/api/', ''), processMessage)
                    });

                return this;
            },

            unbindSignalR: function () {


            }});
    });


