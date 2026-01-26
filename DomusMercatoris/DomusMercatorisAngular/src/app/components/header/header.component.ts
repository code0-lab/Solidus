import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CartService } from '../../services/cart.service';
import { SearchBarComponent } from '../search-bar/search-bar.component';
import { environment } from '../../../environments/environment';
import { signal } from '@angular/core';
import { OrdersService, CheckoutPayload } from '../../services/orders.service';
import { AlertService } from '../../services/alert.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, SearchBarComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderComponent {
  authService = inject(AuthService);
  router = inject(Router);
  cartService = inject(CartService);
  ordersService = inject(OrdersService);
  alertService = inject(AlertService);
  isCartOpen = false;
  isProfileOpen = false;
  imageLoadFailed = signal(false);


  handleProfileClick() {
    if (this.authService.currentUser()) {
      this.isProfileOpen = !this.isProfileOpen;
      this.isCartOpen = false; // Close cart if open
    } else {
      this.authService.toggleLogin();
    }
  }

  closeProfile() {
    this.isProfileOpen = false;
  }

  handleCartClick() {
    this.isCartOpen = !this.isCartOpen;
    this.isProfileOpen = false; // Close profile if open
  }

  async checkout() {
    const items = this.cartService.items();
    if (items.length === 0) return;

    const user = this.authService.currentUser();
    // Assuming all products belong to the same company for now, or just picking the first one.
    const product = items[0].product;
    const companyId = product.companyId;

    if (!companyId) {
      this.alertService.showAlert('Product data incomplete (missing company info). Please refresh the page to update product data.');
      return;
    }

    const payload: CheckoutPayload = {
      companyId: companyId,
      items: items.map(i => ({
        productId: i.product.id,
        variantProductId: i.variant?.id,
        quantity: i.qty
      }))
    };

    if (user) {
      payload.userId = user.id;
    } else {
      this.alertService.showAlert('Please log in before proceeding with payment.', () => {
        this.authService.toggleLogin();
      });
      return;
    }

    this.ordersService.checkout(payload).subscribe({
      next: (res) => {
        // Do not clear cart here. Wait for payment success.
        this.isCartOpen = false;
        // Redirect to Waiting Payment page
        this.router.navigate(['/payment-waiting', res.id]);
      },
      error: (err) => {
        console.error('Checkout failed', err);
        this.alertService.showAlert('Checkout failed. See console.');
      }
    });
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

  handleImageError() {
    this.imageLoadFailed.set(true);
  }
}
