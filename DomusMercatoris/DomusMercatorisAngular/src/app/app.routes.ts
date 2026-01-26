import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { 
    path: '', 
    loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent) 
  },
  { 
    path: 'search', 
    loadComponent: () => import('./pages/search/search.component').then(m => m.SearchComponent) 
  },
  { 
    path: 'products/search', 
    loadComponent: () => import('./pages/search/search.component').then(m => m.SearchComponent) 
  },
  {
    path: 'payment-waiting/:id',
    loadComponent: () => import('./pages/payment-waiting/payment-waiting.component').then(m => m.PaymentWaitingComponent)
  },
  { 
    path: 'profile',  
    loadComponent: () => import('./pages/profile/profile.component').then(m => m.ProfileComponent),
    canActivate: [authGuard] 
  },
  {
    path: 'my-orders',
    loadComponent: () => import('./pages/my-orders/my-orders.component').then(m => m.MyOrdersComponent),
    canActivate: [authGuard]
  },
  { 
    path: '403', 
    loadComponent: () => import('./pages/forbidden/forbidden.component').then(m => m.ForbiddenComponent) 
  },
  { 
    path: '500', 
    loadComponent: () => import('./pages/server-error/server-error.component').then(m => m.ServerErrorComponent) 
  },
  { 
    path: '**', 
    loadComponent: () => import('./pages/not-found/not-found.component').then(m => m.NotFoundComponent) 
  }
];
