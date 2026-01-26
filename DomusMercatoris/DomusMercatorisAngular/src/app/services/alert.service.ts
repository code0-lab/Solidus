import { Injectable, signal } from '@angular/core';

export interface AlertOptions {
  message: string;
  type: 'alert' | 'confirm';
  confirmText?: string;
  cancelText?: string;
  onConfirm?: () => void;
  onCancel?: () => void;
}

@Injectable({
  providedIn: 'root'
})
export class AlertService {
  isOpen = signal(false);
  options = signal<AlertOptions>({
    message: '',
    type: 'alert'
  });

  showAlert(message: string, onConfirm?: () => void) {
    this.options.set({
      message,
      type: 'alert',
      confirmText: 'OK',
      onConfirm
    });
    this.isOpen.set(true);
  }

  showConfirm(message: string, onConfirm: () => void, onCancel?: () => void) {
    this.options.set({
      message,
      type: 'confirm',
      confirmText: 'Yes',
      cancelText: 'Cancel',
      onConfirm,
      onCancel
    });
    this.isOpen.set(true);
  }

  close() {
    this.isOpen.set(false);
  }

  confirm() {
    const opts = this.options();
    if (opts.onConfirm) {
      opts.onConfirm();
    }
    this.close();
  }

  cancel() {
    const opts = this.options();
    if (opts.onCancel) {
      opts.onCancel();
    }
    this.close();
  }
}
