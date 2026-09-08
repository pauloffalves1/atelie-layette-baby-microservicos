import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '@shared/core/services/auth.service';

@Component({
  selector: 'app-verify-email-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './verify-email-page.html',
})
export class VerifyEmailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly success = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.loading.set(false);
      this.errorMessage.set('Link inválido. Solicite um novo e-mail de verificação na sua conta.');
      return;
    }

    this.auth.verifyEmail(token).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(
          err?.error?.detail ?? 'Link inválido ou expirado. Solicite um novo e-mail de verificação na sua conta.',
        );
      },
    });
  }
}
