import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { NewsletterSubscriber } from '@shared/core/models/newsletter.model';
import { NewsletterService } from '@shared/core/services/newsletter.service';
import { LoadError } from '@shared/shared/components/load-error/load-error';

@Component({
  selector: 'app-admin-newsletter',
  standalone: true,
  imports: [DatePipe, LoadError],
  templateUrl: './admin-newsletter.html',
})
export class AdminNewsletter implements OnInit {
  readonly subscribers = signal<NewsletterSubscriber[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly exporting = signal(false);

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

  exportCsv(): void {
    this.exporting.set(true);
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
      error: () => this.exporting.set(false),
    });
  }
}
