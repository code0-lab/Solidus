import { Component, Input, Output, EventEmitter, inject, signal, OnChanges, SimpleChanges, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Product, VariantProduct } from '../../models/product.model';
import { CommentService } from '../../services/comment.service';
import { AuthService } from '../../services/auth.service';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.css'
})
export class ProductDetailComponent implements OnChanges {
  @Input({ required: true }) product!: Product;
  @Output() onClose = new EventEmitter<void>();

  commentService = inject(CommentService);
  authService = inject(AuthService);
  cartService = inject(CartService);

  activeTab: 'details' | 'comments' = 'details';
  newCommentText = signal('');
  currentImageIndex = signal(0);
  viewerOpen = signal(false);
  viewerImageUrl = signal<string | null>(null);
  selectedVariant = signal<VariantProduct | null>(null);

  currentPrice = computed(() => {
    const variant = this.selectedVariant();
    if (variant) {
      return variant.price;
    }
    return this.product.price;
  });

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

  postComment() {
    const text = this.newCommentText();
    if (!text.trim()) return;

    if (!this.authService.currentUser()) {
      alert('You must be logged in to post a comment.');
      this.authService.toggleLogin();
      return;
    }

    this.commentService.createComment({ productId: this.product.id, text }).subscribe({
      next: () => {
        this.newCommentText.set('');
        alert('Your comment has been added!');
      },
      error: (err) => {
        console.error(err);
        alert('Error adding comment.');
      }
    });
  }

  addToCart() {
    const variant = this.selectedVariant();
    // If product has variants but none selected, maybe we should force selection?
    // User requirement: "Angular tarafı sadece product id yi ürün detaylarında görebiliyor... oysa müşteri gri olanı almak istiyor olabilir"
    // So if user selects a variant, we add variant. If not, we add product (default).
    // However, if the product *only* exists as variants (e.g. T-shirt sizes), we might want to force it.
    // For now, let's allow adding base product if no variant selected, but prefer variant if selected.
    
    if (this.product.variants && this.product.variants.length > 0 && !variant) {
        // Optional: Prompt user or just add base product. 
        // Given the requirement "bir product un en fazla 5 varyantı olabilir", it implies base product + variants.
        // Let's assume user can buy base product OR variant.
    }

    this.cartService.add(this.product, variant || undefined);
    alert('Added to cart.');
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
