import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.currentUser()) {
    const token = authService.getToken();
    if (token && !authService.isTokenExpired(token)) {
      return true;
    }
    // Token expired
    authService.logout();
  }

  // Login değilse modalı aç ve false dön
  authService.toggleLogin();
  // Opsiyonel: Ana sayfaya yönlendir
  return router.createUrlTree(['/']);
};
