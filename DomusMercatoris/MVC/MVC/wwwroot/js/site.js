// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function toggleProductCard(cardBody) {
    var details = cardBody.querySelector('.product-details');
    if (!details) return;
    
    var isHidden = details.classList.contains('d-none');
    
    // Close all other product details
    var allDetails = document.querySelectorAll('.product-details');
    for(var i=0; i<allDetails.length; i++) {
        allDetails[i].classList.add('d-none');
    }
    
    // Toggle the clicked one
    if (isHidden) {
        details.classList.remove('d-none');
    }
}