import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TwoFactorSetup } from '@shared/core/models/auth.model';
import { AdminAuthService } from '@shared/core/services/admin-auth.service';

@Component({
  selector: 'app-admin-security',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './admin-security.html',
})
export class AdminSecurity implements OnInit {
  readonly loading = signal(true);
  readonly setup = signal<TwoFactorSetup | null>(null);
  readonly settingUp = signal(false);
  readonly enabling = signal(false);
  readonly confirmCode = signal('');
  readonly enableError = signal<string | null>(null);
  readonly enabled = signal(false);

  readonly disabling = signal(false);
  readonly disablePassword = signal('');
  readonly disableError = signal<string | null>(null);
  readonly confirmingDisable = signal(false);

  readonly currentPassword = signal('');
  readonly newPassword = signal('');
  readonly changingPassword = signal(false);
  readonly passwordError = signal<string | null>(null);
  readonly passwordSuccess = signal(false);

  constructor(private readonly auth: AdminAuthService) {}

  ngOnInit(): void {
    this.auth.getTwoFactorStatus().subscribe({
      next: ({ enabled }) => {
        this.enabled.set(enabled);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  startSetup(): void {
    this.settingUp.set(true);
    this.enableError.set(null);
    this.auth.beginTwoFactorSetup().subscribe({
      next: (setup) => {
        this.setup.set(setup);
        this.settingUp.set(false);
      },
      error: () => this.settingUp.set(false),
    });
  }

  confirmEnable(): void {
    const setup = this.setup();
    if (!setup || !this.confirmCode()) return;

    this.enabling.set(true);
    this.enableError.set(null);

    this.auth.enableTwoFactor(setup.secret, this.confirmCode()).subscribe({
      next: () => {
        this.enabling.set(false);
        this.enabled.set(true);
        this.setup.set(null);
        this.confirmCode.set('');
      },
      error: (err) => {
        this.enabling.set(false);
        this.enableError.set(err?.error?.detail ?? 'Código inválido.');
      },
    });
  }

  cancelSetup(): void {
    this.setup.set(null);
    this.confirmCode.set('');
    this.enableError.set(null);
  }

  startDisable(): void {
    this.confirmingDisable.set(true);
    this.disableError.set(null);
  }

  cancelDisable(): void {
    this.confirmingDisable.set(false);
    this.disablePassword.set('');
    this.disableError.set(null);
  }

  confirmDisable(): void {
    if (!this.disablePassword()) return;

    this.disabling.set(true);
    this.disableError.set(null);

    this.auth.disableTwoFactor(this.disablePassword()).subscribe({
      next: () => {
        this.disabling.set(false);
        this.enabled.set(false);
        this.confirmingDisable.set(false);
        this.disablePassword.set('');
      },
      error: (err) => {
        this.disabling.set(false);
        this.disableError.set(err?.error?.detail ?? 'Senha incorreta.');
      },
    });
  }

  changePassword(): void {
    if (!this.currentPassword() || !this.newPassword()) return;

    this.changingPassword.set(true);
    this.passwordError.set(null);
    this.passwordSuccess.set(false);

    this.auth.changePassword(this.currentPassword(), this.newPassword()).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.passwordSuccess.set(true);
        this.currentPassword.set('');
        this.newPassword.set('');
      },
      error: (err) => {
        this.changingPassword.set(false);
        this.passwordError.set(err?.error?.detail ?? 'Não foi possível alterar a senha.');
      },
    });
  }
}
