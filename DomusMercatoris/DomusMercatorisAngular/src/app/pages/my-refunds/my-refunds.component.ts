import { Component, inject, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RefundsService, RefundResponse } from '../../services/refunds.service';
import { ProductService } from '../../services/product.service';

@Component({
  selector: 'app-my-refunds',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-refunds.component.html',
  styleUrl: './my-refunds.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyRefundsComponent implements OnInit {
  refundsService = inject(RefundsService);
  productService = inject(ProductService);

  refunds = signal<RefundResponse[]>([]);
  isLoading = signal(false);

  ngOnInit() {
    this.loadRefunds();
  }

  loadRefunds() {
    this.isLoading.set(true);
    this.refundsService.getMyRefunds().subscribe({
      next: (data) => {
        this.refunds.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Approved': return 'status-approved';
      case 'Rejected': return 'status-rejected';
      default: return 'status-pending';
    }
  }

  getAbsoluteImageUrl(url?: string): string {
    return this.productService.toAbsoluteImageUrl(url);
  }
}
