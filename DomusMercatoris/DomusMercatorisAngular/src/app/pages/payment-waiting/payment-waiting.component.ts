import { Component, OnInit, inject, signal, effect, OnDestroy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentService } from '../../services/payment.service';
import { AlertService } from '../../services/alert.service';
import { CartService } from '../../services/cart.service';
import { OrdersService, OrderResponse } from '../../services/orders.service';
import { AuthService } from '../../services/auth.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-payment-waiting',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payment-waiting.component.html',
  styleUrls: ['./payment-waiting.component.css']
})
export class PaymentWaitingComponent implements OnInit, OnDestroy {
  route = inject(ActivatedRoute);
  router = inject(Router);
  paymentService = inject(PaymentService);
  alertService = inject(AlertService);
  cartService = inject(CartService);
  ordersService = inject(OrdersService);
  authService = inject(AuthService);
  
  orderId = signal<string>('');
  orderDetails = signal<OrderResponse | null>(null);
  
  // Timeout & Code logic
  timeLeft = signal<number>(35); // 35 seconds
  confirmationCode = signal<string>('');
  isTimedOut = signal<boolean>(false);
  showRejectModal = signal<boolean>(false);
  timerInterval: any;

  formattedTime = computed(() => {
    const m = Math.floor(this.timeLeft() / 60);
    const s = this.timeLeft() % 60;
    return `${m}:${s < 10 ? '0' : ''}${s}`;
  });

  customerName = computed(() => {
    const user = this.authService.currentUser();
    if (user) {
        return `${user.firstName} ${user.lastName}`;
    }
    return 'Valued Customer';
  });

  constructor() {
    effect(() => {
      const status = this.paymentService.paymentStatus();
      if (status && status.orderId.toString() === this.orderId()) {
        if (status.isApproved) {
            // Payment successful, now we can clear the cart
            this.cartService.clear();
            this.paymentService.activePaymentCode.set(null); // Clear code
            sessionStorage.removeItem(`payment_expiry_${this.orderId()}`); // Clean up timer storage
            localStorage.removeItem('active_checkout_id'); // Clear active checkout lock
            if (this.timerInterval) clearInterval(this.timerInterval);

            // Wait a bit to show success animation
            setTimeout(() => {
                // Navigate to success page or show success message
                this.alertService.showAlert('Payment Approved! Order #' + status.orderId, () => {
                  this.router.navigate(['/']); // Redirect to home for now
                });
            }, 2000);
        } else {
            localStorage.removeItem('active_checkout_id'); // Clear active checkout lock on failure
            this.alertService.showAlert('Payment Failed! Please try again.', () => {
              this.router.navigate(['/']); // Redirect to cart or retry
            });
        }
      }
    });
  }

  ngOnInit() {
    // Clear any stale payment status
    this.paymentService.paymentStatus.set(null);
    
    this.route.params.subscribe(params => {
      this.orderId.set(params['id']);
      if (this.orderId()) {
        this.paymentService.startConnection(this.orderId());
        this.fetchOrderDetails();
        this.startTimer();
      }
    });
  }

  fetchOrderDetails() {
    this.ordersService.getOrderById(+this.orderId()).subscribe((data) => {
        this.orderDetails.set(data);
        if (data.paymentCode && data.status === 'PaymentPending') {
            this.paymentService.activePaymentCode.set(data.paymentCode);
        }
    });
  }

  startTimer() {
    const storageKey = `payment_expiry_${this.orderId()}`;
    const storedExpiry = sessionStorage.getItem(storageKey);
    let expiryTime: number;

    if (storedExpiry) {
      expiryTime = parseInt(storedExpiry, 10);
    } else {
      expiryTime = Date.now() + 35000; // 35 seconds from now
      sessionStorage.setItem(storageKey, expiryTime.toString());
    }

    this.updateTimeLeft(expiryTime);

    this.timerInterval = setInterval(() => {
      this.updateTimeLeft(expiryTime);
    }, 1000);
  }

  updateTimeLeft(expiryTime: number) {
    const now = Date.now();
    const diff = Math.ceil((expiryTime - now) / 1000);
    
    if (diff > 0) {
      this.timeLeft.set(diff);
    } else {
      this.timeLeft.set(0);
      if (!this.isTimedOut()) {
        this.isTimedOut.set(true);
        this.paymentService.activePaymentCode.set(null); // Clear code
        clearInterval(this.timerInterval);
        sessionStorage.removeItem(`payment_expiry_${this.orderId()}`);
        localStorage.removeItem('active_checkout_id'); // Clear active checkout lock

        // Automatically reject payment on timeout
        this.paymentService.rejectPayment(this.orderId()).subscribe(() => 
          console.log('Payment rejected due to timeout')
        );
      }
    }
  }

  verifyCode() {
    if (!this.confirmationCode()) return;
    this.paymentService.verifyCode(+this.orderId(), this.confirmationCode())
      .subscribe(() => {
          // Success handled by SignalR
      });
  }

  rejectPayment() {
    this.showRejectModal.set(true);
  }

  confirmReject() {
    this.showRejectModal.set(false);
    this.paymentService.rejectPayment(this.orderId()).subscribe(() => {
        // Handled by SignalR (PaymentFailed status)
    });
  }

  cancelReject() {
    this.showRejectModal.set(false);
  }

  ngOnDestroy() {
    this.paymentService.stopConnection();
    // Keep code visible for navigation
    if (this.timerInterval) clearInterval(this.timerInterval);
  }
}
