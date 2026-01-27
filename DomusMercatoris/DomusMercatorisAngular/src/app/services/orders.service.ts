import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface OrderItem {
  productId: number;
  variantProductId?: number;
  quantity: number;
}

export interface FleetingUserPayload {
  email: string;
  firstName?: string;
  lastName?: string;
  address?: string;
}

export interface CheckoutPayload {
  companyId: number;
  userId?: number;
  fleetingUser?: FleetingUserPayload;
  items: OrderItem[];
}

export interface OrderItemResponse {
  productId: number;
  productName?: string;
  variantProductId?: number;
  variantName?: string;
  quantity: number;
  unitPrice?: number;
  imageUrl?: string;
}

export interface OrderResponse {
  id: number;
  isPaid: boolean;
  totalPrice: number;
  companyId: number;
  userId?: number;
  fleetingUserId?: number;
  cargoTrackingId?: number;
  status: string;
  createdAt: string;
  paymentCode?: string;
  orderItems: OrderItemResponse[];
}

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private http = inject(HttpClient);
  
  private get apiUrl(): string {
    return environment.apiUrl;
  }

  checkout(payload: CheckoutPayload): Observable<OrderResponse> {
    return this.http.post<OrderResponse>(`${this.apiUrl}/orders/checkout`, payload);
  }

  markPaid(id: number): Observable<OrderResponse> {
    return this.http.post<OrderResponse>(`${this.apiUrl}/orders/${id}/mark-paid`, {});
  }

  getMyOrders(): Observable<OrderResponse[]> {
    return this.http.get<OrderResponse[]>(`${this.apiUrl}/orders/my-orders`);
  }

  getOrderById(id: number): Observable<OrderResponse> {
    return this.http.get<OrderResponse>(`${this.apiUrl}/orders/${id}`);
  }
}
