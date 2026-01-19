import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Product, Category, Company, PaginatedResult } from '../models/product.model';
import { Observable, tap, finalize } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);
  
  private get apiUrl(): string {
    return `/api`;
  }

  products = signal<Product[]>([]);
  loading = signal<boolean>(false);
  totalCount = signal<number>(0);
  categories = signal<Category[]>([]);
  companies = signal<Company[]>([]);
  selectedCategory = signal<number | null>(null);
  selectedCompany = signal<number | null>(null);
  queryImageUrl = signal<string | null>(null);

  classifyImage(file: File): Observable<{ clusterId: number; clusterName?: string; version: number }> {
    const formData = new FormData();
    formData.append('file', file);
    try {
      const url = URL.createObjectURL(file);
      this.queryImageUrl.set(url);
    } catch {}
    return this.http.post<{ clusterId: number; clusterName?: string; version: number }>(`${this.apiUrl}/clustering/classify`, formData);
  }

  fetchProductsByCluster(clusterId: number, pageNumber: number = 1, pageSize: number = 9, companyId?: number | null): void {
    const url = `${this.apiUrl}/products/by-cluster/${clusterId}`;
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    if (companyId) {
      params = params.set('companyId', companyId);
    }
    this.http.get<PaginatedResult<Product>>(url, { params })
      .subscribe({
        next: (data) => {
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
          this.products.set([]);
          this.totalCount.set(0);
        }
      });
  }

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

  fetchProducts(categoryId?: number | null, pageNumber: number = 1, pageSize: number = 9, companyId?: number | null, append: boolean = false): void {
    const url = categoryId 
      ? `${this.apiUrl}/products/by-category/${categoryId}`
      : `${this.apiUrl}/products`;

    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    if (companyId) {
      params = params.set('companyId', companyId);
    }

    this.loading.set(true);
    this.http.get<PaginatedResult<Product>>(url, { params })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (data) => {
          this.applyProductPage(data, append);
        },
        error: () => {
          this.products.set([]);
          this.totalCount.set(0);
        }
      });
  }
 
  applyProductPage(data: PaginatedResult<Product>, append: boolean = false) {
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
    if (append) {
      this.products.update(current => [...current, ...processed]);
    } else {
      this.products.set(processed);
    }
    this.totalCount.set(data.totalCount);
  }

  searchProductsByName(query: string, pageNumber: number = 1, pageSize: number = 9, companyId?: number | null): void {
    const url = `${this.apiUrl}/products/search`;
    let params = new HttpParams()
      .set('query', query)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    if (companyId) {
      params = params.set('companyId', companyId);
    }
    this.queryImageUrl.set(null);
    this.loading.set(true);
    this.http.get<PaginatedResult<Product>>(url, { params })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (data) => {
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
          this.products.set([]);
          this.totalCount.set(0);
        }
      });
  }

  private toAbsoluteImageUrl(img?: string): string {
    const fallback = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNjAwIiBoZWlnaHQ9IjQwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZWVlIi8+PHRleHQgeD0iNTAlIiB5PSI1MCUiIGZvbnQtZmFtaWx5PSJBcmlhbCIgZm9udC1zaXplPSIyNCIgZmlsbD0iIzU1NSIgZG9taW5hbnQtYmFzZWxpbmU9Im1pZGRsZSIgdGV4dC1hbmNob3I9Im1pZGRsZSI+UHJvZHVjdDwvdGV4dD48L3N2Zz4=';
    if (!img || img.length === 0) return fallback;
    try {
      const u = new URL(img);
      // If localhost, strip domain to use proxy
      if (u.hostname === 'localhost' || u.hostname === '127.0.0.1') {
         return u.pathname + u.search;
      }
      return u.toString();
    } catch {
      // Backend maps /uploads directly from MVC project
      
      // If path already starts with http, return it
      if (img.startsWith('http')) return img;

      // Ensure we don't have double slashes
      const cleanPath = img.startsWith('/') ? img : `/${img}`;
      
      return cleanPath;
    }
  }

  private getRandomColor() {
    const palette = ['#e8f5ff', '#fff1f1', '#e9fff4', '#f6f4ff'];
    return palette[Math.floor(Math.random() * palette.length)];
  }

}
