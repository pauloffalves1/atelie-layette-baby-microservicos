import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { WishlistItem } from '@shared/core/models/wishlist.model';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';
import { WishlistService } from '@shared/core/services/wishlist.service';

@Component({
  selector: 'app-wishlist-page',
  standalone: true,
  imports: [CurrencyPipe, RouterLink, AssetUrlPipe],
  templateUrl: './wishlist-page.html',
})
export class WishlistPage implements OnInit {
  readonly items = signal<WishlistItem[]>([]);
  readonly loading = signal(true);
  readonly removingId = signal<string | null>(null);

  constructor(private readonly wishlistService: WishlistService) {}

  ngOnInit(): void {
    this.wishlistService.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  remove(item: WishlistItem): void {
    this.removingId.set(item.id);
    this.wishlistService.remove(item.product.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((i) => i.id !== item.id));
        this.removingId.set(null);
      },
      error: () => this.removingId.set(null),
    });
  }
}
