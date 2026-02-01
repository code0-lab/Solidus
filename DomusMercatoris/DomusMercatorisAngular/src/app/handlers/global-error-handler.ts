import { ErrorHandler, Injectable, Injector, NgZone } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ToastService } from '../services/toast.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  constructor(private injector: Injector, private zone: NgZone) {}

  handleError(error: any): void {
    const toastService = this.injector.get(ToastService);
    
    // Check if it's an error from an HTTP response
    if (error instanceof HttpErrorResponse || error?.rejection instanceof HttpErrorResponse) {
        // These are often already handled by interceptors or specific service calls, 
        // but if they bubble up here, we can show a generic message.
        // We might want to avoid double-toasting if the interceptor already did something,
        // but our current interceptor only redirects on 500.
        
        const httpError = error instanceof HttpErrorResponse ? error : error.rejection;
        
        this.zone.run(() => {
            // Use the error message from backend if available, or fallback
            const message = httpError?.error?.message || httpError?.statusText || 'An unexpected error occurred';
            toastService.error(`Error: ${message}`);
        });
    } else {
        // Client-side / Application error
        console.error('Global Error Handler:', error);
        
        this.zone.run(() => {
             // For production, you might want to show "An unexpected error occurred"
             // For dev/demo, showing the error message might be helpful
             toastService.error('An unexpected error occurred. Please try again.');
        });
    }
  }
}
