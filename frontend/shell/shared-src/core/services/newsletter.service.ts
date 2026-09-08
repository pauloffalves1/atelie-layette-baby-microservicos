import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { NewsletterSubscriber } from '../models/newsletter.model';

@Injectable({ providedIn: 'root' })
export class NewsletterService {
  constructor(private readonly http: HttpClient) {}

  subscribe(email: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/newsletter/subscribe`, { email });
  }

  list(): Observable<NewsletterSubscriber[]> {
    return this.http.get<NewsletterSubscriber[]>(`${environment.apiUrl}/admin/newsletter`);
  }

  exportCsv(): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/admin/newsletter/export`, { responseType: 'blob' });
  }
}
