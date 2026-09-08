import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ContactMessage, ContactService } from '@shared/core/services/contact.service';
import { Pagination } from '@shared/shared/components/pagination/pagination';

@Component({
  selector: 'app-admin-contact-messages',
  standalone: true,
  imports: [DatePipe, Pagination],
  templateUrl: './admin-contact-messages.html',
})
export class AdminContactMessages implements OnInit {
  readonly messages = signal<ContactMessage[]>([]);
  readonly loading = signal(true);
  readonly page = signal(1);
  readonly totalPages = signal(0);

  constructor(private readonly contactService: ContactService) {}

  ngOnInit(): void {
    this.load();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.contactService.listForAdmin(this.page()).subscribe({
      next: (result) => {
        this.messages.set(result.items);
        this.totalPages.set(result.totalPages);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
