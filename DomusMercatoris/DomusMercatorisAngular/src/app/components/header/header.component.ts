import { Component, inject, ChangeDetectionStrategy, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { CartService } from '../../services/cart.service';
import { ProductService } from '../../services/product.service';
import { SearchBarComponent } from '../search-bar/search-bar.component';
import { environment } from '../../../environments/environment';
import { signal } from '@angular/core';
import { OrdersService, CheckoutPayload } from '../../services/orders.service';
import { AlertService } from '../../services/alert.service';
import { Subject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { HeaderNotificationComponent } from '../header-notification/header-notification.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, SearchBarComponent, FormsModule, HeaderNotificationComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderComponent implements OnDestroy {
  authService = inject(AuthService);
  router = inject(Router);
  cartService = inject(CartService);
  productService = inject(ProductService);
  ordersService = inject(OrdersService);
  alertService = inject(AlertService);

  isCartOpen = false;
  isProfileOpen = false;
  imageLoadFailed = signal(false);

  @ViewChild(HeaderNotificationComponent) notification?: HeaderNotificationComponent;

  constructor() { }

  ngOnDestroy() {
    // Clean up if needed
  }

  handleProfileClick() {
    if (this.authService.currentUser()) {
      this.isProfileOpen = !this.isProfileOpen;
      this.isCartOpen = false;
      this.notification?.close();
    } else {
      this.authService.toggleLogin();
    }
  }

  closeProfile() {
    this.isProfileOpen = false;
  }

  handleCartClick() {
    this.isCartOpen = !this.isCartOpen;
    this.isProfileOpen = false;
    this.notification?.close();
  }

  onNotificationOpened() {
    this.isCartOpen = false;
    this.isProfileOpen = false;
  }



  async checkout() {
    // Check for active checkout in localStorage
    const activeOrderId = localStorage.getItem('active_checkout_id');
    if (activeOrderId) {
      // Verify if this order is still pending via API
      this.ordersService.getOrderById(+activeOrderId).subscribe({
        next: (order) => {
          if (order.status === 'PaymentPending') {
            this.isCartOpen = false;
            this.alertService.showAlert('You have a pending payment session. Redirecting...');
            this.router.navigate(['/payment-waiting', activeOrderId]);
          } else {
            // Status is not pending (e.g. Paid, Cancelled, etc.), so it's stale.
            localStorage.removeItem('active_checkout_id');
            this.performCheckout();
          }
        },
        error: () => {
          // If error (e.g. 404), assume invalid and clear
          localStorage.removeItem('active_checkout_id');
          this.performCheckout();
        }
      });
      return;
    }

    this.performCheckout();
  }

  performCheckout() {
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
      if (!user.address) {
        this.alertService.showAlert('Please add a shipping address to your profile before checking out.', () => {
          this.router.navigate(['/profile']);
          this.closeProfile(); // Ensure profile dropdown doesn't stay open if it was somehow involved
        });
        return;
      }
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
        // Store active checkout ID to prevent multiple checkouts
        localStorage.setItem('active_checkout_id', res.id.toString());
        // Redirect to Waiting Payment page
        this.router.navigate(['/payment-waiting', res.id]);
      },
      error: (err) => {
        console.error('Checkout failed', err);
        if (err.status === 409 && err.error?.code === 'STOCK_ADJUSTED') {
          // Stock adjusted
          this.alertService.showAlert(`Some items in your cart have been updated due to insufficient stock. Please review your cart.`);

          // Refresh cart to get updated quantities
          this.cartService.fetchCart();

          // Highlight adjusted items
          if (err.error.adjustments) {
            const adjustmentIds: number[] = [];
            const warnings = new Map<number, string>();

            err.error.adjustments.forEach((a: any) => {
              const item = this.cartService.items().find(i =>
                i.product.id === a.productId &&
                ((!i.variant && !a.variantProductId) || (i.variant?.id === a.variantProductId))
              );
              if (item && item.id) {
                adjustmentIds.push(item.id);
                warnings.set(item.id, `Stock limited. Available: ${a.availableQuantity}`);
              }
            });

            this.cartService.highlightedItemIds.set(adjustmentIds);
            this.cartService.itemWarnings.set(warnings);

            // Clear highlight after some time? Maybe 5 seconds
            setTimeout(() => {
              this.cartService.highlightedItemIds.set([]);
              this.cartService.itemWarnings.set(new Map());
            }, 10000);
          }
        } else {
          this.alertService.showAlert('Checkout failed. See console.');
        }
      }
    });
  }

  inc(item: any) {
    this.cartService.increment(item);
  }

  dec(item: any) {
    this.cartService.decrement(item);
  }

  onQtyChange(item: any, value: any) {
    const strVal = value?.toString();

    // If user types "0", remove the item
    if (strVal === '0') {
      this.remove(item);
      return;
    }

    // Regex to ensure positive integer only (no decimals, no leading zeros unless just "0" but we want >0)
    if (strVal && /^[1-9][0-9]*$/.test(strVal)) {
      const qty = parseInt(strVal, 10);
      this.cartService.updateQuantity(item, qty);
    }
  }

  preventInvalidInput(event: KeyboardEvent) {
    // Allow: Backspace, Delete, Tab, Escape, Enter, Arrows, Home, End
    if ([
      'Backspace', 'Delete', 'Tab', 'Escape', 'Enter',
      'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown',
      'Home', 'End'
    ].includes(event.key)) {
      return;
    }
    // Allow: Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
    if ((event.ctrlKey || event.metaKey) && ['a', 'c', 'v', 'x'].includes(event.key.toLowerCase())) {
      return;
    }
    // Block non-numeric
    // Also block 'e', '.', ',', '-', '+' explicitly if they slip through /^\d$/ check (usually they don't match \d)
    if (!/^\d$/.test(event.key)) {
      event.preventDefault();
    }
  }

  onPaste(event: ClipboardEvent) {
    const clipboardData = event.clipboardData;
    const pastedData = clipboardData?.getData('text') || '';
    if (!/^[1-9][0-9]*$/.test(pastedData)) {
      event.preventDefault();
    }
  }

  onBlur(item: any, event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.value || !/^[1-9][0-9]*$/.test(input.value)) {
      input.value = item.qty.toString();
    }
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
