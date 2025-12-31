import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent {
  authService = inject(AuthService);
  router = inject(Router);
  cartService = inject(CartService);
  isCartOpen = false;

  handleProfileClick() {
    if (this.authService.currentUser()) {
      this.router.navigate(['/profile']);
    } else {
      this.authService.toggleLogin();
    }
  }

  handleCartClick() {
    this.isCartOpen = !this.isCartOpen;
  }

  inc(item: any) {
    this.cartService.increment(item);
  }

  dec(item: any) {
    this.cartService.decrement(item);
  }

  remove(item: any) {
    this.cartService.remove(item);
  }

  clearCart() {
    this.cartService.clear();
  }
}
