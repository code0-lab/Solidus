import { Component, signal, inject, ChangeDetectionStrategy, DestroyRef, effect, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ImageCropperComponent, ImageCroppedEvent, LoadedImage } from 'ngx-image-cropper';
import { ProductService } from '../../services/product.service';
import { SearchService } from '../../services/search.service';
import { Product } from '../../models/product.model';
import { ProductListComponent } from '../../components/product-list/product-list.component';
import { ProductDetailComponent } from '../../components/product-detail/product-detail.component';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, ProductListComponent, ProductDetailComponent, ImageCropperComponent],
  templateUrl: './search.component.html',
  styleUrl: './search.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SearchComponent {
  productService = inject(ProductService);
  searchService = inject(SearchService);
  destroyRef = inject(DestroyRef);

  selectedProduct = signal<Product | null>(null);
  selectedClusterId = signal<number | null>(null);
  isClassifying = signal(false);
  classifyError = signal<string | null>(null);
  itemsPerPage = 9;

  // Cropper state
  imageFile: File | undefined = undefined;
  croppedImageBlob: Blob | null = null;
  showCropper = signal(false);
  isVisualSearchMode = signal(false);

  constructor() {
    effect(() => {
      const pendingFile = this.searchService.pendingImageFile();
      if (pendingFile) {
        untracked(() => {
          this.imageFile = pendingFile;
          this.showCropper.set(true);
          this.isVisualSearchMode.set(false);
          this.classifyError.set(null);
          this.searchService.pendingImageFile.set(null);
        });
      }
    });

    // Restore last results if empty
    effect(() => {
      // Check only once on init
      untracked(() => {
        if (this.productService.products().length === 0) {
          this.productService.loadLastResults();
        }
      });
    });
  }

  onSelectProduct(p: Product) {
    this.selectedProduct.set(p);
  }

  closeProductDetail() {
    this.selectedProduct.set(null);
  }

  closeVisualSearch() {
    this.isVisualSearchMode.set(false);
    this.productService.queryImageUrl.set(null);
    this.selectedClusterId.set(null);
    // Do NOT clear products, so they persist in the standard view
    // this.productService.products.set([]); 
  }

  imageCropped(event: ImageCroppedEvent) {
    this.croppedImageBlob = event.blob || null;
  }

  imageLoaded(image: LoadedImage) {
      // Image loaded
  }

  cropperReady() {
      // Cropper ready
  }

  loadImageFailed() {
      this.classifyError.set('Image load failed');
      this.showCropper.set(false);
  }

  cancelCrop() {
      this.showCropper.set(false);
      this.imageFile = undefined;
      this.croppedImageBlob = null;
  }

  async confirmCrop() {
    if (!this.croppedImageBlob) return;
    
    this.showCropper.set(false);
    this.isVisualSearchMode.set(true); // Enable Visual Search Mode
    this.isClassifying.set(true);
    
    // Create File from Blob
    const file = new File([this.croppedImageBlob], "cropped.png", { type: "image/png" });

    try {
      // Pass true to skip processing because we want to send the cropped image (High Res) 
      // to Python for rembg. Python will handle resizing after rembg.
      this.searchService.classifyImage(file, true)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => {
            this.selectedClusterId.set(res.clusterId);
            this.searchService.fetchProductsByCluster(
              res.clusterId,
              1,
              this.itemsPerPage,
              this.productService.selectedCompany(),
              null,
              res.similarProductIds
            );
            this.isClassifying.set(false);
          },
          error: () => {
            this.classifyError.set('Classification failed.');
            this.isClassifying.set(false);
          }
        });
    } catch (err) {
      this.classifyError.set('Image processing failed.');
      this.isClassifying.set(false);
    }
  }
}
