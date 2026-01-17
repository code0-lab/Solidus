import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Banner } from '../models/banner.model';

@Injectable({
  providedIn: 'root'
})
export class BannerService {
  private http = inject(HttpClient);

  banner = signal<Banner | null>(null);

  private get apiUrl(): string {
    return `/api`;
  }

  loadActiveBanner(): void {
    this.http.get<Banner>(`${this.apiUrl}/banners/active`)
      .subscribe({
        next: (data) => this.banner.set(data),
        error: () => this.banner.set(null)
      });
  }
}
