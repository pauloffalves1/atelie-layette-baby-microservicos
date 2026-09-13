import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

/** Admin catch-all — an unknown /admin/... URL used to bounce silently to the dashboard. */
@Component({
  selector: 'app-admin-not-found',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="p-3 p-md-4 p-lg-5">
      <div class="stat-card text-center py-5 mx-auto" style="max-width: 32rem;">
        <i class="bi bi-signpost-split fs-1 d-block mb-3 text-secondary" aria-hidden="true"></i>
        <h2 class="h4 mb-2">Página não encontrada</h2>
        <p class="text-secondary small mb-4">
          <span class="font-monospace text-break">{{ path }}</span> não existe no painel.
        </p>
        <a routerLink="/admin/dashboard" class="btn btn-primary rounded-pill">Ir para o Dashboard</a>
      </div>
    </div>
  `,
})
export class AdminNotFound {
  readonly path = inject(Router).url;
}
