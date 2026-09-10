import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '@shared/environment';
import {
  AdminLoginResponse,
  AdminPermissionName,
  AuthResponse,
  AuthUser,
  LoginRequest,
  TwoFactorSetup,
} from '../models/auth.model';

const STORAGE_KEY = 'atelie-bebe.admin.token';
const USER_KEY = 'atelie-bebe.admin.user';

@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  private readonly userSignal = signal<AuthUser | null>(this.readStoredUser());

  readonly currentUser = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.userSignal() !== null);
  readonly permissions = computed<AdminPermissionName[]>(() => this.userSignal()?.permissions ?? []);

  hasPermission(permission: AdminPermissionName): boolean {
    return this.permissions().includes(permission);
  }

  constructor(private readonly http: HttpClient) {}

  login(request: LoginRequest): Observable<AdminLoginResponse> {
    return this.http.post<AdminLoginResponse>(`${environment.apiUrl}/admin/auth/login`, request).pipe(
      tap((response) => {
        if (response.auth) this.persistSession(response.auth);
      }),
    );
  }

  verifyTwoFactor(adminId: string, code: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/admin/auth/2fa/verify`, { adminId, code })
      .pipe(tap((response) => this.persistSession(response)));
  }

  getTwoFactorStatus(): Observable<{ enabled: boolean }> {
    return this.http.get<{ enabled: boolean }>(`${environment.apiUrl}/admin/auth/2fa/status`);
  }

  beginTwoFactorSetup(): Observable<TwoFactorSetup> {
    return this.http.post<TwoFactorSetup>(`${environment.apiUrl}/admin/auth/2fa/setup`, {});
  }

  enableTwoFactor(secret: string, code: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/admin/auth/2fa/enable`, { secret, code });
  }

  disableTwoFactor(password: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/admin/auth/2fa/disable`, { password });
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/admin/auth/change-password`, { currentPassword, newPassword });
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
    const user: AuthUser = {
      id: response.id,
      name: response.name,
      email: response.email,
      permissions: response.permissions ?? [],
    };
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.userSignal.set(user);
  }

  private readStoredUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  }
}
