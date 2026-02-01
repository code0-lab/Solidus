import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { User, LoginResponse, UserProfileDto, LoginRequest, RegisterRequest, Company, UpdateProfileRequest, ChangePasswordRequest, ChangeEmailRequest } from '../models/user.model';
import { Observable, tap } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../environments/environment';
import { PaymentService } from './payment.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private paymentService = inject(PaymentService);

  private get apiUrl(): string {
    return `${environment.apiUrl}/users`;
  }

  private get companiesUrl(): string {
    return `${environment.apiUrl}/companies`;
  }

  private readonly USER_KEY = 'user';

  // Signals for state management
  currentUser = signal<User | null>(null);
  isLoginOpen = signal(false);
  isRegisterMode = signal(false);

  constructor() {
    this.loadUser();
  }

  getCompanies(): Observable<Company[]> {
    return this.http.get<Company[]>(this.companiesUrl);
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

  private saveUserToStorage(user: User) {
    this.currentUser.set(user);
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  private mapToUser(profile: UserProfileDto, token: string): User {
    return {
      id: profile.id,
      email: profile.email,
      token: token,
      firstName: profile.firstName,
      lastName: profile.lastName,
      phone: profile.phone ?? null,
      address: profile.address ?? null,
      companyId: profile.companyId,
      roles: profile.roles || [],
      profilePictureUrl: profile.profilePictureUrl,
      blockedByCompanyIds: profile.blockedByCompanyIds || []
    };
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        const user = this.mapToUser(response.user, response.token);
        this.saveUserToStorage(user);
        this.closeLogin();
      })
    );
  }

  register(data: RegisterRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, data);
  }

  logout() {
    this.currentUser.set(null);
    this.paymentService.resetState();
    localStorage.removeItem(this.USER_KEY);
  }

  // Modal State Management
  toggleLogin() {
    this.isLoginOpen.set(!this.isLoginOpen());
    if (this.isLoginOpen()) {
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
        this.updateUserFromProfile(profile);
      })
    );
  }

  updateProfile(payload: UpdateProfileRequest): Observable<UserProfileDto> {
    return this.http.put<UserProfileDto>(`${this.apiUrl}/me`, payload).pipe(
      tap(profile => {
        this.updateUserFromProfile(profile);
      })
    );
  }

  changePassword(payload: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/me/password`, payload);
  }



  uploadProfilePicture(file: File): Observable<UserProfileDto> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UserProfileDto>(`${this.apiUrl}/me/picture`, formData).pipe(
      tap(profile => {
        this.updateUserFromProfile(profile);
      })
    );
  }

  getProfileImageUrl(url: string | undefined): string {
    if (!url) return '';
    if (url.startsWith('http')) return url;
    if (url.startsWith('/uploads')) return url;
    return `${environment.apiUrl}${url}`;
  }

  private updateUserFromProfile(profile: UserProfileDto) {
    const current = this.currentUser();
    const token = current?.token ?? '';
    const updated = this.mapToUser(profile, token);

    this.saveUserToStorage(updated);
  }
}
