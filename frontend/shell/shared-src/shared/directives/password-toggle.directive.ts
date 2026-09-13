import { Directive, ElementRef, OnDestroy, OnInit, Renderer2, inject } from '@angular/core';

/**
 * Adds a "mostrar/ocultar senha" button right after a password input. Put the input inside a
 * Bootstrap `.input-group` (use `.has-validation` when it shows `.invalid-feedback`, rendered
 * after the group with `d-block`). The button is a real, labelled toggle (aria-pressed), and the
 * field goes back to hidden when its form is submitted, so a revealed password isn't left on screen.
 */
@Directive({
  selector: 'input[appPasswordToggle]',
  standalone: true,
})
export class PasswordToggleDirective implements OnInit, OnDestroy {
  private readonly input = inject<ElementRef<HTMLInputElement>>(ElementRef).nativeElement;
  private readonly renderer = inject(Renderer2);
  private button!: HTMLButtonElement;
  private icon!: HTMLElement;
  private visible = false;
  private readonly unlisteners: (() => void)[] = [];

  ngOnInit(): void {
    this.button = this.renderer.createElement('button');
    this.renderer.setAttribute(this.button, 'type', 'button');
    this.renderer.addClass(this.button, 'btn');
    this.renderer.addClass(this.button, 'btn-outline-secondary');
    this.renderer.addClass(this.button, 'password-toggle');
    this.icon = this.renderer.createElement('i');
    this.renderer.setAttribute(this.icon, 'aria-hidden', 'true');
    this.renderer.appendChild(this.button, this.icon);
    this.renderer.insertBefore(this.input.parentNode, this.button, this.input.nextSibling);

    this.unlisteners.push(this.renderer.listen(this.button, 'click', () => this.setVisible(!this.visible)));
    const form = this.input.form;
    if (form) this.unlisteners.push(this.renderer.listen(form, 'submit', () => this.setVisible(false)));

    this.setVisible(false);
  }

  ngOnDestroy(): void {
    this.unlisteners.forEach((unlisten) => unlisten());
  }

  private setVisible(visible: boolean): void {
    this.visible = visible;
    this.renderer.setAttribute(this.input, 'type', visible ? 'text' : 'password');
    this.renderer.setAttribute(this.button, 'aria-label', visible ? 'Ocultar senha' : 'Mostrar senha');
    this.renderer.setAttribute(this.button, 'title', visible ? 'Ocultar senha' : 'Mostrar senha');
    this.renderer.setAttribute(this.button, 'aria-pressed', String(visible));
    this.renderer.setAttribute(this.icon, 'class', visible ? 'bi bi-eye-slash' : 'bi bi-eye');
  }
}
