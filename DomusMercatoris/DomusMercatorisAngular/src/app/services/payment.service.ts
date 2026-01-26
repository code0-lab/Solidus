import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

export interface PaymentStatus {
  orderId: number;
  status: string;
  isApproved: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private hubConnection: HubConnection | null = null;
  public paymentStatus = signal<PaymentStatus | null>(null);
  public connectionState = signal<string>('Disconnected');

  constructor() {}

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
