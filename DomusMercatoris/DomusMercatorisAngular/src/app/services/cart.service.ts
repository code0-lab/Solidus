import { Injectable, signal, computed } from '@angular/core';
import { Product, VariantProduct } from '../models/product.model';

export interface CartItem {
  product: Product;
  variant?: VariantProduct;
  qty: number;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  items = signal<CartItem[]>([]);

  totalCount = computed(() => this.items().reduce((sum, i) => sum + i.qty, 0));
  totalPrice = computed(() => this.items().reduce((sum, i) => {
    const price = i.variant ? i.variant.price : (i.product.price || 0);
    return sum + price * i.qty;
  }, 0));

  add(product: Product, variant?: VariantProduct) {
    const arr = this.items();
    const idx = arr.findIndex(i => 
      i.product.id === product.id && 
      ((!i.variant && !variant) || (i.variant?.id === variant?.id))
    );
    
    if (idx >= 0) {
      const next = [...arr];
      next[idx] = { ...next[idx], qty: next[idx].qty + 1 };
      this.items.set(next);
    } else {
      this.items.set([...arr, { product, variant, qty: 1 }]);
    }
  }

  increment(item: CartItem) {
    const arr = this.items();
    const idx = arr.findIndex(i => 
      i.product.id === item.product.id && 
      ((!i.variant && !item.variant) || (i.variant?.id === item.variant?.id))
    );
    
    if (idx >= 0) {
      const next = [...arr];
      next[idx] = { ...next[idx], qty: next[idx].qty + 1 };
      this.items.set(next);
    }
  }

  decrement(item: CartItem) {
    const arr = this.items();
    const idx = arr.findIndex(i => 
      i.product.id === item.product.id && 
      ((!i.variant && !item.variant) || (i.variant?.id === item.variant?.id))
    );
    
    if (idx >= 0) {
      const next = [...arr];
      const newQty = next[idx].qty - 1;
      if (newQty <= 0) {
        next.splice(idx, 1);
      } else {
        next[idx] = { ...next[idx], qty: newQty };
      }
      this.items.set(next);
    }
  }

  remove(item: CartItem) {
    this.items.set(this.items().filter(i => 
      !(i.product.id === item.product.id && 
        ((!i.variant && !item.variant) || (i.variant?.id === item.variant?.id)))
    ));
  }

  clear() {
    this.items.set([]);
  }
}
