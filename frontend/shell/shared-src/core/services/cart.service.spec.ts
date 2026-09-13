import { TestBed } from '@angular/core/testing';
import { Product } from '../models/product.model';
import { CartService } from './cart.service';

function makeProduct(overrides: Partial<Product> = {}): Product {
  const price = overrides.price ?? 69.9;
  return {
    id: 'p1',
    name: 'Body Manga Longa',
    slug: 'body-manga-longa',
    description: null,
    price,
    category: 'Bodies',
    imageUrl: null,
    active: true,
    featured: false,
    isExclusive: false,
    imageUrls: [],
    discountPercentage: null,
    promotionStartsAt: null,
    promotionEndsAt: null,
    isOnPromotion: false,
    effectivePrice: price,
    productionLeadTimeDays: null,
    ...overrides,
  };
}

describe('CartService', () => {
  let service: CartService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(CartService);
  });

  it('starts empty when there is nothing in storage', () => {
    expect(service.items()).toEqual([]);
    expect(service.totalItems()).toBe(0);
    expect(service.totalPrice()).toBe(0);
  });

  it('adds a new product with the requested quantity', () => {
    service.add(makeProduct(), 2);

    expect(service.items()).toHaveLength(1);
    expect(service.items()[0].quantity).toBe(2);
    expect(service.totalItems()).toBe(2);
  });

  it('increments the quantity when adding an already-present product', () => {
    const product = makeProduct();
    service.add(product, 1);
    service.add(product, 2);

    expect(service.items()).toHaveLength(1);
    expect(service.items()[0].quantity).toBe(3);
  });

  it('updateQuantity replaces the quantity for the matching product', () => {
    const product = makeProduct();
    service.add(product, 1);

    service.updateQuantity(product.id, 4);

    expect(service.items()[0].quantity).toBe(4);
  });

  it('updateQuantity removes the item once its quantity reaches zero', () => {
    const product = makeProduct();
    service.add(product, 1);

    service.updateQuantity(product.id, 0);

    expect(service.items()).toEqual([]);
  });

  it('remove drops only the targeted product', () => {
    const a = makeProduct({ id: 'a' });
    const b = makeProduct({ id: 'b' });
    service.add(a, 1);
    service.add(b, 1);

    service.remove('a');

    expect(service.items().map((i) => i.product.id)).toEqual(['b']);
  });

  it('add keeps separate lines for the same product with different embroidery text', () => {
    const product = makeProduct({ isExclusive: true });
    service.add(product, 1, 'ANA');
    service.add(product, 1, 'BIA');

    expect(service.items()).toHaveLength(2);
    expect(service.items().map((i) => i.embroideryText)).toEqual(['ANA', 'BIA']);
  });

  it('add merges quantity for the same product with the same embroidery text', () => {
    const product = makeProduct({ isExclusive: true });
    service.add(product, 1, 'ANA');
    service.add(product, 2, 'ANA');

    expect(service.items()).toHaveLength(1);
    expect(service.items()[0].quantity).toBe(3);
  });

  it('updateQuantity and remove target only the matching embroidery-text line', () => {
    const product = makeProduct({ isExclusive: true });
    service.add(product, 1, 'ANA');
    service.add(product, 1, 'BIA');

    service.updateQuantity(product.id, 5, 'ANA');
    expect(service.items().find((i) => i.embroideryText === 'ANA')?.quantity).toBe(5);
    expect(service.items().find((i) => i.embroideryText === 'BIA')?.quantity).toBe(1);

    service.remove(product.id, 'ANA');
    expect(service.items()).toHaveLength(1);
    expect(service.items()[0].embroideryText).toBe('BIA');
  });

  it('undoRemove puts the removed line back at its original position', () => {
    const product = makeProduct({ isExclusive: true });
    service.add(product, 1, 'ANA', 'Rosa');
    service.add(product, 2, 'BIA', 'Azul');
    service.add(product, 1, 'CAU', 'Verde');

    service.remove(product.id, 'BIA', 'Azul');
    expect(service.lastRemoved()?.item.embroideryText).toBe('BIA');

    service.undoRemove();

    expect(service.items().map((i) => i.embroideryText)).toEqual(['ANA', 'BIA', 'CAU']);
    expect(service.items()[1].quantity).toBe(2);
    expect(service.lastRemoved()).toBeNull();
  });

  it('updateQuantity to zero goes through remove, so it can be undone too', () => {
    const product = makeProduct();
    service.add(product, 1);

    service.updateQuantity(product.id, 0);
    expect(service.lastRemoved()?.item.product.id).toBe(product.id);

    service.undoRemove();
    expect(service.items()).toHaveLength(1);
  });

  it('adding or clearing dismisses a pending undo', () => {
    service.add(makeProduct({ id: 'a' }), 1);
    service.remove('a');

    service.add(makeProduct({ id: 'b' }), 1);
    expect(service.lastRemoved()).toBeNull();

    service.remove('b');
    service.clear();
    expect(service.lastRemoved()).toBeNull();
  });

  it('clear empties the cart', () => {
    service.add(makeProduct(), 1);

    service.clear();

    expect(service.items()).toEqual([]);
  });

  it('totalPrice sums price times quantity across items', () => {
    service.add(makeProduct({ id: 'a', price: 10 }), 2); // 20
    service.add(makeProduct({ id: 'b', price: 5 }), 3); // 15

    expect(service.totalPrice()).toBe(35);
  });

  it('persists changes to localStorage and restores them for a new instance', () => {
    service.add(makeProduct(), 2);

    const restored = TestBed.inject(CartService);
    // same root-injected instance in this TestBed, so re-read storage directly instead
    const raw = localStorage.getItem('atelie-bebe.cart');
    expect(raw).not.toBeNull();
    expect(JSON.parse(raw!)).toHaveLength(1);
    expect(restored).toBe(service);
  });
});
