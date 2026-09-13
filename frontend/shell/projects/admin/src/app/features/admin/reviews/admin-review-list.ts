import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { AdminProductReview } from '@shared/core/models/review.model';
import { ReviewService } from '@shared/core/services/review.service';
import { httpErrorMessage } from '@shared/core/utils/http-error-message';
import { Pagination } from '@shared/shared/components/pagination/pagination';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';
import { LoadError } from '@shared/shared/components/load-error/load-error';

type ApprovalFilter = 'pending' | 'approved' | 'all';

@Component({
  selector: 'app-admin-review-list',
  standalone: true,
  imports: [DatePipe, Pagination, AssetUrlPipe, LoadError],
  templateUrl: './admin-review-list.html',
})
export class AdminReviewList implements OnInit {
  readonly reviews = signal<AdminProductReview[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly filter = signal<ApprovalFilter>('pending');
  readonly busyId = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly actionMessage = signal<string | null>(null);

  constructor(private readonly reviewService: ReviewService) {}

  ngOnInit(): void {
    this.load();
  }

  setFilter(filter: ApprovalFilter): void {
    if (this.filter() === filter) return;
    this.clearFeedback();
    this.filter.set(filter);
    this.page.set(1);
    this.load();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  approve(review: AdminProductReview): void {
    if (this.busyId()) return;
    this.busyId.set(review.id);
    this.clearFeedback();
    this.reviewService.approve(review.id).subscribe({
      next: () => {
        this.busyId.set(null);
        // Updated in place instead of reloading the page: the reload flashed a spinner over the whole
        // list after every click, which is slow going through a queue of pending reviews.
        if (this.filter() === 'pending') this.removeLocally(review.id);
        else this.reviews.update((list) => list.map((r) => (r.id === review.id ? { ...r, approved: true } : r)));
        this.actionMessage.set(`Avaliação de ${review.customerName} aprovada — já aparece na página do produto.`);
      },
      error: (err) => {
        this.busyId.set(null);
        this.actionError.set(httpErrorMessage(err, `Não foi possível aprovar a avaliação de ${review.customerName}.`));
      },
    });
  }

  reject(review: AdminProductReview): void {
    if (this.busyId()) return;
    const confirmed = confirm(`Rejeitar e remover permanentemente a avaliação de "${review.customerName}"?`);
    if (!confirmed) return;

    this.busyId.set(review.id);
    this.clearFeedback();
    this.reviewService.reject(review.id).subscribe({
      next: () => {
        this.busyId.set(null);
        this.removeLocally(review.id);
        this.actionMessage.set(`Avaliação de ${review.customerName} removida.`);
      },
      error: (err) => {
        this.busyId.set(null);
        this.actionError.set(httpErrorMessage(err, `Não foi possível remover a avaliação de ${review.customerName}.`));
      },
    });
  }

  dismissFeedback(): void {
    this.clearFeedback();
  }

  private clearFeedback(): void {
    this.actionError.set(null);
    this.actionMessage.set(null);
  }

  /** Last card of a page gone — fetch again so the next page's reviews slide in (or step back a page). */
  private removeLocally(id: string): void {
    this.reviews.update((list) => list.filter((r) => r.id !== id));
    if (this.reviews().length === 0) {
      if (this.page() > 1) this.page.update((p) => p - 1);
      this.load();
    }
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    const approved = this.filter() === 'pending' ? false : this.filter() === 'approved' ? true : null;
    this.reviewService.listForAdmin(approved, this.page()).subscribe({
      next: (result) => {
        this.reviews.set(result.items);
        this.totalPages.set(result.totalPages);
        this.loading.set(false);
      },
      error: () => {
          this.loading.set(false);
          this.loadError.set(true);
        },
    });
  }
}
