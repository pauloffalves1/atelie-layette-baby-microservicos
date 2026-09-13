import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NewsletterSubscriber } from '@shared/core/models/newsletter.model';
import { NewsletterService } from '@shared/core/services/newsletter.service';
import { httpErrorMessage } from '@shared/core/utils/http-error-message';
import { LoadError } from '@shared/shared/components/load-error/load-error';

@Component({
  selector: 'app-admin-newsletter',
  standalone: true,
  imports: [DatePipe, LoadError, FormsModule],
  templateUrl: './admin-newsletter.html',
})
export class AdminNewsletter implements OnInit {
  readonly subscribers = signal<NewsletterSubscriber[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly exporting = signal(false);
  readonly exportError = signal<string | null>(null);
  readonly search = signal('');
  readonly copied = signal(false);

  readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    const list = this.subscribers();
    return term ? list.filter((s) => s.email.toLowerCase().includes(term)) : list;
  });

  constructor(private readonly newsletterService: NewsletterService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.newsletterService.list().subscribe({
      next: (subscribers) => {
        this.subscribers.set(subscribers);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      },
    });
  }

  /** One address per line — pastes straight into the BCC field or a campaign tool's import box. */
  copyEmails(): void {
    const text = this.filtered().map((s) => s.email).join('\n');
    navigator.clipboard?.writeText(text).then(
      () => {
        this.copied.set(true);
        setTimeout(() => this.copied.set(false), 2000);
      },
      () => this.exportError.set('O navegador bloqueou a cópia. Use "Exportar CSV".'),
    );
  }

  exportCsv(): void {
    this.exporting.set(true);
    this.exportError.set(null);
    this.newsletterService.exportCsv().subscribe({
      next: (blob) => {
        this.exporting.set(false);
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `newsletter-${new Date().toISOString().slice(0, 10)}.csv`;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.exporting.set(false);
        this.exportError.set(httpErrorMessage(err, 'Não foi possível gerar o CSV.'));
      },
    });
  }
}
