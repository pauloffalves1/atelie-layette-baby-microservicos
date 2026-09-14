import { AbstractControl, ValidationErrors } from '@angular/forms';

/**
 * Same rule as Catalog/Identity's `Cpf.Create`: 11 digits, not all equal, both check digits right.
 * The forms used to accept any "000.000.000-00" shape, so a typo only surfaced after submitting
 * ("O CPF '...' não é válido" from the API) — on the checkout, after filling the whole address.
 */
export function isValidCpf(value: string | null | undefined): boolean {
  const digits = (value ?? '').replace(/\D/g, '');
  if (digits.length !== 11 || /^(\d)\1{10}$/.test(digits)) return false;

  const numbers = [...digits].map(Number);
  const checkDigit = (count: number) => {
    const sum = numbers.slice(0, count).reduce((acc, n, i) => acc + n * (count + 1 - i), 0);
    const remainder = sum % 11;
    return remainder < 2 ? 0 : 11 - remainder;
  };
  return checkDigit(9) === numbers[9] && checkDigit(10) === numbers[10];
}

/** "12345678909" (or any partial input) → "123.456.789-09" as the customer types. */
export function maskCpf(value: string): string {
  const digits = value.replace(/\D/g, '').slice(0, 11);
  if (digits.length <= 3) return digits;
  if (digits.length <= 6) return `${digits.slice(0, 3)}.${digits.slice(3)}`;
  if (digits.length <= 9) return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6)}`;
  return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9)}`;
}

/** Empty is left to `Validators.required`. */
export function cpfValidator(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '');
  return value.trim() === '' || isValidCpf(value) ? null : { cpf: true };
}

/**
 * At least DDD + 8 digits. The phone mask alone let "(11) 9" through, which then reached the order
 * (and PagBank's 3DS, which needs a real number) as the customer's only contact.
 */
export function phoneDigitsValidator(control: AbstractControl): ValidationErrors | null {
  const digits = String(control.value ?? '').replace(/\D/g, '');
  return digits.length === 0 || digits.length >= 10 ? null : { phoneDigits: true };
}
