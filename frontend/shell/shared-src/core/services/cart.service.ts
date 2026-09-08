import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { environment } from '@shared/environment';
import { CartItem } from '../models/cart.model';
import { Product } from '../models/product.model';
import { AuthService } from './auth.service';

const STORAGE_KEY = 'atelie-bebe.cart';
const SYNC_DEBOUNCE_MS = 2000;

function normalize(value?: string | null): string | null {
  return value ?? null;
}

function matches(item: CartItem, productId: string, embroideryText?: string | null, threadColor?: string | null): boolean {
  return (
    item.product.id === productId &&
    normalize(item.embroideryText) === normalize(embroideryText) &&
    normalize(item.threadColor) === normalize(threadColor)
  );
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private syncTimeout: ReturnType<typeof setTimeout> | undefined;

  private readonly itemsSignal = signal<CartItem[]>(this.readStoredCart());

  readonly items = this.itemsSignal.asReadonly();
  readonly totalItems = computed(() => this.itemsSignal().reduce((sum, item) => sum + item.quantity, 0));
  readonly totalPrice = computed(() =>
    this.itemsSignal().reduce((sum, item) => sum + item.product.effectivePrice * item.quantity, 0),
  );

  constructor() {
    // Picks up anything added while logged out (or before this tab loaded) as soon as we know
    // there's an authenticated customer to attribute the cart to.
    if (this.auth.isAuthenticated()) this.scheduleSync();
  }

  add(product: Product, quantity = 1, embroideryText?: string | null, threadColor?: string | null): void {
    const items = [...this.itemsSignal()];
    const existing = items.find((i) => matches(i, product.id, embroideryText, threadColor));

    if (existing) {
      existing.quantity += quantity;
    } else {
      items.push({ product, quantity, embroideryText: normalize(embroideryText), threadColor: normalize(threadColor) });
    }

    this.persist(items);
  }

  updateQuantity(productId: string, quantity: number, embroideryText?: string | null, threadColor?: string | null): void {
    const items = this.itemsSignal()
      .map((item) => (matches(item, productId, embroideryText, threadColor) ? { ...item, quantity } : item))
      .filter((item) => item.quantity > 0);
    this.persist(items);
  }

  remove(productId: string, embroideryText?: string | null, threadColor?: string | null): void {
    this.persist(this.itemsSignal().filter((item) => !matches(item, productId, embroideryText, threadColor)));
  }

  clear(): void {
    this.persist([]);
  }

  private persist(items: CartItem[]): void {
    this.itemsSignal.set(items);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
    if (this.auth.isAuthenticated()) this.scheduleSync();
  }

  private readStoredCart(): CartItem[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as CartItem[]) : [];
    } catch {
      return [];
    }
  }

  /**
   * Debounced push of the current cart to the server, purely so an abandoned-cart reminder can
   * be sent later — the cart is never read back from the server into the UI.
   */
  private scheduleSync(): void {
    if (this.syncTimeout) clearTimeout(this.syncTimeout);
    this.syncTimeout = setTimeout(() => {
      const items = this.itemsSignal().map((item) => ({
        productId: item.product.id,
        quantity: item.quantity,
        embroideryText: item.embroideryText ?? null,
        threadColor: item.threadColor ?? null,
      }));
      this.http.put(`${environment.apiUrl}/cart-sync`, { items }).subscribe({ error: () => {} });
    }, SYNC_DEBOUNCE_MS);
  }
}
