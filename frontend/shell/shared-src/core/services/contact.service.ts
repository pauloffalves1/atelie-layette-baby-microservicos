import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { PagedResult } from '../models/pagination.model';

export interface ContactMessage {
  id: string;
  name: string;
  email: string;
  phone: string;
  message: string;
  createdAt: string;
}

export interface SubmitContactRequest {
  name: string;
  email: string;
  phone: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ContactService {
  constructor(private readonly http: HttpClient) {}

  submit(request: SubmitContactRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/contact`, request);
  }

  listForAdmin(page = 1, pageSize = 20): Observable<PagedResult<ContactMessage>> {
    return this.http.get<PagedResult<ContactMessage>>(`${environment.apiUrl}/admin/contact-messages`, {
      params: { page, pageSize },
    });
  }
}
