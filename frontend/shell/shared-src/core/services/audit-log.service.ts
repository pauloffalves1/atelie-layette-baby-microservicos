import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { AuditLogEntry } from '../models/audit-log.model';
import { PagedResult } from '../models/pagination.model';

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  constructor(private readonly http: HttpClient) {}

  list(page: number, pageSize = 20): Observable<PagedResult<AuditLogEntry>> {
    return this.http.get<PagedResult<AuditLogEntry>>(`${environment.apiUrl}/admin/audit-log`, {
      params: { page, pageSize },
    });
  }
}
