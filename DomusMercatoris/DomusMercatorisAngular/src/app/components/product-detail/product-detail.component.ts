import { Component, Input, Output, EventEmitter, inject, signal, OnChanges, SimpleChanges, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Product, VariantProduct } from '../../models/product.model';
import { CommentService } from '../../services/comment.service';
import { ProductDetailInfoComponent } from './product-detail-info/product-detail-info';
import { ProductCommentsComponent } from './product-comments/product-comments';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, ProductDetailInfoComponent, ProductCommentsComponent],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.css'
})
export class ProductDetailComponent implements OnChanges {
  @Input({ required: true }) product!: Product;
  @Output() onClose = new EventEmitter<void>();

  commentService = inject(CommentService);

  activeTab: 'details' | 'comments' = 'details';
  currentImageIndex = signal(0);
  viewerOpen = signal(false);
  viewerImageUrl = signal<string | null>(null);
  selectedVariant = signal<VariantProduct | null>(null);

  allImages = computed(() => {
    const images: { url: string; variant?: VariantProduct }[] = [];
    
    // Main product images
    if (this.product.imageUrls) {
      this.product.imageUrls.forEach(url => images.push({ url }));
    } else if (this.product.imageUrl) {
      images.push({ url: this.product.imageUrl });
    }

    // Associate variants with existing images
    if (this.product.variants) {
      this.product.variants.forEach(v => {
        if (v.coverImage) {
          // Check if this image already exists in the list
          const existingImg = images.find(img => img.url === v.coverImage);
          if (existingImg) {
            // Associate variant with existing image
            existingImg.variant = v;
          } else {
            // Add as new image if not found (fallback)
            images.push({ url: v.coverImage, variant: v });
          }
        }
      });
    }

    return images;
  });
  
  ngOnChanges(changes: SimpleChanges) {
    if (changes['product'] && this.product) {
      this.commentService.fetchComments(this.product.id);
      this.currentImageIndex.set(0);
      this.selectedVariant.set(null);
    }
  }

  selectVariant(variant: VariantProduct) {
    if (this.selectedVariant()?.id === variant.id) {
      this.selectedVariant.set(null);
      // Reset to first image (usually main product image) if deselected
      this.currentImageIndex.set(0);
    } else {
      this.selectedVariant.set(variant);
      // Find image index for this variant by URL first, then by variant ID
      const images = this.allImages();
      let idx = images.findIndex(img => img.url === variant.coverImage);
      
      // Fallback to searching by variant ID if URL match fails (e.g. slight url differences)
      if (idx === -1) {
        idx = images.findIndex(img => img.variant?.id === variant.id);
      }

      if (idx >= 0) {
        this.currentImageIndex.set(idx);
      }
    }
  }

  close() {
    this.onClose.emit();
  }

  nextImage() {
    const images = this.allImages();
    if (images.length === 0) return;
    const current = this.currentImageIndex();
    const next = (current + 1) % images.length;
    this.setImage(next);
  }

  prevImage() {
    const images = this.allImages();
    if (images.length === 0) return;
    const current = this.currentImageIndex();
    const prev = (current - 1 + images.length) % images.length;
    this.setImage(prev);
  }

  setImage(index: number) {
    this.currentImageIndex.set(index);
    const img = this.allImages()[index];
    if (img && img.variant) {
      this.selectedVariant.set(img.variant);
    } else {
      // If we switch to a main product image, should we deselect variant?
      // User says: "varyanta ait kapak fotoğrafına geçilince varyantın bilgileri görüntülenir"
      // This implies if we move AWAY from variant photo, we should probably go back to product info.
      this.selectedVariant.set(null);
    }
  }

  openImageViewer(url: string | undefined) {
    if (!url) return;
    this.viewerImageUrl.set(url);
    this.viewerOpen.set(true);
  }

  closeImageViewer() {
    this.viewerOpen.set(false);
    this.viewerImageUrl.set(null);
  }
}
