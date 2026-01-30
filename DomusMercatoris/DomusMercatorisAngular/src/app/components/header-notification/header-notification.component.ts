import { Component, inject, signal, effect, output, ChangeDetectionStrategy, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../services/payment.service';
import { CartService } from '../../services/cart.service';

export interface Notification {
  code: string;
  amount: number;
  time: Date;
  read: boolean;
}

@Component({
  selector: 'app-header-notification',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './header-notification.component.html',
  styleUrl: './header-notification.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderNotificationComponent {
  paymentService = inject(PaymentService);
  cartService = inject(CartService);

  isNotificationsOpen = false;
  notifications = signal<Notification[]>([]);
  
  opened = output<void>();

  constructor() {
    effect(() => {
      const code = this.paymentService.activePaymentCode();
      if (code) {
        // Use untracked to prevent the effect from subscribing to notifications
        // This prevents the infinite loop where deleting (updating) the signal triggers the effect again
        const current = untracked(() => this.notifications());
        // Prevent duplicates
        if (!current.find(n => n.code === code)) {
          const newNotification: Notification = {
            code,
            amount: this.cartService.totalPrice(),
            time: new Date(),
            read: false
          };
          // Add to top
          this.notifications.update(n => [newNotification, ...n]);
        }
      }
    }, { allowSignalWrites: true });
  }

  get activeNotification() {
    // Show the most recent one if it is unread
    const latest = this.notifications()[0];
    return latest && !latest.read ? latest : null;
  }

  get unreadCount() {
    return this.notifications().filter(n => !n.read).length;
  }

  handleNotificationsClick() {
    this.isNotificationsOpen = !this.isNotificationsOpen;
    if (this.isNotificationsOpen) {
      this.opened.emit();
    }
  }

  close() {
    this.isNotificationsOpen = false;
  }

  closeNotification(code: string, event?: Event) {
    if (event) {
      event.stopPropagation();
    }
    this.notifications.update(list => 
      list.map(n => n.code === code ? { ...n, read: true } : n)
    );
  }

  deleteNotification(code: string, event?: Event) {
    if (event) {
      event.preventDefault();
      event.stopImmediatePropagation();
    }
    // Remove from local list
    this.notifications.update(list => list.filter(n => n.code !== code));
    
    // If this was the active payment code, clear it from service so it doesn't reappear
    if (this.paymentService.activePaymentCode() === code) {
      this.paymentService.activePaymentCode.set(null);
    }
  }
}
