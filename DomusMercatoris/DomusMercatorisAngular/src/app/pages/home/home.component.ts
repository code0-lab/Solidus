import { Component, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProductListComponent } from '../../components/product-list/product-list.component';
import { ProductDetailComponent } from '../../components/product-detail/product-detail.component';
import { ProductService } from '../../services/product.service';
import { Product } from '../../models/product.model';
import { SliderComponent } from '../../components/slider/slider.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule, 
    ProductListComponent, 
    ProductDetailComponent,
    SliderComponent
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
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  selectCompany(id: number | null) {
    this.productService.selectedCompany.set(id);
    this.productService.fetchProducts(this.productService.selectedCategory(), 1, this.itemsPerPage, id);
    this.isFilterOpen.set(false);
    this.currentPage.set(1);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  onSelectProduct(product: Product) {
    this.selectedProduct.set(product);
  }

  closeProductDetail() {
    this.selectedProduct.set(null);
  }

  loadMore() {
    if (this.productService.loading()) return;
    if (this.currentPage() >= this.totalPages()) return;
    
    const nextPage = this.currentPage() + 1;
    this.currentPage.set(nextPage);
    this.productService.fetchProducts(
      this.productService.selectedCategory(), 
      nextPage, 
      this.itemsPerPage,
      this.productService.selectedCompany(),
      true
    );
  }
}
