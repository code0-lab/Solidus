import { Component, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductListComponent } from '../../components/product-list/product-list.component';
import { ProductDetailComponent } from '../../components/product-detail/product-detail.component';
import { ProductService } from '../../services/product.service';
import { Product } from '../../models/product.model';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule, 
    ProductListComponent, 
    ProductDetailComponent
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {
  productService = inject(ProductService);

  isFilterOpen = signal(false);
  selectedProduct = signal<Product | null>(null);

  // Pagination
  currentPage = signal(1);
  itemsPerPage = 9;

  paginatedProducts = computed(() => {
    return this.productService.products();
  });

  totalPages = computed(() => {
    return Math.ceil(this.productService.totalCount() / this.itemsPerPage);
  });

  pages = computed(() => {
    const total = this.totalPages();
    return Array.from({ length: total }, (_, i) => i + 1);
  });

  constructor() {
    // If data is already loaded (singleton service), we might not need to fetch again, 
    // but fetching ensures freshness.
    this.productService.fetchCategories();
    this.productService.fetchCompanies();
    this.productService.fetchProducts(null, 1, this.itemsPerPage, null);
  }

  toggleFilter() {
    this.isFilterOpen.set(!this.isFilterOpen());
  }

  selectCategory(id: number | null) {
    this.productService.selectedCategory.set(id);
    this.productService.fetchProducts(id, 1, this.itemsPerPage, this.productService.selectedCompany());
    this.isFilterOpen.set(false);
    this.currentPage.set(1); // Reset to first page when filtering
  }

  selectCompany(id: number | null) {
    this.productService.selectedCompany.set(id);
    this.productService.fetchProducts(this.productService.selectedCategory(), 1, this.itemsPerPage, id);
    this.isFilterOpen.set(false);
    this.currentPage.set(1);
  }

  onSelectProduct(product: Product) {
    this.selectedProduct.set(product);
  }

  closeProductDetail() {
    this.selectedProduct.set(null);
  }

  changePage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.productService.fetchProducts(
        this.productService.selectedCategory(), 
        page, 
        this.itemsPerPage,
        this.productService.selectedCompany()
      );
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }
}
