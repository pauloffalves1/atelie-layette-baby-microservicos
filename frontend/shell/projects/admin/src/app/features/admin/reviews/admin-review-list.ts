import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { AdminProductReview } from '@shared/core/models/review.model';
import { ReviewService } from '@shared/core/services/review.service';
import { Pagination } from '@shared/shared/components/pagination/pagination';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';

type ApprovalFilter = 'pending' | 'approved' | 'all';

@Component({
  selector: 'app-admin-review-list',
  standalone: true,
  imports: [DatePipe, Pagination, AssetUrlPipe],
  templateUrl: './admin-review-list.html',
})
export class AdminReviewList implements OnInit {
  readonly reviews = signal<AdminProductReview[]>([]);
  readonly loading = signal(true);
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly filter = signal<ApprovalFilter>('pending');
  readonly busyId = signal<string | null>(null);

  constructor(private readonly reviewService: ReviewService) {}

  ngOnInit(): void {
    this.load();
  }

  setFilter(filter: ApprovalFilter): void {
    if (this.filter() === filter) return;
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
    this.reviewService.approve(review.id).subscribe({
      next: () => {
        this.busyId.set(null);
        this.load();
      },
      error: () => this.busyId.set(null),
    });
  }

  reject(review: AdminProductReview): void {
    if (this.busyId()) return;
    const confirmed = confirm(`Rejeitar e remover permanentemente a avaliação de "${review.customerName}"?`);
    if (!confirmed) return;

    this.busyId.set(review.id);
    this.reviewService.reject(review.id).subscribe({
      next: () => {
        this.busyId.set(null);
        this.load();
      },
      error: () => this.busyId.set(null),
    });
  }

  private load(): void {
    this.loading.set(true);
    const approved = this.filter() === 'pending' ? false : this.filter() === 'approved' ? true : null;
    this.reviewService.listForAdmin(approved, this.page()).subscribe({
      next: (result) => {
        this.reviews.set(result.items);
        this.totalPages.set(result.totalPages);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
