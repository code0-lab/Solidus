import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';
import { describe, it, expect, beforeEach, vi } from 'vitest';

describe('AuthGuard', () => {
  let authServiceSpy: {
    currentUser: any;
    getToken: any;
    isTokenExpired: any;
    logout: any;
    toggleLogin: any;
  };
  let routerSpy: { createUrlTree: any };

  beforeEach(() => {
    authServiceSpy = {
      currentUser: vi.fn(),
      getToken: vi.fn(),
      isTokenExpired: vi.fn(),
      logout: vi.fn(),
      toggleLogin: vi.fn()
    };
    routerSpy = {
      createUrlTree: vi.fn().mockReturnValue('mockUrlTree')
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });
  });

  const executeGuard = () => TestBed.runInInjectionContext(() => authGuard(null as any, null as any));

  it('should allow access when user is logged in and token is valid', () => {
    authServiceSpy.currentUser.mockReturnValue({ id: 1, email: 'test@test.com' });
    authServiceSpy.getToken.mockReturnValue('valid-token');
    authServiceSpy.isTokenExpired.mockReturnValue(false);

    const result = executeGuard();

    expect(result).toBe(true);
    expect(authServiceSpy.toggleLogin).not.toHaveBeenCalled();
    expect(routerSpy.createUrlTree).not.toHaveBeenCalled();
  });

  it('should redirect to home and open login modal when user is not logged in', () => {
    authServiceSpy.currentUser.mockReturnValue(null);

    const result = executeGuard();

    expect(result).toBe('mockUrlTree');
    expect(authServiceSpy.toggleLogin).toHaveBeenCalled();
    expect(routerSpy.createUrlTree).toHaveBeenCalledWith(['/']);
  });

  it('should logout, redirect to home and open login modal when token is expired', () => {
    authServiceSpy.currentUser.mockReturnValue({ id: 1, email: 'test@test.com' });
    authServiceSpy.getToken.mockReturnValue('expired-token');
    authServiceSpy.isTokenExpired.mockReturnValue(true);

    const result = executeGuard();

    expect(result).toBe('mockUrlTree');
    expect(authServiceSpy.logout).toHaveBeenCalled();
    expect(authServiceSpy.toggleLogin).toHaveBeenCalled();
    expect(routerSpy.createUrlTree).toHaveBeenCalledWith(['/']);
  });
});
