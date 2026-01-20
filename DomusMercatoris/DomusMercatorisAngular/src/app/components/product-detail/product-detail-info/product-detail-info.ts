import { Component, Input, Output, EventEmitter, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Product, VariantProduct } from '../../../models/product.model';
import { CartService } from '../../../services/cart.service';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-product-detail-info',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './product-detail-info.html',
  styleUrl: './product-detail-info.css'
})
export class ProductDetailInfoComponent {
  @Input({ required: true }) product!: Product;
  @Input() selectedVariant: VariantProduct | null = null;
  @Output() variantSelected = new EventEmitter<VariantProduct>();
  
  private sanitizer = inject(DomSanitizer);
  private cartService = inject(CartService);
  private toastService = inject(ToastService);
  isDescriptionExpanded = signal(false);

  currentPrice = computed(() => {
    if (this.selectedVariant) {
      return this.selectedVariant.price;
    }
    return this.product.price;
  });

  safeDescription = computed<SafeHtml>(() => {
    const html = this.product?.description || 'No description available for this product.';
    return this.sanitizer.bypassSecurityTrustHtml(html);
  });

  onSelectVariant(variant: VariantProduct) {
    this.variantSelected.emit(variant);
  }

  toggleDescription() {
    this.isDescriptionExpanded.update(v => !v);
  }

  onAddToCart() {
    this.cartService.add(this.product, this.selectedVariant || undefined);
    this.toastService.success('Added to cart.');
  }
}
