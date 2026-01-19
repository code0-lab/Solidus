import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, finalize } from 'rxjs';
import { ProductService } from './product.service';
import { PaginatedResult, Product } from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class SearchService {
  private http = inject(HttpClient);
  private productService = inject(ProductService);

  private get apiUrl(): string {
    return `/api`;
  }

  classifyImage(file: File): Observable<{ clusterId: number; clusterName?: string; version: number }> {
    const formData = new FormData();
    formData.append('file', file);
    try {
      const url = URL.createObjectURL(file);
      this.productService.queryImageUrl.set(url);
    } catch {}
    return this.http.post<{ clusterId: number; clusterName?: string; version: number }>(`${this.apiUrl}/clustering/classify`, formData);
  }

  fetchProductsByCluster(clusterId: number, pageNumber: number = 1, pageSize: number = 9, companyId?: number | null, brandId?: number | null): void {
    const url = `${this.apiUrl}/products/by-cluster/${clusterId}`;
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    if (companyId) {
      params = params.set('companyId', companyId);
    }
    if (brandId) {
      params = params.set('brandId', brandId);
    }
    this.http.get<PaginatedResult<Product>>(url, { params })
      .subscribe({
        next: (data) => this.productService.applyProductPage(data),
        error: () => {
          this.productService.products.set([]);
          this.productService.totalCount.set(0);
        }
      });
  }

  searchProductsByName(query: string, pageNumber: number = 1, pageSize: number = 9, companyId?: number | null, brandId?: number | null, categoryId?: number | null): void {
    const url = `${this.apiUrl}/products/search`;
    let params = new HttpParams()
      .set('query', query)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    if (companyId) {
      params = params.set('companyId', companyId);
    }
    if (brandId) {
      params = params.set('brandId', brandId);
    }
    if (categoryId) {
      params = params.set('categoryId', categoryId);
    }
    this.productService.queryImageUrl.set(null);
    this.productService.loading.set(true);
    this.http.get<PaginatedResult<Product>>(url, { params })
      .pipe(finalize(() => this.productService.loading.set(false)))
      .subscribe({
        next: (data) => this.productService.applyProductPage(data),
        error: () => {
          this.productService.products.set([]);
          this.productService.totalCount.set(0);
        }
      });
  }
}
