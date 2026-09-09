import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { CustomerAddress, SaveCustomerAddressRequest } from '../models/customer-address.model';

@Injectable({ providedIn: 'root' })
export class CustomerAddressService {
  private readonly baseUrl = `${environment.apiUrl}/customers/me/addresses`;

  constructor(private readonly http: HttpClient) {}

  list(): Observable<CustomerAddress[]> {
    return this.http.get<CustomerAddress[]>(this.baseUrl);
  }

  create(request: SaveCustomerAddressRequest): Observable<CustomerAddress> {
    return this.http.post<CustomerAddress>(this.baseUrl, request);
  }

  update(id: string, request: SaveCustomerAddressRequest): Observable<CustomerAddress> {
    return this.http.put<CustomerAddress>(`${this.baseUrl}/${id}`, request);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  setDefault(id: string): Observable<CustomerAddress> {
    return this.http.post<CustomerAddress>(`${this.baseUrl}/${id}/default`, {});
  }
}
