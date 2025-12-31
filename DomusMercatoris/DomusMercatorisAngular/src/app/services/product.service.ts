import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Product, Category, Company, PaginatedResult } from '../models/product.model';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5280/api';

  products = signal<Product[]>([]);
  totalCount = signal<number>(0);
  categories = signal<Category[]>([]);
  companies = signal<Company[]>([]);
  selectedCategory = signal<number | null>(null);
  selectedCompany = signal<number | null>(null);

  fetchCategories(): void {
    this.http.get<Category[]>(`${this.apiUrl}/categories`)
      .subscribe({
        next: (data) => this.categories.set(data),
        error: () => console.error('Failed to fetch categories')
      });
  }

  fetchCompanies(): void {
    this.http.get<Company[]>(`${this.apiUrl}/companies`)
      .subscribe({
        next: (data) => this.companies.set(data),
        error: () => console.error('Failed to fetch companies')
      });
  }

  fetchProducts(categoryId?: number | null, pageNumber: number = 1, pageSize: number = 9, companyId?: number | null): void {
    const url = categoryId 
      ? `${this.apiUrl}/products/by-category/${categoryId}`
      : `${this.apiUrl}/products`;

    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    if (companyId) {
      params = params.set('companyId', companyId);
    }

    this.http.get<PaginatedResult<Product>>(url, { params })
      .subscribe({
        next: (data) => {
          // Process products (e.g., set default images or formatting)
          const processed = data.items.map(p => ({
            ...p,
            imageUrl: this.toAbsoluteImageUrl(p.images && p.images.length > 0 ? p.images[0] : undefined),
            imageUrls: p.images && p.images.length > 0 
              ? p.images.map(img => this.toAbsoluteImageUrl(img)) 
              : [this.toAbsoluteImageUrl(undefined)],
            priceText: new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(p.price),
            bg: this.getRandomColor(),
            variants: p.variants ? p.variants.map(v => ({
              ...v,
              id: v.id,
              price: v.price,
              isCustomizable: v.isCustomizable,
              color: v.color,
              coverImage: this.toAbsoluteImageUrl(v.coverImage)
            })) : []
          }));
          this.products.set(processed);
          this.totalCount.set(data.totalCount);
        },
        error: () => {
          // If API fails, fall back to sample products (simulating a page)
          const sample = this.getSampleProducts();
          this.products.set(sample);
          this.totalCount.set(sample.length); // Assume single page for sample
        }
      });
  }

  private toAbsoluteImageUrl(img?: string): string {
    const fallback = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNjAwIiBoZWlnaHQ9IjQwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZWVlIi8+PHRleHQgeD0iNTAlIiB5PSI1MCUiIGZvbnQtZmFtaWx5PSJBcmlhbCIgZm9udC1zaXplPSIyNCIgZmlsbD0iIzU1NSIgZG9taW5hbnQtYmFzZWxpbmU9Im1pZGRsZSIgdGV4dC1hbmNob3I9Im1pZGRsZSI+UHJvZHVjdDwvdGV4dD48L3N2Zz4=';
    if (!img || img.length === 0) return fallback;
    try {
      const u = new URL(img);
      return u.toString();
    } catch {
      // Backend maps /uploads directly from MVC project
      const baseUrl = 'http://localhost:5280';
      
      // If path already starts with http, return it
      if (img.startsWith('http')) return img;

      // Ensure we don't have double slashes
      const cleanPath = img.startsWith('/') ? img : `/${img}`;
      
      return `${baseUrl}${cleanPath}`;
    }
  }

  private getRandomColor() {
    const palette = ['#e8f5ff', '#fff1f1', '#e9fff4', '#f6f4ff'];
    return palette[Math.floor(Math.random() * palette.length)];
  }

  private getSampleProducts(): Product[] {
    // Return dummy data if API fails or is empty
    const baseProducts = [
        { id: 1, name: 'Sample Product A', sku: 'SAMPLE-A', price: 19.9, images: [] },
        { id: 2, name: 'Sample Product B', sku: 'SAMPLE-B', price: 29.9, images: [] },
        { id: 3, name: 'Sample Product C', sku: 'SAMPLE-C', price: 39.9, images: [] },
        { id: 4, name: 'Sample Product D', sku: 'SAMPLE-D', price: 49.9, images: [] }
    ];

    // Generate more products for pagination testing (3x3 = 9 per page)
    const products: any[] = [];
    for (let i = 0; i < 3; i++) {
      baseProducts.forEach(p => {
        products.push({
          ...p,
          id: products.length + 1,
          name: `${p.name} (${products.length + 1})`,
          sku: `${p.sku}-${products.length + 1}`
        });
      });
    }

    return products.map((p) => ({
        ...p,
        priceText: new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(p.price),
        imageUrl: this.toAbsoluteImageUrl(undefined),
        bg: this.getRandomColor(),
      }));
  }
}
