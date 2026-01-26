import { Component, OnInit, inject, signal, effect, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentService } from '../../services/payment.service';
import { AlertService } from '../../services/alert.service';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-payment-waiting',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payment-waiting.component.html',
  styleUrls: ['./payment-waiting.component.css']
})
export class PaymentWaitingComponent implements OnInit, OnDestroy {
  route = inject(ActivatedRoute);
  router = inject(Router);
  paymentService = inject(PaymentService);
  alertService = inject(AlertService);
  cartService = inject(CartService);
  
  orderId = signal<string>('');
  
  constructor() {
    effect(() => {
      const status = this.paymentService.paymentStatus();
      if (status && status.orderId.toString() === this.orderId()) {
        if (status.isApproved) {
            // Payment successful, now we can clear the cart
            this.cartService.clear();

            // Wait a bit to show success animation
            setTimeout(() => {
                // Navigate to success page or show success message
                this.alertService.showAlert('Payment Approved! Order #' + status.orderId, () => {
                  this.router.navigate(['/']); // Redirect to home for now
                });
            }, 2000);
        } else {
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
      }
    });
  }

  ngOnDestroy() {
    this.paymentService.stopConnection();
  }
}
