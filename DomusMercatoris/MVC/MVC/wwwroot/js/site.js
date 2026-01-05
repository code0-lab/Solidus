// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function toggleProductCard(cardBody) {
    var details = cardBody.querySelector('.product-details');
    if (!details) return;
    
    var isHidden = details.classList.contains('d-none');
    
    // Close all other product details
    var allCards = document.querySelectorAll('.product-card');
    for(var i=0; i<allCards.length; i++) {
        var d = allCards[i].querySelector('.product-details');
        if(d) d.classList.add('d-none');
        allCards[i].classList.remove('expanded');
    }
    
    // Toggle the clicked one
    if (isHidden) {
        details.classList.remove('d-none');
        // Find the parent .product-card and add expanded class
        var card = cardBody.closest('.product-card');
        if(card) card.classList.add('expanded');
    }
}