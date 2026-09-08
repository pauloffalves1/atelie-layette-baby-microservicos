import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { GalleryImage } from '../models/gallery-image.model';

@Injectable({ providedIn: 'root' })
export class GalleryImageService {
  private readonly baseUrl = `${environment.apiUrl}/gallery-images`;
  private readonly adminUrl = `${environment.apiUrl}/admin/gallery-images`;

  constructor(private readonly http: HttpClient) {}

  list(): Observable<GalleryImage[]> {
    return this.http.get<GalleryImage[]>(this.baseUrl);
  }

  upload(file: File): Observable<GalleryImage> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<GalleryImage>(this.adminUrl, formData);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.adminUrl}/${id}`);
  }
}
