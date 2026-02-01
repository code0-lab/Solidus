import { Component, inject, OnInit, ChangeDetectionStrategy, signal, computed, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OrdersService, OrderResponse, CheckoutPayload } from '../../services/orders.service';
import { RefundsService } from '../../services/refunds.service';
import { ToastService } from '../../services/toast.service';
import { AuthService } from '../../services/auth.service';
import { ProductService } from '../../services/product.service';
import { forkJoin, of } from 'rxjs';
import { map, catchError, finalize } from 'rxjs/operators';

@Component({
  selector: 'app-my-orders',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './my-orders.component.html',
  styleUrl: './my-orders.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyOrdersComponent implements OnInit {
  ordersService = inject(OrdersService);
  refundsService = inject(RefundsService);
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

  // Pagination
  currentPage = signal(1);
  pageSize = signal(10);
  totalItems = signal(0);
  totalPages = signal(0);

  // Refund Modal State
  refundModalOpen = signal(false);
  refundItem = signal<{ id: number; name: string; maxQty: number } | null>(null);
  refundReason = signal('');
  refundQuantity = signal(1);
  
  // Loading State
  isLoading = signal(false);
  
  // Since backend filters by tab, orders() contains the correct list for the current tab
  currentTabOrders = computed(() => this.orders());

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
           // Only update if different to avoid loop if we navigate programmatically
           if (this.activeTab() !== tab) {
               this.activeTab.set(tab as 'orders' | 'failed-orders');
               this.currentPage.set(1); // Reset page on tab change
               this.loadOrders();
           }
        }
      }
    });

    this.loadOrders();
  }

  setActiveTab(tab: 'orders' | 'failed-orders') {
      if (this.activeTab() === tab) return;
      this.activeTab.set(tab);
      this.currentPage.set(1);
      this.orders.set([]); // Clear data to prevent flash of incorrect content
      this.loadOrders();
      
      // Optional: Update URL without reload
      const url = this.router.createUrlTree([], { relativeTo: this.route, queryParams: { tab } }).toString();
      this.router.navigateByUrl(url);
  }

  loadOrders() {
    this.isLoading.set(true);
    
    const request$ = this.activeTab() === 'orders' 
      ? this.ordersService.getSuccessfulOrders(this.currentPage(), this.pageSize())
      : this.ordersService.getFailedOrders(this.currentPage(), this.pageSize());

    request$.pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe((data) => {
      this.orders.set(data.items);
      this.totalItems.set(data.totalCount);
      this.totalPages.set(data.totalPages);
      this.fetchMissingImages(data.items);
    });
  }

  onPageChange(page: number) {
      if (page < 1 || page > this.totalPages()) return;
      this.currentPage.set(page);
      this.loadOrders();
      window.scrollTo({ top: 0, behavior: 'smooth' });
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

  getProgressPercentage(status: string): number {
    const progressMap: { [key: string]: number } = {
      'Created': 5,
      'PaymentPending': 10,
      'PaymentApproved': 25,
      'PaymentFailed': 0,
      'Preparing': 50,
      'Shipped': 75,
      'Delivered': 100
    };
    return progressMap[status] ?? 0;
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

    this.ordersService.checkout(payload).subscribe((res) => {
        this.router.navigate(['/payment-waiting', res.id]);
    });
  }

  // Refund Logic
  handleRefundRequest(order: OrderResponse, item: any) {
    if (order.status === 'Shipped') {
      this.toastService.show('Order cannot be canceled during shipping process', 'error');
      return;
    }
    
    this.openRefundModal(item);
  }

  openRefundModal(item: any) {
    this.refundItem.set({
      id: item.id, // OrderItem Id
      name: item.productName,
      maxQty: item.quantity
    });
    this.refundQuantity.set(1);
    this.refundReason.set('');
    this.refundModalOpen.set(true);
  }

  closeRefundModal() {
    this.refundModalOpen.set(false);
    this.refundItem.set(null);
  }

  submitRefundRequest() {
    const item = this.refundItem();
    if (!item) return;

    if (this.refundQuantity() < 1 || this.refundQuantity() > item.maxQty) {
      this.toastService.show('Invalid quantity.', 'error');
      return;
    }
    if (!this.refundReason().trim()) {
      this.toastService.show('Please provide a reason.', 'error');
      return;
    }

    this.refundsService.createRefundRequest({
      orderItemId: item.id,
      quantity: this.refundQuantity(),
      reason: this.refundReason()
    }).subscribe(() => {
        this.toastService.show('Refund request submitted successfully.', 'success');
        this.closeRefundModal();
    });
  }
}
