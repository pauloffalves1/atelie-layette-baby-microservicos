import { FormControl } from '@angular/forms';
import { cpfValidator, isValidCpf, maskCpf, phoneDigitsValidator } from './br-documents';

describe('isValidCpf', () => {
  it('accepts valid CPFs with or without punctuation', () => {
    expect(isValidCpf('111.444.777-35')).toBe(true);
    expect(isValidCpf('11144477735')).toBe(true);
    expect(isValidCpf('529.982.247-25')).toBe(true);
  });

  it('rejects wrong check digits, repeated digits and wrong length', () => {
    expect(isValidCpf('111.444.777-36')).toBe(false);
    expect(isValidCpf('111.111.111-11')).toBe(false);
    expect(isValidCpf('1114447773')).toBe(false);
    expect(isValidCpf('')).toBe(false);
    expect(isValidCpf(null)).toBe(false);
  });
});

describe('maskCpf', () => {
  it('formats progressively and drops extra digits', () => {
    expect(maskCpf('111')).toBe('111');
    expect(maskCpf('1114')).toBe('111.4');
    expect(maskCpf('1114447')).toBe('111.444.7');
    expect(maskCpf('1114447773')).toBe('111.444.777-3');
    expect(maskCpf('111444777359999')).toBe('111.444.777-35');
    expect(maskCpf('abc')).toBe('');
  });
});

describe('form validators', () => {
  it('cpfValidator leaves empty to required and flags invalid CPFs', () => {
    expect(cpfValidator(new FormControl(''))).toBeNull();
    expect(cpfValidator(new FormControl('111.444.777-35'))).toBeNull();
    expect(cpfValidator(new FormControl('123.456.789-00'))).toEqual({ cpf: true });
  });

  it('phoneDigitsValidator needs DDD plus number', () => {
    expect(phoneDigitsValidator(new FormControl(''))).toBeNull();
    expect(phoneDigitsValidator(new FormControl('(11) 1234-5678'))).toBeNull();
    expect(phoneDigitsValidator(new FormControl('(11) 91234-5678'))).toBeNull();
    expect(phoneDigitsValidator(new FormControl('(11) 9'))).toEqual({ phoneDigits: true });
  });
});
