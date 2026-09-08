import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminAuthService } from '@shared/core/services/admin-auth.service';

@Component({
  selector: 'app-admin-login',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule],
  templateUrl: './admin-login.html',
})
export class AdminLogin {
  private readonly fb = inject(FormBuilder);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly awaitingTwoFactor = signal(false);
  readonly twoFactorCode = signal('');

  private pendingAdminId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  constructor(
    private readonly auth: AdminAuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: (response) => {
        this.submitting.set(false);
        if (response.requiresTwoFactor && response.adminId) {
          this.pendingAdminId = response.adminId;
          this.awaitingTwoFactor.set(true);
        } else {
          this.router.navigate(['/admin/dashboard']);
        }
      },
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('E-mail ou senha inválidos.');
      },
    });
  }

  submitTwoFactor(): void {
    if (!this.pendingAdminId || !this.twoFactorCode()) return;

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.auth.verifyTwoFactor(this.pendingAdminId, this.twoFactorCode()).subscribe({
      next: () => this.router.navigate(['/admin/dashboard']),
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('Código inválido.');
      },
    });
  }
}
