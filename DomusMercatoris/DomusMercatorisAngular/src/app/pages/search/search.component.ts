import { Component, signal, inject } from '@angular/core';
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
  styleUrl: './search.component.css'
})
export class SearchComponent {
  productService = inject(ProductService);
  searchService = inject(SearchService);

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

  onImageSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    this.isClassifying.set(true);
    this.classifyError.set(null);
    this.searchService.classifyImage(file).subscribe({
      next: (res) => {
        this.selectedClusterId.set(res.clusterId);
        this.searchService.fetchProductsByCluster(res.clusterId, 1, this.itemsPerPage, this.productService.selectedCompany());
        this.isClassifying.set(false);
      },
      error: () => {
        this.classifyError.set('Classification failed.');
        this.isClassifying.set(false);
      }
    });
  }
}
