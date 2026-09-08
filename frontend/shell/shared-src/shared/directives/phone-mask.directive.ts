import { Directive, HostListener, inject } from '@angular/core';
import { NgControl } from '@angular/forms';

/** Formats a Brazilian phone/WhatsApp number as the user types: (11) 91234-5678 or (11) 1234-5678. */
@Directive({
  selector: '[appPhoneMask]',
  standalone: true,
})
export class PhoneMaskDirective {
  private readonly ngControl = inject(NgControl);

  @HostListener('input', ['$event'])
  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const formatted = PhoneMaskDirective.format(input.value);
    input.value = formatted;
    this.ngControl.control?.setValue(formatted, { emitEvent: false });
  }

  private static format(value: string): string {
    const digits = value.replace(/\D/g, '').slice(0, 11);

    if (digits.length === 0) return '';
    if (digits.length <= 2) return `(${digits}`;
    if (digits.length <= 6) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
    if (digits.length <= 10) return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
  }
}
