import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { AuditLogEntry, AuditLogFilterOptions, AuditLogFilters } from '../models/audit-log.model';
import { PagedResult } from '../models/pagination.model';

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  constructor(private readonly http: HttpClient) {}

  list(page: number, filters: AuditLogFilters = {}, pageSize = 20): Observable<PagedResult<AuditLogEntry>> {
    const params: Record<string, string | number> = { page, pageSize };
    for (const [key, value] of Object.entries(filters)) {
      if (value) params[key] = value;
    }
    return this.http.get<PagedResult<AuditLogEntry>>(`${environment.apiUrl}/admin/audit-log`, { params });
  }

  filterOptions(): Observable<AuditLogFilterOptions> {
    return this.http.get<AuditLogFilterOptions>(`${environment.apiUrl}/admin/audit-log/filters`);
  }
}
