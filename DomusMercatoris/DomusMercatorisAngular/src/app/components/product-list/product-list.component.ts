import { Component, Input, Output, EventEmitter, inject, OnChanges, SimpleChanges, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Product } from '../../models/product.model';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css'
})
export class ProductListComponent implements OnChanges, AfterViewInit, OnDestroy {
  @Input({ required: true }) products: Product[] = [];
  @Output() selectProduct = new EventEmitter<Product>();
  @Output() loadMore = new EventEmitter<void>();

  cartService = inject(CartService);
  
  rows: Product[][] = [];
  cols = 3;
  private resizeObserver: ResizeObserver | null = null;
  private intersectionObserver: IntersectionObserver | null = null;
  
  @ViewChild('container') container!: ElementRef;
  @ViewChild('sentinel') sentinel!: ElementRef;

  constructor() {}

  ngAfterViewInit() {
    this.resizeObserver = new ResizeObserver(entries => {
      for (const entry of entries) {
        this.updateCols(entry.contentRect.width);
      }
    });
    this.resizeObserver.observe(this.container.nativeElement);

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
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
    if (this.intersectionObserver) {
      this.intersectionObserver.disconnect();
    }
  }

  updateCols(width: number) {
    let newCols = 1;
    if (width >= 1024) {
      newCols = 4;
    } else if (width >= 768) {
      newCols = 3;
    } else if (width >= 400) {
      newCols = 2;
    } else {
      newCols = 1;
    }
    
    if (newCols !== this.cols) {
      this.cols = newCols;
      this.chunkProducts();
    }
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['products']) {
      this.chunkProducts();
    }
  }

  chunkProducts() {
    if (!this.products) return;
    this.rows = [];
    for (let i = 0; i < this.products.length; i += this.cols) {
      this.rows.push(this.products.slice(i, i + this.cols));
    }
  }

  addToCart(event: Event, product: Product) {
    event.stopPropagation();
    this.cartService.add(product);
  }
}
