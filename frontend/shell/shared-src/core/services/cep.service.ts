import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ViaCepAddress } from '../models/cep.model';

@Injectable({ providedIn: 'root' })
export class CepService {
  constructor(private readonly http: HttpClient) {}

  /** Looks up a Brazilian CEP via ViaCEP. Pass digits only or formatted (00000-000) — both work. */
  lookup(cep: string): Observable<ViaCepAddress> {
    const digits = cep.replace(/\D/g, '');
    return this.http.get<ViaCepAddress>(`https://viacep.com.br/ws/${digits}/json/`);
  }
}
