import { Component, Input, Output, EventEmitter, inject, OnChanges, SimpleChanges, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ScrollingModule, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { Product } from '../../models/product.model';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, ScrollingModule],
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
  itemHeight = 320;
  private resizeObserver: ResizeObserver | null = null;
  
  @ViewChild(CdkVirtualScrollViewport) viewport!: CdkVirtualScrollViewport;
  @ViewChild('container') container!: ElementRef;

  constructor() {}

  ngAfterViewInit() {
    this.resizeObserver = new ResizeObserver(entries => {
      for (const entry of entries) {
        this.updateCols(entry.contentRect.width);
      }
    });
    this.resizeObserver.observe(this.container.nativeElement);
  }

  ngOnDestroy() {
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
  }

  updateCols(width: number) {
    let newCols = 1;
    if (width >= 1024) {
      newCols = 4;
      this.itemHeight = 320;
    } else if (width >= 768) {
      newCols = 3;
      this.itemHeight = 320;
    } else if (width >= 400) { // Changed from 550 to 400 to allow 2 cols on most phones
      newCols = 2;
      this.itemHeight = 290; // Card 270 + Padding 20
    } else {
      newCols = 1;
      this.itemHeight = 350; // Card 330 + Padding 20
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

  onScrollIndexChanged() {
    if (!this.viewport) return;
    const end = this.viewport.getRenderedRange().end;
    const total = this.rows.length;
    // If we are close to the end (e.g., within 5 rows)
    if (end >= total - 3) {
      this.loadMore.emit();
    }
  }

  addToCart(event: Event, product: Product) {
    event.stopPropagation();
    this.cartService.add(product);
  }
}
