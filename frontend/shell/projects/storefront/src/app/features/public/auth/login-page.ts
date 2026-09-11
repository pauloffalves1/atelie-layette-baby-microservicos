import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '@shared/core/services/auth.service';
import { CheckoutModalService } from '@shared/core/services/checkout-modal.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login-page.html',
})
export class LoginPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly returnUrl = signal('/minha-conta');

  // Set when this login was triggered from the checkout modal's "Continuar" (see
  // checkout-modal.ts's goToDelivery) — reopens that same modal at the delivery step, with the
  // cart untouched, instead of leaving the customer to re-find the cart icon after logging in.
  private resumeCheckoutStep: 'delivery' | null = null;

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly checkoutModal: CheckoutModalService,
  ) {}

  ngOnInit(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    if (returnUrl) this.returnUrl.set(returnUrl);

    if (this.route.snapshot.queryParamMap.get('resumeCheckout') === 'delivery') this.resumeCheckoutStep = 'delivery';
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.router.navigateByUrl(this.returnUrl()).then(() => {
          if (this.resumeCheckoutStep) this.checkoutModal.open(this.resumeCheckoutStep);
        });
      },
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('E-mail ou senha inválidos.');
      },
    });
  }
}
