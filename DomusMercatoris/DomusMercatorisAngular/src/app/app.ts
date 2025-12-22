import { Component, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('DomusMercatorisAngular');
  protected readonly products = signal<Array<{ id: number; name: string; sku: string; priceText: string; imageUrl: string; bg: string }>>([]);
  protected readonly categories = signal<Array<{ id: number; name: string }>>([]);
  protected readonly isFilterOpen = signal(false);
  protected readonly selectedCategoryId = signal<number | null>(null);

  private readonly http = inject(HttpClient);
  private toAbsoluteImageUrl(img?: string): string {
    const fallback = 'https://via.placeholder.com/600x400?text=Product';
    if (!img || img.length === 0) return fallback;
    try {
      const u = new URL(img);
      return u.toString();
    } catch {
      const base = 'http://localhost:5280';
      return img.startsWith('/') ? base + img : `${base}/${img}`;
    }
  }

  private sampleProducts() {
    const palette = ['#e8f5ff', '#fff1f1', '#e9fff4', '#f6f4ff'];
    const nf = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
    return [
      { id: 1, name: 'Örnek Ürün A', sku: 'SAMPLE-A', price: 19.9, images: [] },
      { id: 2, name: 'Örnek Ürün B', sku: 'SAMPLE-B', price: 29.9, images: [] },
      { id: 3, name: 'Örnek Ürün C', sku: 'SAMPLE-C', price: 39.9, images: [] },
      { id: 4, name: 'Örnek Ürün D', sku: 'SAMPLE-D', price: 49.9, images: [] }
    ].map((p, i) => ({
      id: p.id,
      name: p.name,
      sku: p.sku,
      priceText: nf.format(p.price ?? 0),
      imageUrl: this.toAbsoluteImageUrl(undefined),
      bg: palette[i % palette.length],
    }));
  }

  constructor() {
    this.fetchProducts();
    this.fetchCategories();
  }

  fetchCategories() {
    this.http.get<Array<{ id: number; name: string }>>('http://localhost:5280/api/categories')
      .subscribe({
        next: (data) => this.categories.set(data),
        error: () => console.error('Failed to fetch categories')
      });
  }

  fetchProducts(categoryId?: number) {
    const url = categoryId 
      ? `http://localhost:5280/api/products/by-category/${categoryId}`
      : 'http://localhost:5280/api/products';

    this.http.get<Array<{ id: number; name: string; sku: string; price: number; images?: string[] }>>(url)
      .subscribe({
        next: (data) => {
          const palette = ['#e8f5ff', '#fff1f1', '#e9fff4', '#f6f4ff'];
          const nf = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
          const mapped = data.map((p, i) => ({
            id: p.id,
            name: p.name,
            sku: p.sku,
            priceText: nf.format(p.price ?? 0),
            imageUrl: this.toAbsoluteImageUrl(p.images && p.images.length > 0 ? p.images[0] : undefined),
            bg: palette[i % palette.length],
          }));
          this.products.set(mapped.length > 0 ? mapped : this.sampleProducts());
        },
        error: () => {
          this.products.set(this.sampleProducts());
        }
      });
  }

  toggleFilter() {
    this.isFilterOpen.set(!this.isFilterOpen());
  }

  selectCategory(id: number | null) {
    this.selectedCategoryId.set(id);
    this.fetchProducts(id ?? undefined);
    this.isFilterOpen.set(false);
  }
}
