import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { SiteImage } from '../models/site-image.model';

@Injectable({ providedIn: 'root' })
export class SiteImageService {
  private readonly baseUrl = `${environment.apiUrl}/site-images`;
  private readonly adminUrl = `${environment.apiUrl}/admin/site-images`;

  constructor(private readonly http: HttpClient) {}

  list(): Observable<SiteImage[]> {
    return this.http.get<SiteImage[]>(this.baseUrl);
  }

  upload(key: string, file: File): Observable<SiteImage> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<SiteImage>(`${this.adminUrl}/${key}`, formData);
  }
}
