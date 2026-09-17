import { Product } from '../models/product.model';
import { buildWhatsappOrderMessage } from './whatsapp-order-message';
import { parseBrl, parseWhatsappOrderMessage } from './whatsapp-order-parser';

const product = (name: string, price: number) => ({ id: name, name, price, effectivePrice: price }) as unknown as Product;

describe('parseWhatsappOrderMessage', () => {
  it('reads back everything the storefront message contains (round trip)', () => {
    const message = buildWhatsappOrderMessage({
      items: [
        { product: product('Fralda de Boca Florzinha', 34.9), quantity: 2, embroideryText: 'ANA, LU', threadColor: 'Rosa Bebê' },
        { product: product('Kit Nuvem', 1250), quantity: 1 },
      ],
      subtotal: 1319.8,
      couponCode: 'BEMVINDO',
      couponDiscount: 10,
      deliveryMethod: 'Entrega',
      address: { street: 'Rua das Flores', number: '10', complement: 'Apto 2', neighborhood: 'Centro', city: 'São Bernardo do Campo', state: 'SP', zipCode: '09751-251' },
      shippingCost: 25.5,
      total: 1335.3,
      customerName: 'Maria Silva',
      customerEmail: 'maria@exemplo.com',
      customerPhone: '(11) 91234-5678',
      notes: 'Entregar à tarde',
      isGift: true,
      recipientName: 'Ana',
      giftMessage: 'Parabéns, mamãe!',
    });

    const parsed = parseWhatsappOrderMessage(message);

    expect(parsed.items).toEqual([
      { name: 'Fralda de Boca Florzinha', quantity: 2, unitPrice: 34.9, embroideryText: 'ANA, LU', threadColor: 'Rosa Bebê' },
      { name: 'Kit Nuvem', quantity: 1, unitPrice: 1250, embroideryText: null, threadColor: null },
    ]);
    expect(parsed.deliveryMethod).toBe('Entrega');
    expect(parsed.address).toEqual({ street: 'Rua das Flores', number: '10', complement: 'Apto 2', neighborhood: 'Centro', city: 'São Bernardo do Campo', state: 'SP', zipCode: '09751-251' });
    expect(parsed.shippingCost).toBe(25.5);
    expect(parsed.couponCode).toBe('BEMVINDO');
    expect(parsed.couponDiscount).toBe(10);
    expect([parsed.customerName, parsed.customerEmail, parsed.customerPhone]).toEqual(['Maria Silva', 'maria@exemplo.com', '(11) 91234-5678']);
    expect([parsed.isGift, parsed.recipientName, parsed.giftMessage]).toEqual([true, 'Ana', 'Parabéns, mamãe!']);
    expect(parsed.notes).toBe('Entregar à tarde');
  });

  it('understands pickup, free shipping and an address to be agreed', () => {
    expect(parseWhatsappOrderMessage('Entrega: retirar no ateliê').deliveryMethod).toBe('Retirada');
    const agreed = parseWhatsappOrderMessage('Entrega: endereço a combinar\nFrete estimado: grátis');
    expect(agreed.deliveryMethod).toBe('Entrega');
    expect(agreed.address).toBeNull();
    expect(agreed.shippingCost).toBe(0);
  });

  it('tolerates Windows line breaks, non-breaking spaces and unrelated lines', () => {
    const parsed = parseWhatsappOrderMessage('Oi! tudo bem?\r\n• 3× Toalha — R$ 90,00\r\nobrigada');
    expect(parsed.items).toEqual([{ name: 'Toalha', quantity: 3, unitPrice: 30, embroideryText: null, threadColor: null }]);
    expect(parsed.customerName).toBeNull();
  });

  it('parses Brazilian currency', () => {
    expect(parseBrl('R$ 1.234,56')).toBe(1234.56);
    expect(parseBrl('R$ 0,90')).toBe(0.9);
    expect(parseBrl('abc')).toBeNull();
  });
});
