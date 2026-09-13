import { Component, Injector, OnInit, afterNextRender, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, filter, map, of, switchMap, tap } from 'rxjs';
import { AuthService } from '@shared/core/services/auth.service';
import { CepService } from '@shared/core/services/cep.service';
import { CheckoutModalService } from '@shared/core/services/checkout-modal.service';
import { httpErrorMessage } from '@shared/core/utils/http-error-message';
import { PasswordToggleDirective } from '@shared/shared/directives/password-toggle.directive';
import { PhoneMaskDirective } from '@shared/shared/directives/phone-mask.directive';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PhoneMaskDirective, PasswordToggleDirective],
  templateUrl: './register-page.html',
})
export class RegisterPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly cepService = inject(CepService);
  private readonly checkoutModal = inject(CheckoutModalService);
  private readonly injector = inject(Injector);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly returnUrl = signal('/minha-conta');
  readonly cepLoading = signal(false);
  readonly cepError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    cpf: ['', [Validators.required, Validators.pattern(/^\d{3}\.?\d{3}\.?\d{3}-?\d{2}$/)]],
    phone: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(6)]],
    zipCode: ['', Validators.required],
    street: ['', Validators.required],
    number: ['', Validators.required],
    complement: [''],
    neighborhood: ['', Validators.required],
    city: ['', Validators.required],
    state: ['', Validators.required],
  });

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

    this.auth
      .register({
        name: value.name,
        email: value.email,
        cpf: value.cpf,
        password: value.password,
        phone: value.phone || null,
        addressStreet: value.street,
        addressNumber: value.number,
        addressComplement: value.complement || null,
        addressNeighborhood: value.neighborhood,
        addressCity: value.city,
        addressState: value.state,
        addressZipCode: value.zipCode,
      })
      .subscribe({
        next: () =>
          this.router.navigateByUrl(this.returnUrl()).then(() => {
            if (this.resumeCheckoutStep) this.checkoutModal.open(this.resumeCheckoutStep);
          }),
        error: (err) => {
          this.submitting.set(false);
          this.errorMessage.set(httpErrorMessage(err, 'Não foi possível criar sua conta.'));
        },
      });
  }
}
