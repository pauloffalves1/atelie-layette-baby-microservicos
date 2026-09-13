import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { WishlistItem } from '@shared/core/models/wishlist.model';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';
import { WishlistService } from '@shared/core/services/wishlist.service';
import { LoadError } from '@shared/shared/components/load-error/load-error';

@Component({
  selector: 'app-wishlist-page',
  standalone: true,
  imports: [CurrencyPipe, RouterLink, AssetUrlPipe, LoadError],
  templateUrl: './wishlist-page.html',
})
export class WishlistPage implements OnInit {
  readonly items = signal<WishlistItem[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly removeError = signal<string | null>(null);
  readonly removingId = signal<string | null>(null);

  constructor(private readonly wishlistService: WishlistService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.wishlistService.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      },
    });
  }

  remove(item: WishlistItem): void {
    this.removingId.set(item.id);
    this.removeError.set(null);
    this.wishlistService.remove(item.product.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((i) => i.id !== item.id));
        this.removingId.set(null);
      },
      error: () => {
        this.removingId.set(null);
        this.removeError.set(`Não foi possível remover "${item.product.name}" dos favoritos. Tente de novo.`);
      },
    });
  }
}
