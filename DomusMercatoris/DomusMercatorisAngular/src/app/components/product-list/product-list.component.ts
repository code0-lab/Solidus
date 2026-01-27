import { Component, Input, Output, EventEmitter, inject, ViewChild, ElementRef, AfterViewInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Product } from '../../models/product.model';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductListComponent implements AfterViewInit, OnDestroy {
  @Input({ required: true }) products: Product[] = [];
  @Output() selectProduct = new EventEmitter<Product>();
  @Output() loadMore = new EventEmitter<void>();

  cartService = inject(CartService);
  
  private intersectionObserver: IntersectionObserver | null = null;
  
  @ViewChild('sentinel') sentinel!: ElementRef;

  constructor() {}

  ngAfterViewInit() {
    this.intersectionObserver = new IntersectionObserver(entries => {
      if (entries[0].isIntersecting) {
        this.loadMore.emit();
      }
    }, { threshold: 0.1 }); // Trigger when 10% of sentinel is visible
    
    if (this.sentinel) {
      this.intersectionObserver.observe(this.sentinel.nativeElement);
    }
  }

  ngOnDestroy() {
    if (this.intersectionObserver) {
      this.intersectionObserver.disconnect();
    }
  }

  addToCart(event: Event, product: Product) {
    event.stopPropagation();
    this.cartService.add(product);
  }
}
