import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { User, LoginResponse, UserProfileDto } from '../models/user.model';
import { Observable, tap } from 'rxjs';
import { jwtDecode } from 'jwt-decode';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  
  private get apiUrl(): string {
    return `/api/users`;
  }

  private get companiesUrl(): string {
    return `/api/companies`;
  }

  private readonly USER_KEY = 'user';

  // Signals for state management
  currentUser = signal<User | null>(null);
  isLoginOpen = signal(false);
  isRegisterMode = signal(false);

  constructor() {
    this.loadUser();
  }

  getCompanies(): Observable<any[]> {
    return this.http.get<any[]>(this.companiesUrl);
  }

  private loadUser() {
    const storedUser = localStorage.getItem(this.USER_KEY);
    if (storedUser) {
      try {
        const user: User = JSON.parse(storedUser);
        
        // Check if token is expired
        if (this.isTokenExpired(user.token)) {
          this.logout();
          return;
        }

        this.currentUser.set(user);
      } catch (e) {
        console.error('Failed to parse user from local storage', e);
        localStorage.removeItem(this.USER_KEY);
      }
    }
  }

  isTokenExpired(token: string): boolean {
    if (!token) return true;
    try {
      const decoded: any = jwtDecode(token);
      if (!decoded.exp) return false; // Token doesn't have an expiration date
      
      const currentTime = Math.floor(Date.now() / 1000);
      return decoded.exp < currentTime;
    } catch (error) {
      return true; // Invalid token
    }
  }

  login(credentials: any): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        const user: User = { 
          id: response.user.id,
          email: response.user.email, 
          token: response.token,
          firstName: response.user.firstName,
          lastName: response.user.lastName,
          phone: response.user.phone ?? null,
          address: response.user.address ?? null
        };
        this.currentUser.set(user);
        localStorage.setItem(this.USER_KEY, JSON.stringify(user));
        this.closeLogin();
      })
    );
  }

  register(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, data);
  }

  logout() {
    this.currentUser.set(null);
    localStorage.removeItem(this.USER_KEY);
  }

  // Modal State Management
  toggleLogin() {
    if (this.currentUser()) {
      if (confirm('Are you sure you want to logout?')) {
        this.logout();
      }
    } else {
      this.isLoginOpen.set(!this.isLoginOpen());
      this.isRegisterMode.set(false);
    }
  }

  closeLogin() {
    this.isLoginOpen.set(false);
    this.isRegisterMode.set(false);
  }

  toggleAuthMode() {
    this.isRegisterMode.set(!this.isRegisterMode());
  }

  getToken(): string | null {
    return this.currentUser()?.token ?? null;
  }

  fetchProfile(): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${this.apiUrl}/me`).pipe(
      tap(profile => {
        const current = this.currentUser();
        const updated: User = {
          ...(current ?? { token: '' } as User),
          id: profile.id,
          email: profile.email,
          firstName: profile.firstName,
          lastName: profile.lastName,
          phone: profile.phone ?? null,
          address: profile.address ?? null
        };
        if (!updated.token && current?.token) {
          updated.token = current.token;
        }
        this.currentUser.set(updated);
        localStorage.setItem(this.USER_KEY, JSON.stringify(updated));
      })
    );
  }

  updateProfile(payload: { phone?: string | null; address?: string | null }): Observable<UserProfileDto> {
    return this.http.put<UserProfileDto>(`${this.apiUrl}/me`, payload).pipe(
      tap(profile => {
        const current = this.currentUser();
        const updated: User = {
          ...(current ?? { token: '' } as User),
          id: profile.id,
          email: profile.email,
          firstName: profile.firstName,
          lastName: profile.lastName,
          phone: profile.phone ?? null,
          address: profile.address ?? null
        };
        if (!updated.token && current?.token) {
          updated.token = current.token;
        }
        this.currentUser.set(updated);
        localStorage.setItem(this.USER_KEY, JSON.stringify(updated));
      })
    );
  }
}
