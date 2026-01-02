import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Product } from '../../models/product.model';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css'
})
export class ProductListComponent {
  @Input({ required: true }) products: Product[] = [];
  @Output() selectProduct = new EventEmitter<Product>();

  cartService = inject(CartService);

  addToCart(event: Event, product: Product) {
    event.stopPropagation();
    this.cartService.add(product);
    // Optional: show a small feedback or rely on the cart count updating
  }
}
