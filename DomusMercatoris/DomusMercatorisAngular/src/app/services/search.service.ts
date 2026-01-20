import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, finalize } from 'rxjs';
import { ProductService } from './product.service';
import { PaginatedResult, Product } from '../models/product.model';
import { environment } from '../../environments/environment';

declare module 'heic2any';

@Injectable({ providedIn: 'root' })
export class SearchService {
  private http = inject(HttpClient);
  private productService = inject(ProductService);

  private get apiUrl(): string {
    return environment.apiUrl;
  }

  validateFile(file: File): { valid: boolean; error?: string } {
    const MAX_SIZE_BYTES = 17 * 1024 * 1024;
    const isImageMime = file.type && file.type.startsWith('image/');
    const validExtensions = /\.(jpg|jpeg|png|webp|gif|bmp|heic|heif)$/i;
    const hasValidExtension = validExtensions.test(file.name);

    if (!isImageMime && !hasValidExtension) {
       if (file.type && !file.type.startsWith('image/')) {
         return { valid: false, error: 'Only images are accepted.' };
       }
    }

    if (file.size > MAX_SIZE_BYTES) {
      return { valid: false, error: 'Image exceeds 17MB limit.' };
    }
    return { valid: true };
  }

  async processImage(file: File): Promise<File> {
    if (file.name.toLowerCase().endsWith('.heic') || file.name.toLowerCase().endsWith('.heif')) {
      try {
        const heic2anyModule = await import('heic2any');
        // @ts-ignore
        const heic2any = heic2anyModule.default || heic2anyModule;
        
        const convertedBlob = await heic2any({
          blob: file,
          toType: 'image/jpeg',
          quality: 0.8
        });
        
        const blob = Array.isArray(convertedBlob) ? convertedBlob[0] : convertedBlob;
        return new File([blob as Blob], file.name.replace(/\.hei[cf]$/i, '.jpg'), { type: 'image/jpeg' });
      } catch (err) {
        console.error('HEIC conversion failed:', err);
        throw new Error('Could not process HEIC image.');
      }
    }
    return file;
  }

  classifyImage(file: File): Observable<{ clusterId: number; clusterName?: string; version: number; similarProductIds?: number[] }> {
    const formData = new FormData();
    formData.append('file', file);
    try {
      const url = URL.createObjectURL(file);
      this.productService.queryImageUrl.set(url);
    } catch {}
    return this.http.post<{ clusterId: number; clusterName?: string; version: number; similarProductIds?: number[] }>(`${this.apiUrl}/clustering/classify`, formData);
  }

  fetchProductsByCluster(clusterId: number, pageNumber: number = 1, pageSize: number = 9, companyId?: number | null, brandId?: number | null, prioritizedIds?: number[] | null): void {
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
    if (prioritizedIds && prioritizedIds.length > 0) {
      prioritizedIds.forEach(id => {
        params = params.append('prioritizedIds', id);
      });
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
