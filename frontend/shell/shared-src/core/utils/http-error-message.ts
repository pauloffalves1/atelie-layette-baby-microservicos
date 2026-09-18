import { HttpErrorResponse } from '@angular/common/http';

/**
 * A user-facing message for a failed request. Auth forms used to show one fixed text for any
 * failure — "E-mail ou senha inválidos" even when the Gateway's rate limit (5/min) or a network
 * drop was the cause, which sent people retrying a correct password straight into more 429s.
 *
 * @param unauthorizedMessage shown for 401 (e.g. wrong password); other 4xx use the API's `detail`.
 * @param forbiddenMessage shown for 403 (the session lacks a permission); defaults to
 * `unauthorizedMessage`, since from the person's point of view "you can't do this" is the same
 * answer whether the token expired or never carried the permission — a screen behind a permission
 * gets both, and "não foi possível carregar" tells them nothing about what to do next.
 */
export function httpErrorMessage(
  err: unknown,
  fallback: string,
  unauthorizedMessage?: string,
  forbiddenMessage?: string,
): string {
  const response = err instanceof HttpErrorResponse ? err : null;
  const status = response?.status ?? -1;
  const detail = (response?.error as { detail?: string } | null)?.detail;

  if (status === 0) return 'Não foi possível conectar. Verifique sua internet e tente de novo.';
  if (status === 429) return 'Muitas tentativas em pouco tempo. Aguarde um minuto e tente de novo.';
  if (status >= 500) return 'O site está com instabilidade agora. Tente de novo em alguns minutos.';
  if (status === 401 && unauthorizedMessage) return unauthorizedMessage;
  if (status === 403 && (forbiddenMessage ?? unauthorizedMessage)) return (forbiddenMessage ?? unauthorizedMessage)!;
  return detail ?? fallback;
}
