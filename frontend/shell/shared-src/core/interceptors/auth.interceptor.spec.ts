import { HttpErrorResponse } from '@angular/common/http';
import { isSessionRejected } from './auth.interceptor';

describe('isSessionRejected', () => {
  it('treats an empty-body 401 (JWT middleware: expired/invalid token) as a dead session', () => {
    expect(isSessionRejected(new HttpErrorResponse({ status: 401, error: null }))).toBe(true);
    expect(isSessionRejected(new HttpErrorResponse({ status: 401, error: '' }))).toBe(true);
  });

  it('keeps the session on a 401 business error such as a wrong current password', () => {
    const wrongPassword = new HttpErrorResponse({ status: 401, error: { title: 'Não autorizado', detail: 'Senha atual incorreta.' } });
    expect(isSessionRejected(wrongPassword)).toBe(false);
  });

  it('ignores every other status', () => {
    expect(isSessionRejected(new HttpErrorResponse({ status: 403, error: null }))).toBe(false);
    expect(isSessionRejected(new HttpErrorResponse({ status: 500, error: null }))).toBe(false);
    expect(isSessionRejected(new Error('boom'))).toBe(false);
  });
});
