import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { AdminPermissionName, AdminSummary, CreateAdminRequest } from '../models/auth.model';

/** Backs `/admin/administradores` — only an admin holding AdminManagement can reach these routes (backend enforces it too). */
@Injectable({ providedIn: 'root' })
export class AdminManagementService {
  constructor(private readonly http: HttpClient) {}

  list(): Observable<AdminSummary[]> {
    return this.http.get<AdminSummary[]>(`${environment.apiUrl}/admin/admins`);
  }

  create(request: CreateAdminRequest): Observable<AdminSummary> {
    return this.http.post<AdminSummary>(`${environment.apiUrl}/admin/admins`, request);
  }

  updatePermissions(id: string, permissions: AdminPermissionName[]): Observable<AdminSummary> {
    return this.http.put<AdminSummary>(`${environment.apiUrl}/admin/admins/${id}/permissions`, { permissions });
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/admin/admins/${id}`);
  }
}
