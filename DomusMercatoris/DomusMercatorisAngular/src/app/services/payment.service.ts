import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

export interface PaymentStatus {
  orderId: number;
  status: string;
  isApproved: boolean;
}

import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private hubConnection: HubConnection | null = null;
  public paymentStatus = signal<PaymentStatus | null>(null);
  public connectionState = signal<string>('Disconnected');
  public activePaymentCode = signal<string | null>(null);

  private get apiUrl(): string {
    return environment.apiUrl;
  }

  constructor(private http: HttpClient) {}

  public verifyCode(orderId: number, code: string) {
    return this.http.post(`${this.apiUrl}/Payment/verify-code`, { orderId, code });
  }

  public rejectPayment(orderId: string | number) {
    return this.http.post(`${this.apiUrl}/Payment/reject/${orderId}`, {});
  }

  public startConnection(orderId: string) {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl('/paymentHub')
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR Connection started');
        this.connectionState.set('Connected');
        this.joinOrderGroup(orderId);
      })
      .catch(err => {
        console.log('Error while starting connection: ' + err);
        this.connectionState.set('Error');
      });

    this.hubConnection.on('PaymentStatusChanged', (data: PaymentStatus) => {
      console.log('Payment status received:', data);
      this.paymentStatus.set(data);
    });
  }

  public joinOrderGroup(orderId: string) {
    if (this.hubConnection) {
      this.hubConnection.invoke('JoinOrderGroup', orderId)
        .catch(err => console.error(err));
    }
  }

  public stopConnection() {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.connectionState.set('Disconnected');
    }
  }
}
