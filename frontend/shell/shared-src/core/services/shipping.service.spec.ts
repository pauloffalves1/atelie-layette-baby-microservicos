import { ShippingService } from './shipping.service';

/**
 * Posting weights agreed with the ateliê: one piece is quoted at 500 g, a kit at 1 kg, and each
 * extra kit adds its own kilo. São Paulo's base rate is R$ 12,90 and every extra half kilo adds
 * 22% of it (R$ 2,838), which is where the expected values below come from.
 */
describe('ShippingService.estimate', () => {
  const service = new ShippingService();
  const subtotal = 100;

  it('quotes a single piece at the destination base rate', () => {
    expect(service.estimate('SP', [{ category: 'Fralda de Boca', quantity: 1 }], subtotal)).toBe(12.9);
  });

  it('charges one extra step for two pieces (1 kg)', () => {
    expect(service.estimate('SP', [{ category: 'Fralda de Ombro', quantity: 2 }], subtotal)).toBe(15.74);
  });

  it('quotes a kit as a whole kilo', () => {
    expect(service.estimate('SP', [{ category: 'Kit Ombro e Boca', quantity: 1 }], subtotal)).toBe(15.74);
  });

  it('adds a kilo per kit', () => {
    expect(service.estimate('SP', [{ category: 'Kit Ombro, Boca e Toalha', quantity: 2 }], subtotal)).toBe(21.41);
  });

  it('quotes a whole kilo for the categories that are not named "Kit ..." but ship like one', () => {
    const kit = service.estimate('SP', [{ category: 'Kit Ombro e Boca', quantity: 1 }], subtotal);

    expect(service.estimate('SP', [{ category: 'Boca, Ombro e Maternidade', quantity: 1 }], subtotal)).toBe(kit);
    expect(service.estimate('SP', [{ category: 'Toalha', quantity: 1 }], subtotal)).toBe(kit);
  });

  it('still quotes half a kilo for the other single pieces', () => {
    expect(service.estimate('SP', [{ category: 'Almofadinha', quantity: 1 }], subtotal)).toBe(12.9);
  });

  it('charges more for a farther destination', () => {
    const sp = service.estimate('SP', [{ category: 'Kit Ombro e Boca', quantity: 1 }], subtotal);
    const am = service.estimate('AM', [{ category: 'Kit Ombro e Boca', quantity: 1 }], subtotal);
    expect(am).toBeGreaterThan(sp);
  });

  it('is free once the destination threshold is reached', () => {
    expect(service.estimate('SP', [{ category: 'Kit Ombro e Boca', quantity: 5 }], 599)).toBe(0);
    expect(service.estimate('SP', [{ category: 'Kit Ombro e Boca', quantity: 5 }], 399, 'São Bernardo do Campo')).toBe(0);
  });
});
