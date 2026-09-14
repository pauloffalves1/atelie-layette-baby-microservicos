import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, Injector, OnInit, afterNextRender, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, filter, map, of, switchMap, tap } from 'rxjs';
import { AuthService } from '@shared/core/services/auth.service';
import { CepService } from '@shared/core/services/cep.service';
import { CheckoutModalService } from '@shared/core/services/checkout-modal.service';
import { cpfValidator, phoneDigitsValidator } from '@shared/core/utils/br-documents';
import { httpErrorMessage } from '@shared/core/utils/http-error-message';
import { CpfMaskDirective } from '@shared/shared/directives/cpf-mask.directive';
import { PasswordToggleDirective } from '@shared/shared/directives/password-toggle.directive';
import { PhoneMaskDirective } from '@shared/shared/directives/phone-mask.directive';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PhoneMaskDirective, PasswordToggleDirective, CpfMaskDirective],
  templateUrl: './register-page.html',
})
export class RegisterPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly cepService = inject(CepService);
  private readonly checkoutModal = inject(CheckoutModalService);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  /** The API said this e-mail already has an account — offer login/reset instead of a dead end. */
  readonly emailTaken = signal(false);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly returnUrl = signal('/minha-conta');
  readonly cepLoading = signal(false);
  readonly cepError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    cpf: ['', [Validators.required, cpfValidator]],
    phone: ['', [Validators.required, phoneDigitsValidator]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    // Optional at sign-up (checkout asks for the delivery address anyway) — but once any part is
    // filled, the rest becomes required so a half address never gets saved. See ngOnInit.
    zipCode: [''],
    street: [''],
    number: [''],
    complement: [''],
    neighborhood: [''],
    city: [''],
    state: [''],
  });

  private readonly requiredAddressControls = [
    this.form.controls.zipCode,
    this.form.controls.street,
    this.form.controls.number,
    this.form.controls.neighborhood,
    this.form.controls.city,
    this.form.controls.state,
  ];

  readonly addressStarted = signal(false);

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  /** Set when sign-up was reached from the checkout modal (via the login page's link) — reopens the
   * modal at the delivery step after the account is created, same as logging in does. */
  private resumeCheckoutStep: 'delivery' | null = null;
  readonly fromCheckout = signal(false);
  readonly loginQueryParams = computed(() => ({
    returnUrl: this.returnUrl(),
    ...(this.fromCheckoutModal() ? { resumeCheckout: 'delivery' } : {}),
  }));
  private readonly fromCheckoutModal = signal(false);

  ngOnInit(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    if (returnUrl) this.returnUrl.set(returnUrl);

    if (this.route.snapshot.queryParamMap.get('resumeCheckout') === 'delivery') {
      this.resumeCheckoutStep = 'delivery';
      this.fromCheckoutModal.set(true);
    }
    this.fromCheckout.set(this.resumeCheckoutStep !== null || this.returnUrl() === '/checkout');

    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      const started = [...this.requiredAddressControls, this.form.controls.complement].some((c) => c.value.trim() !== '');
      if (started === this.addressStarted()) return;
      this.addressStarted.set(started);
      for (const control of this.requiredAddressControls) {
        control.setValidators(started ? [Validators.required] : []);
        control.updateValueAndValidity({ emitEvent: false });
      }
    });

    this.form.controls.zipCode.valueChanges
      .pipe(
        map((value) => value.replace(/\D/g, '')),
        distinctUntilChanged(),
        tap(() => this.cepError.set(null)),
        filter((digits) => digits.length === 8),
        tap(() => this.cepLoading.set(true)),
        debounceTime(300),
        switchMap((digits) => this.cepService.lookup(digits).pipe(catchError(() => of(null)))),
      )
      .subscribe((address) => {
        this.cepLoading.set(false);

        if (!address || address.erro) {
          this.cepError.set('CEP não encontrado. Confira o número ou preencha o endereço manualmente.');
          return;
        }

        this.form.patchValue({
          street: address.logradouro,
          neighborhood: address.bairro,
          city: address.localidade,
          state: address.uf,
        });
      });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      // Long form — on a phone the first error is usually several screens above the button.
      afterNextRender(
        () => {
          const field = document.querySelector<HTMLElement>('app-register-page .is-invalid');
          field?.scrollIntoView({ behavior: 'smooth', block: 'center' });
          field?.focus({ preventScroll: true });
        },
        { injector: this.injector },
      );
      return;
    }

    const value = this.form.getRawValue();
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.emailTaken.set(false);
    const withAddress = this.addressStarted();

    this.auth
      .register({
        name: value.name,
        email: value.email,
        cpf: value.cpf,
        password: value.password,
        phone: value.phone || null,
        addressStreet: withAddress ? value.street : null,
        addressNumber: withAddress ? value.number : null,
        addressComplement: withAddress ? value.complement || null : null,
        addressNeighborhood: withAddress ? value.neighborhood : null,
        addressCity: withAddress ? value.city : null,
        addressState: withAddress ? value.state : null,
        addressZipCode: withAddress ? value.zipCode : null,
      })
      .subscribe({
        next: () =>
          this.router.navigateByUrl(this.returnUrl()).then(() => {
            if (this.resumeCheckoutStep) this.checkoutModal.open(this.resumeCheckoutStep);
          }),
        error: (err) => {
          this.submitting.set(false);
          const detail = err instanceof HttpErrorResponse ? String(err.error?.detail ?? '') : '';
          this.emailTaken.set(err instanceof HttpErrorResponse && err.status === 409 && detail.includes('e-mail'));
          this.errorMessage.set(httpErrorMessage(err, 'Não foi possível criar sua conta.'));
        },
      });
  }
}
