import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { CustomerSummary, UpdateCustomerRequest } from '../models/customer.model';

@Injectable({ providedIn: 'root' })
export class CustomerAdminService {
  private readonly baseUrl = `${environment.apiUrl}/admin/customers`;

  constructor(private readonly http: HttpClient) {}

  list(): Observable<CustomerSummary[]> {
    return this.http.get<CustomerSummary[]>(this.baseUrl);
  }

  getById(id: string): Observable<CustomerSummary> {
    return this.http.get<CustomerSummary>(`${this.baseUrl}/${id}`);
  }

  update(id: string, request: UpdateCustomerRequest): Observable<CustomerSummary> {
    return this.http.put<CustomerSummary>(`${this.baseUrl}/${id}`, request);
  }

  verifyEmail(id: string): Observable<CustomerSummary> {
    return this.http.post<CustomerSummary>(`${this.baseUrl}/${id}/verify-email`, {});
  }

  /** RF40 — aprova/revoga a cliente como usuária de teste. Exige a permissão Testes; só vale no próximo login dela. */
  setTest(id: string, isTest: boolean): Observable<CustomerSummary> {
    return this.http.patch<CustomerSummary>(`${this.baseUrl}/${id}/test`, { isTest });
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
