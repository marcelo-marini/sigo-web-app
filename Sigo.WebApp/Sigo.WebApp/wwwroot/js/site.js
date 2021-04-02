// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(document).ready(function () {
    $('#confirm-delete').on('click', '.btn-ok', function (e) {
        var $modalDiv = $(e.delegateTarget);
        var id = $(this).data('itemId');
        $modalDiv.addClass('loading');
        $.post('/Standards/Delete/' + id).then(function () {
            $modalDiv.modal('hide').removeClass('loading');
            location.reload();
        });
    });

    $('#confirm-delete').on('show.bs.modal', function (e) {
        var data = $(e.relatedTarget).data();
        $('.btn-ok', this).data('itemId', data.itemId);
    });
});