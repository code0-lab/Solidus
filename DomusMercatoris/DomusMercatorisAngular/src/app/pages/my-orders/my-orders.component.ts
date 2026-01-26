import { Component, inject, OnInit, ChangeDetectionStrategy, signal, computed, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OrdersService, OrderResponse, CheckoutPayload } from '../../services/orders.service';
import { ToastService } from '../../services/toast.service';
import { AuthService } from '../../services/auth.service';
import { ProductService } from '../../services/product.service';
import { forkJoin, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

@Component({
  selector: 'app-my-orders',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-orders.component.html',
  styleUrl: './my-orders.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyOrdersComponent implements OnInit {
  ordersService = inject(OrdersService);
  toastService = inject(ToastService);
  router = inject(Router);
  route = inject(ActivatedRoute);
  authService = inject(AuthService);
  destroyRef = inject(DestroyRef);
  productService = inject(ProductService);

  activeTab = signal<'orders' | 'failed-orders'>('orders');
  orders = signal<OrderResponse[]>([]);
  expandedOrderIds = signal<Set<number>>(new Set());
  productImages = signal<Map<number, string>>(new Map());
  
  failedOrders = computed(() => this.orders().filter(o => o.status === 'PaymentFailed')); // PaymentFailed
  pastOrders = computed(() => this.orders().filter(o => o.status !== 'PaymentFailed')); // Active/Completed orders

  ngOnInit() {
    // Redirect if not logged in
    if (!this.authService.currentUser()) {
      this.router.navigate(['/']);
      return;
    }

    this.route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      if (params['tab']) {
        const tab = params['tab'];
        if (['orders', 'failed-orders'].includes(tab)) {
          this.setActiveTab(tab as 'orders' | 'failed-orders');
        }
      }
    });

    this.loadOrders();
  }

  loadOrders() {
    this.ordersService.getMyOrders().subscribe({
      next: (data) => {
        this.orders.set(data);
        this.fetchMissingImages(data);
      },
      error: (err) => console.error('Failed to load orders', err)
    });
  }

  fetchMissingImages(orders: OrderResponse[]) {
    const productIds = new Set<number>();
    orders.forEach(order => {
      order.orderItems.forEach(item => productIds.add(item.productId));
    });

    const currentMap = this.productImages();
    const missingIds = Array.from(productIds).filter(id => !currentMap.has(id));

    if (missingIds.length === 0) return;

    const requests = missingIds.map(id => 
      this.productService.getProductById(id).pipe(
        map(p => ({ id, url: p.imageUrl })),
        catchError(() => of({ id, url: this.productService.toAbsoluteImageUrl(undefined) }))
      )
    );

    forkJoin(requests).subscribe(results => {
      this.productImages.update(map => {
        const newMap = new Map(map);
        results.forEach(r => newMap.set(r.id, r.url || ''));
        return newMap;
      });
    });
  }

  setActiveTab(tab: 'orders' | 'failed-orders') {
    this.activeTab.set(tab);
  }

  toggleOrder(orderId: number) {
    this.expandedOrderIds.update(ids => {
      const newIds = new Set(ids);
      if (newIds.has(orderId)) {
        newIds.delete(orderId);
      } else {
        newIds.add(orderId);
      }
      return newIds;
    });
  }

  isExpanded(orderId: number): boolean {
    return this.expandedOrderIds().has(orderId);
  }

  getProductImage(productId: number): string {
    return this.productImages().get(productId) || this.productService.toAbsoluteImageUrl(undefined);
  }

  resolveImage(url?: string): string {
    return this.productService.toAbsoluteImageUrl(url);
  }

  getOrderStatusText(status: string): string {
    // Convert CamelCase to Spaced String (e.g. PaymentPending -> Payment Pending)
    return status.replace(/([A-Z])/g, ' $1').trim();
  }

  getStatusClass(status: string): string {
    const statusMap: { [key: string]: number } = {
      'Created': 0,
      'PaymentPending': 1,
      'PaymentApproved': 2,
      'PaymentFailed': 3,
      'Preparing': 4,
      'Shipped': 5,
      'Delivered': 6
    };
    const code = statusMap[status] ?? 0;
    return `status-${code}`;
  }

  retryOrder(order: OrderResponse) {
    if (!order.companyId) {
        this.toastService.error('Order data invalid');
        return;
    }
    
    const payload: CheckoutPayload = {
        companyId: order.companyId,
        userId: order.userId,
        items: order.orderItems.map(i => ({
            productId: i.productId,
            variantProductId: i.variantProductId,
            quantity: i.quantity
        }))
    };

    this.ordersService.checkout(payload).subscribe({
        next: (res) => {
             this.router.navigate(['/payment-waiting', res.id]);
        },
        error: (err) => {
             this.toastService.error('Failed to retry order');
        }
    });
  }
}
