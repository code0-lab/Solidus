import { TestBed } from '@angular/core/testing';
import { LoadingService } from './loading.service';
import { describe, it, expect, beforeEach } from 'vitest';

describe('LoadingService', () => {
  let service: LoadingService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LoadingService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have initial loading state as false', () => {
    expect(service.isLoading()).toBe(false);
  });

  it('should set loading to true when show is called', () => {
    service.show();
    expect(service.isLoading()).toBe(true);
  });

  it('should keep loading true when show is called multiple times', () => {
    service.show();
    service.show();
    expect(service.isLoading()).toBe(true);
  });

  it('should set loading to false only when hide is called same amount of times as show', () => {
    service.show(); // count 1
    service.show(); // count 2
    expect(service.isLoading()).toBe(true);

    service.hide(); // count 1
    expect(service.isLoading()).toBe(true);

    service.hide(); // count 0
    expect(service.isLoading()).toBe(false);
  });

  it('should not go below zero', () => {
    service.hide();
    expect(service.isLoading()).toBe(false);
  });
});
