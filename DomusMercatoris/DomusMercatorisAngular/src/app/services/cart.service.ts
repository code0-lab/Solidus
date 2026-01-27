import { Injectable, signal, computed, effect, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Product, VariantProduct } from '../models/product.model';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';
import { Subject, debounceTime, firstValueFrom } from 'rxjs';
import { AlertService } from './alert.service';

export interface CartItem {
  id?: number; // Database ID
  product: Product;
  variant?: VariantProduct;
  qty: number;
}

interface CartItemDto {
  id: number;
  productId: number;
  productName: string;
  productImage: string;
  price: number;
  variantProductId?: number;
  variantColor?: string;
  quantity: number;
  companyId: number;
}

interface AddToCartDto {
  productId: number;
  variantProductId?: number;
  quantity: number;
}

interface SyncCartItemDto {
  productId: number;
  variantProductId?: number;
  quantity: number;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private alertService = inject(AlertService);
  private apiUrl = `${environment.apiUrl}/cart`;
  private readonly LOCAL_CART_KEY = 'local_cart';

  items = signal<CartItem[]>([]);
  highlightedItemIds = signal<number[]>([]);
  itemWarnings = signal<Map<number, string>>(new Map()); // itemId -> warning message

  totalCount = computed(() => this.items().reduce((sum, i) => sum + i.qty, 0));
  totalPrice = computed(() => this.items().reduce((sum, i) => {
    const price = i.variant ? i.variant.price : (i.product.price || 0);
    return sum + price * i.qty;
  }, 0));

  private updateSubject = new Subject<{ itemId: number, qty: number }>();

  constructor() {
    // Initialize cart based on auth state
    effect(() => {
      const user = this.authService.currentUser();
      if (user) {
        // Logged in: Sync local cart then fetch DB cart
        this.handleLoginSync().then(() => {
           this.fetchCart();
        });
      } else {
        // Logged out: Load from local storage
        this.loadLocalCart();
      }
    }, { allowSignalWrites: true });

    // Setup debounce for updates
    this.updateSubject.pipe(
      debounceTime(500)
    ).subscribe(update => {
       this.http.patch<any>(`${this.apiUrl}/${update.itemId}`, { quantity: update.qty }).subscribe(res => {
         if (res && res.isWarning) {
             this.alertService.showAlert(res.message);
             this.fetchCart();
         }
       });
    });
  }

  private loadLocalCart() {
    const stored = localStorage.getItem(this.LOCAL_CART_KEY);
    if (stored) {
      try {
        this.items.set(JSON.parse(stored));
      } catch {
        this.items.set([]);
      }
    } else {
        this.items.set([]);
    }
  }

  private saveLocalCart(items: CartItem[]) {
    localStorage.setItem(this.LOCAL_CART_KEY, JSON.stringify(items));
  }

  private async handleLoginSync() {
    const localItems = this.getLocalCartItems();
    if (localItems.length > 0) {
      const syncDtos: SyncCartItemDto[] = localItems.map(i => ({
        productId: i.product.id,
        variantProductId: i.variant?.id,
        quantity: i.qty
      }));
      
      try {
        await firstValueFrom(this.http.post(`${this.apiUrl}/sync`, syncDtos));
        // Clear local storage after sync
        localStorage.removeItem(this.LOCAL_CART_KEY);
      } catch (e) {
        console.error('Sync failed', e);
      }
    }
  }

  private getLocalCartItems(): CartItem[] {
      const stored = localStorage.getItem(this.LOCAL_CART_KEY);
      return stored ? JSON.parse(stored) : [];
  }

  fetchCart() {
    this.http.get<CartItemDto[]>(this.apiUrl).subscribe({
      next: (dtos) => {
        const mappedItems = dtos.map(dto => this.mapDtoToItem(dto));
        this.items.set(mappedItems);
      },
      error: (err) => console.error('Failed to fetch cart', err)
    });
  }

  mapDtoToItem(dto: CartItemDto): CartItem {
    return {
      id: dto.id,
      product: {
        id: dto.productId,
        name: dto.productName,
        price: dto.price,
        images: [dto.productImage],
        companyId: dto.companyId,
        // Fill other required fields with dummy values if needed, or rely on optionality
      } as Product,
      variant: dto.variantProductId ? {
        id: dto.variantProductId,
        productId: dto.productId,
        productName: dto.productName,
        color: dto.variantColor || '',
        price: dto.price,
        coverImage: dto.productImage,
        isCustomizable: false
      } as VariantProduct : undefined,
      qty: dto.quantity
    };
  }

  add(product: Product, variant?: VariantProduct, quantity: number = 1) {
    const user = this.authService.currentUser();
    
    if (user) {
      // API Call
      const dto: AddToCartDto = {
        productId: product.id,
        variantProductId: variant?.id,
        quantity
      };
      this.http.post<any>(this.apiUrl, dto).subscribe(res => {
        if (res && res.isWarning) {
            this.alertService.showAlert(res.message);
        }
        this.fetchCart(); // Refresh cart from DB
      });
    } else {
      // Local Logic
      const arr = this.items();
      const idx = arr.findIndex(i => 
        i.product.id === product.id && 
        ((!i.variant && !variant) || (i.variant?.id === variant?.id))
      );
      
      let next = [...arr];
      if (idx >= 0) {
        next[idx] = { ...next[idx], qty: next[idx].qty + quantity };
      } else {
        next = [...next, { product, variant, qty: quantity }];
      }
      this.items.set(next);
      this.saveLocalCart(next);
    }
  }

  increment(item: CartItem) {
     this.updateItemQty(item, item.qty + 1);
  }

  decrement(item: CartItem) {
    if (item.qty > 1) {
       this.updateItemQty(item, item.qty - 1);
    } else {
       this.remove(item);
    }
  }

  public updateQuantity(item: CartItem, newQty: number) {
      if (newQty > 0) {
          this.updateItemQty(item, newQty);
      }
  }

  private updateItemQty(item: CartItem, newQty: number) {
    const user = this.authService.currentUser();
    
    // Optimistic Update
    const arr = this.items();
    const idx = arr.findIndex(i => 
       // Match by ID if exists (DB items), otherwise by properties (local items)
       (item.id && i.id === item.id) ||
       (!item.id && i.product.id === item.product.id && 
        ((!i.variant && !item.variant) || (i.variant?.id === item.variant?.id)))
    );

    if (idx >= 0) {
        const next = [...arr];
        next[idx] = { ...next[idx], qty: newQty };
        this.items.set(next);
        
        if (user && item.id) {
            // Debounced API call
            this.updateSubject.next({ itemId: item.id, qty: newQty });
        } else {
            this.saveLocalCart(next);
        }
    }
  }

  remove(item: CartItem) {
    const user = this.authService.currentUser();
    
    // Optimistic Remove
    const filterFn = (i: CartItem) => {
         if (item.id && i.id === item.id) return false;
         if (!item.id && i.product.id === item.product.id && ((!i.variant && !item.variant) || (i.variant?.id === item.variant?.id))) return false;
         return true;
    };
    
    this.items.set(this.items().filter(filterFn));

    if (user && item.id) {
        this.http.delete(`${this.apiUrl}/${item.id}`).subscribe();
    } else {
        this.saveLocalCart(this.items());
    }
  }

  clear() {
    const user = this.authService.currentUser();
    this.items.set([]);
    if (user) {
        this.http.delete(this.apiUrl).subscribe();
    } else {
        this.saveLocalCart([]);
    }
  }
}