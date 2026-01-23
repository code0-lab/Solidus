import { Component, signal, inject, ChangeDetectionStrategy, DestroyRef, ViewChild, ElementRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../services/product.service';
import { SearchService } from '../../services/search.service';
import { Product } from '../../models/product.model';
import { ProductListComponent } from '../../components/product-list/product-list.component';
import { ProductDetailComponent } from '../../components/product-detail/product-detail.component';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, ProductListComponent, ProductDetailComponent],
  templateUrl: './search.component.html',
  styleUrl: './search.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SearchComponent {
  productService = inject(ProductService);
  searchService = inject(SearchService);
  destroyRef = inject(DestroyRef);

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  selectedProduct = signal<Product | null>(null);
  selectedClusterId = signal<number | null>(null);
  isClassifying = signal(false);
  classifyError = signal<string | null>(null);
  itemsPerPage = 9;

  onSelectProduct(p: Product) {
    this.selectedProduct.set(p);
  }

  closeProductDetail() {
    this.selectedProduct.set(null);
  }

  openSelectDialog() {
    if (this.fileInput) {
      this.fileInput.nativeElement.click();
    }
  }

  async onImageSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    const validation = this.searchService.validateFile(file);
    if (!validation.valid) {
      this.classifyError.set(validation.error || 'Invalid file');
      return;
    }

    this.isClassifying.set(true);
    this.classifyError.set(null);

    try {
      const processedFile = await this.searchService.processImage(file);

      this.searchService.classifyImage(processedFile)
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
