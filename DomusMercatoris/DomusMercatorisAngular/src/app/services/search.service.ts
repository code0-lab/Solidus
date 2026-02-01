import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, finalize, from, switchMap } from 'rxjs';
import { ProductService } from './product.service';
import { PaginatedResult, Product } from '../models/product.model';
import { environment } from '../../environments/environment';

import imageCompression from 'browser-image-compression';

// ... imports remain the same ...

@Injectable({ providedIn: 'root' })
export class SearchService {
  private http = inject(HttpClient);
  private productService = inject(ProductService);
  
  // Pending file from other components (e.g. Navbar/SearchBar) to be handled by SearchComponent
  pendingImageFile = signal<File | null>(null);

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
    console.log('[SearchService] processImage using browser-image-compression:', file.name);

    const options = {
      maxSizeMB: 1, // Reasonable limit
      maxWidthOrHeight: 512, // User requested 512px
      useWebWorker: true,
      fileType: 'image/jpeg',
      initialQuality: 0.85
    };

    try {
      const compressedFile = await imageCompression(file, options);
      console.log('[SearchService] Compression success:', compressedFile.name, compressedFile.size);

      // Ensure the name ends with .jpg
      const newFileName = file.name.replace(/\.[^/.]+$/, "") + ".jpg";
      return new File([compressedFile], newFileName, { type: 'image/jpeg' });
    } catch (error) {
      console.warn('[SearchService] browser-image-compression failed:', error);
      // Fallback: Return original file if compression fails (e.g. strict HEIC without lib)
      return file;
    }
  }

  async processImageForResNet(file: File): Promise<File> {
    console.log('[SearchService] Processing image for ResNet (224x224, RGB)...');
    return new Promise((resolve) => {
      const img = new Image();
      const reader = new FileReader();

      reader.onload = (e: any) => {
        img.src = e.target.result;
        img.onload = () => {
          try {
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            if (!ctx) {
              console.warn('[SearchService] Canvas context not available');
              resolve(file);
              return;
            }

            const TARGET_SIZE = 224;
            canvas.width = TARGET_SIZE;
            canvas.height = TARGET_SIZE;

            // 1. Fill background with white (handles transparency)
            ctx.fillStyle = '#FFFFFF';
            ctx.fillRect(0, 0, TARGET_SIZE, TARGET_SIZE);

            // 2. Calculate aspect-ratio preserving dimensions
            // We want to fit the image INSIDE 224x224 without cropping, adding whitespace (padding) if needed.
            // "En boy oranı bozulmadan olabilecek en yakın durum" -> Fit and Pad.
            const scale = Math.min(TARGET_SIZE / img.width, TARGET_SIZE / img.height);
            const w = img.width * scale;
            const h = img.height * scale;
            const x = (TARGET_SIZE - w) / 2;
            const y = (TARGET_SIZE - h) / 2;

            ctx.drawImage(img, x, y, w, h);

            // 3. Export as JPEG (forces RGB, drops alpha channel but we painted white background already)
            canvas.toBlob((blob) => {
              if (blob) {
                const newName = file.name.replace(/\.[^/.]+$/, "") + ".jpg";
                const newFile = new File([blob], newName, { type: 'image/jpeg' });
                console.log(`[SearchService] ResNet processing done. ${file.size} -> ${newFile.size} bytes`);
                resolve(newFile);
              } else {
                resolve(file);
              }
            }, 'image/jpeg', 0.95);

          } catch (err) {
            console.error('[SearchService] ResNet processing failed:', err);
            resolve(file);
          }
        };
        img.onerror = () => {
            console.warn('[SearchService] Image load failed');
            resolve(file);
        };
      };
      reader.onerror = () => {
          console.warn('[SearchService] FileReader failed');
          resolve(file);
      };
      reader.readAsDataURL(file);
    });
  }

  classifyImage(file: File, skipProcessing: boolean = false): Observable<{ clusterId: number; clusterName?: string; version: number; similarProductIds?: number[] }> {
    // Return Observable directly using RxJS operators
    const fileProcessing$ = skipProcessing ? from(Promise.resolve(file)) : from(
      this.processImageForResNet(file).catch(e => {
        console.warn('[SearchService] Fallback to original file:', e);
        return file;
      })
    );

    return fileProcessing$.pipe(
      switchMap(fileToSend => {
        const formData = new FormData();
        formData.append('file', fileToSend);
        try {
          const url = URL.createObjectURL(fileToSend);
          this.productService.queryImageUrl.set(url);
        } catch { }
        
        return this.http.post<{ clusterId: number; clusterName?: string; version: number; similarProductIds?: number[] }>(`${this.apiUrl}/clustering/classify`, formData);
      })
    );
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

  searchProductsByName(query: string, pageNumber: number = 1, pageSize: number = 9, companyId?: number | null, brandId?: number | null, categoryId?: number | null, autoCategoryId?: number | null): void {
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
    if (autoCategoryId) {
      params = params.set('autoCategoryId', autoCategoryId);
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
