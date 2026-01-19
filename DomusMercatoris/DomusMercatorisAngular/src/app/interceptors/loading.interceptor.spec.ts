import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { loadingInterceptor } from './loading.interceptor';
import { LoadingService } from '../services/loading.service';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

describe('LoadingInterceptor', () => {
  let client: HttpClient;
  let httpTesting: HttpTestingController;
  let loadingServiceSpy: { show: any; hide: any };

  beforeEach(() => {
    loadingServiceSpy = {
      show: vi.fn(),
      hide: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([loadingInterceptor])),
        provideHttpClientTesting(),
        { provide: LoadingService, useValue: loadingServiceSpy }
      ]
    });

    client = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should call show() on request and hide() on response', () => {
    client.get('/test').subscribe();

    expect(loadingServiceSpy.show).toHaveBeenCalled();

    const req = httpTesting.expectOne('/test');
    req.flush({});

    expect(loadingServiceSpy.hide).toHaveBeenCalled();
  });

  it('should call hide() even on error', () => {
    client.get('/test').subscribe({
      error: () => {}
    });

    expect(loadingServiceSpy.show).toHaveBeenCalled();

    const req = httpTesting.expectOne('/test');
    req.flush('error', { status: 500, statusText: 'Server Error' });

    expect(loadingServiceSpy.hide).toHaveBeenCalled();
  });
});
