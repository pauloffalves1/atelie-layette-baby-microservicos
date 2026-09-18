import { HttpErrorResponse } from '@angular/common/http';
import { httpErrorMessage } from './http-error-message';

const error = (status: number, detail?: string) =>
  new HttpErrorResponse({ status, error: detail ? { detail } : null });

describe('httpErrorMessage', () => {
  it('uses the specific message for a wrong password (401)', () => {
    expect(httpErrorMessage(error(401), 'Falhou.', 'E-mail ou senha inválidos.')).toBe('E-mail ou senha inválidos.');
  });

  it('explains the rate limit instead of blaming the password', () => {
    expect(httpErrorMessage(error(429), 'Falhou.', 'E-mail ou senha inválidos.')).toContain('Muitas tentativas');
  });

  it('distinguishes network and server failures', () => {
    expect(httpErrorMessage(error(0), 'Falhou.')).toContain('conectar');
    expect(httpErrorMessage(error(503), 'Falhou.')).toContain('instabilidade');
  });

  it('answers a missing permission (403) with the same guidance as an expired session, unless given its own', () => {
    expect(httpErrorMessage(error(403), 'Falhou.', 'Entre de novo.')).toBe('Entre de novo.');
    expect(httpErrorMessage(error(403), 'Falhou.', 'Entre de novo.', 'Sem a permissão Testes.')).toBe(
      'Sem a permissão Testes.',
    );
    expect(httpErrorMessage(error(403), 'Falhou.')).toBe('Falhou.');
  });

  it("prefers the API's own detail for other client errors, else the fallback", () => {
    expect(httpErrorMessage(error(409, 'E-mail já cadastrado.'), 'Falhou.')).toBe('E-mail já cadastrado.');
    expect(httpErrorMessage(error(400), 'Falhou.')).toBe('Falhou.');
    expect(httpErrorMessage(new Error('boom'), 'Falhou.')).toBe('Falhou.');
  });
});
