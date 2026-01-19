import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SaleItem {
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
  items: SaleItem[];
}

export interface SaleResponse {
  id: number;
  isPaid: boolean;
  totalPrice: number;
  companyId: number;
  userId?: number;
  fleetingUserId?: number;
  cargoTrackingId?: number;
}

@Injectable({ providedIn: 'root' })
export class SalesService {
  private http = inject(HttpClient);
  
  private get apiUrl(): string {
    return `/api`;
  }

  checkout(payload: CheckoutPayload): Observable<SaleResponse> {
    return this.http.post<SaleResponse>(`${this.apiUrl}/sales/checkout`, payload);
  }

  markPaid(id: number): Observable<SaleResponse> {
    return this.http.post<SaleResponse>(`${this.apiUrl}/sales/${id}/mark-paid`, {});
  }
}
