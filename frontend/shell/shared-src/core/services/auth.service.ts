import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '@shared/environment';
import { AuthResponse, AuthUser, CustomerProfile, LoginRequest, RegisterCustomerRequest } from '../models/auth.model';

const STORAGE_KEY = 'atelie-bebe.customer.token';
const USER_KEY = 'atelie-bebe.customer.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly userSignal = signal<AuthUser | null>(this.readStoredUser());

  readonly currentUser = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.userSignal() !== null);

  constructor(private readonly http: HttpClient) {}

  register(request: RegisterCustomerRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/register`, request)
      .pipe(tap((response) => this.persistSession(response)));
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/login`, request)
      .pipe(tap((response) => this.persistSession(response)));
  }

  getProfile(): Observable<CustomerProfile> {
    return this.http.get<CustomerProfile>(`${environment.apiUrl}/auth/me`);
  }

  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/forgot-password`, { email });
  }

  resetPassword(token: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/reset-password`, { token, newPassword });
  }

  deleteAccount(password: string): Observable<void> {
    return this.http
      .post<void>(`${environment.apiUrl}/auth/delete-account`, { password })
      .pipe(tap(() => this.logout()));
  }

  verifyEmail(token: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/verify-email`, { token });
  }

  resendVerification(): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/resend-verification`, {});
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    localStorage.removeItem(USER_KEY);
    this.userSignal.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem(STORAGE_KEY);
  }

  private persistSession(response: AuthResponse): void {
    localStorage.setItem(STORAGE_KEY, response.token);
    const user: AuthUser = { id: response.id, name: response.name, email: response.email };
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.userSignal.set(user);
  }

  private readStoredUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  }
}
