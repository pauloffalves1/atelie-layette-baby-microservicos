import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { SITE_NAME } from '@shared/core/constants/site';
import { ContactMessage, ContactService } from '@shared/core/services/contact.service';
import { whatsappUrl } from '@shared/core/utils/contact-links';
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

  readonly draftingReplyId = signal<string | null>(null);
  readonly draftedReplies = signal<Record<string, string>>({});
  readonly replyErrorId = signal<string | null>(null);

  constructor(private readonly contactService: ContactService) {}

  ngOnInit(): void {
    this.load();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  suggestReply(message: ContactMessage): void {
    this.draftingReplyId.set(message.id);
    this.replyErrorId.set(null);
    this.contactService.suggestReply(message.name, message.message).subscribe({
      next: ({ reply }) => {
        this.draftedReplies.update((current) => ({ ...current, [message.id]: reply }));
        this.draftingReplyId.set(null);
      },
      error: () => {
        this.draftingReplyId.set(null);
        this.replyErrorId.set(message.id);
      },
    });
  }

  readonly copiedReplyId = signal<string | null>(null);

  whatsappLink(message: ContactMessage): string | null {
    return whatsappUrl(message.phone, this.draftedReplies()[message.id]);
  }

  mailtoLink(message: ContactMessage): string {
    const body = this.draftedReplies()[message.id];
    const params = new URLSearchParams({ subject: `Re: seu contato com o ${SITE_NAME}` });
    if (body) params.set('body', body);
    // URLSearchParams encodes spaces as "+", which mail clients show literally in mailto bodies.
    return `mailto:${message.email}?${params.toString().replace(/\+/g, '%20')}`;
  }

  copyReply(message: ContactMessage): void {
    const reply = this.draftedReplies()[message.id];
    if (!reply) return;
    navigator.clipboard.writeText(reply).then(() => {
      this.copiedReplyId.set(message.id);
      setTimeout(() => this.copiedReplyId.set(null), 2000);
    });
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
