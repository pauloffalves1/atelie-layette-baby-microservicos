import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { NewsletterSubscriber } from '@shared/core/models/newsletter.model';
import { NewsletterService } from '@shared/core/services/newsletter.service';

@Component({
  selector: 'app-admin-newsletter',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './admin-newsletter.html',
})
export class AdminNewsletter implements OnInit {
  readonly subscribers = signal<NewsletterSubscriber[]>([]);
  readonly loading = signal(true);
  readonly exporting = signal(false);

  constructor(private readonly newsletterService: NewsletterService) {}

  ngOnInit(): void {
    this.newsletterService.list().subscribe({
      next: (subscribers) => {
        this.subscribers.set(subscribers);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
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
