import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RefundResponse {
  id: number;
  orderItemId: number;
  productName: string;
  quantity: number;
  refundAmount: number;
  reason: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  rejectionReason?: string;
  createdAt: string;
}

export interface CreateRefundRequest {
  orderItemId: number;
  quantity: number;
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class RefundsService {
  private http = inject(HttpClient);

  private get apiUrl(): string {
    return environment.apiUrl;
  }

  getMyRefunds(): Observable<RefundResponse[]> {
    return this.http.get<RefundResponse[]>(`${this.apiUrl}/refunds/my`);
  }

  createRefundRequest(request: CreateRefundRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/refunds`, request);
  }
}
