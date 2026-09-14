import { Directive, HostListener, inject } from '@angular/core';
import { NgControl } from '@angular/forms';
import { maskCpf } from '../../core/utils/br-documents';

/** Formats a CPF as the user types: 000.000.000-00 (same approach as PhoneMaskDirective). */
@Directive({
  selector: '[appCpfMask]',
  standalone: true,
  host: { inputmode: 'numeric', maxlength: '14' },
})
export class CpfMaskDirective {
  private readonly ngControl = inject(NgControl);

  @HostListener('input', ['$event'])
  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const formatted = maskCpf(input.value);
    input.value = formatted;
    this.ngControl.control?.setValue(formatted, { emitEvent: false });
  }
}
