import { Pipe, PipeTransform } from '@angular/core';

/** Masks a CPF for display, keeping only the middle block visible (e.g. ***.982.247-**). */
@Pipe({ name: 'cpfMask', standalone: true })
export class CpfMaskPipe implements PipeTransform {
  transform(cpf: string | null | undefined): string {
    if (!cpf) return '—';

    const digits = cpf.replace(/\D/g, '');
    if (digits.length !== 11) return cpf;

    return `***.${digits.slice(3, 6)}.${digits.slice(6, 9)}-**`;
  }
}
