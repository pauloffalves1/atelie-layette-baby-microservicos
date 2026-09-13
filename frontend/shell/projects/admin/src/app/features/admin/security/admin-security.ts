import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TwoFactorSetup } from '@shared/core/models/auth.model';
import { AdminAuthService } from '@shared/core/services/admin-auth.service';
import { httpErrorMessage } from '@shared/core/utils/http-error-message';
import { LoadError } from '@shared/shared/components/load-error/load-error';
import { PixQrCode } from '@shared/shared/components/pix-qr-code/pix-qr-code';
import { PasswordToggleDirective } from '@shared/shared/directives/password-toggle.directive';

@Component({
  selector: 'app-admin-security',
  standalone: true,
  imports: [FormsModule, PixQrCode, PasswordToggleDirective, LoadError],
  templateUrl: './admin-security.html',
})
export class AdminSecurity implements OnInit {
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly setup = signal<TwoFactorSetup | null>(null);
  readonly settingUp = signal(false);
  readonly enabling = signal(false);
  readonly confirmCode = signal('');
  readonly enableError = signal<string | null>(null);
  readonly enabled = signal(false);
  readonly setupError = signal<string | null>(null);
  readonly secretCopied = signal(false);

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
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.auth.getTwoFactorStatus().subscribe({
      next: ({ enabled }) => {
        this.enabled.set(enabled);
        this.loading.set(false);
      },
      // Without this, a failed status check looked exactly like "2FA is off" and offered to enable it.
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      },
    });
  }

  startSetup(): void {
    this.settingUp.set(true);
    this.enableError.set(null);
    this.setupError.set(null);
    this.auth.beginTwoFactorSetup().subscribe({
      next: (setup) => {
        this.setup.set(setup);
        this.settingUp.set(false);
      },
      error: (err) => {
        this.settingUp.set(false);
        this.setupError.set(httpErrorMessage(err, 'Não foi possível iniciar a configuração agora.'));
      },
    });
  }

  /** Keeps only digits in the 6-digit code field (setting the element too, so typed letters vanish). */
  onCodeInput(input: HTMLInputElement): void {
    const digits = input.value.replace(/\D/g, '').slice(0, 6);
    input.value = digits;
    this.confirmCode.set(digits);
    this.enableError.set(null);
  }

  copySecret(): void {
    const secret = this.setup()?.secret;
    if (!secret) return;
    navigator.clipboard.writeText(secret).then(() => {
      this.secretCopied.set(true);
      setTimeout(() => this.secretCopied.set(false), 2000);
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
        this.enableError.set(httpErrorMessage(err, 'Código inválido.', 'Código inválido ou expirado — use o código que aparece agora no aplicativo.'));
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
        this.disableError.set(httpErrorMessage(err, 'Senha incorreta.', 'Senha incorreta.'));
      },
    });
  }

  changePassword(): void {
    if (!this.currentPassword() || !this.newPassword()) {
      this.passwordError.set('Preencha a senha atual e a nova senha.');
      return;
    }
    if (this.newPassword() === this.currentPassword()) {
      this.passwordError.set('A nova senha precisa ser diferente da atual.');
      return;
    }

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
        this.passwordError.set(httpErrorMessage(err, 'Não foi possível alterar a senha.', 'Senha atual incorreta.'));
      },
    });
  }
}
