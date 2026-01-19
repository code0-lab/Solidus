import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 500) {
        // Server error -> redirect to 500 page
        router.navigate(['/500']);
      } else if (error.status === 0) {
        // Network error or server down
        console.error('Network error occurred:', error);
        // Optionally show a toast or redirect to a generic error page
      }
      
      // Note: 401 and 403 are typically handled by AuthInterceptor
      
      return throwError(() => error);
    })
  );
};
