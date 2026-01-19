import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { Router } from '@angular/router';
import { errorInterceptor } from './error.interceptor';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

describe('errorInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;
  let routerSpy: { navigate: any };

  beforeEach(() => {
    routerSpy = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: routerSpy }
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should redirect to /500 on 500 error', () => {
    httpClient.get('/test').subscribe({
      next: () => { throw new Error('Should have failed with 500 error'); },
      error: (error) => {
        expect(error.status).toBe(500);
      }
    });

    const req = httpMock.expectOne('/test');
    req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/500']);
  });

  it('should propagate other errors', () => {
    httpClient.get('/test-other').subscribe({
      next: () => { throw new Error('Should have failed with 400 error'); },
      error: (error) => {
        expect(error.status).toBe(400);
      }
    });

    const req = httpMock.expectOne('/test-other');
    req.flush('Bad Request', { status: 400, statusText: 'Bad Request' });

    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });
});
