import { Product } from '../models/product.model';
import { WhatsappOrderDraft, buildWhatsappOrderMessage } from './whatsapp-order-message';

const product = (name: string, price: number) => ({ id: name, name, price, effectivePrice: price }) as unknown as Product;

const baseDraft = (): WhatsappOrderDraft => ({
  items: [
    { product: product('Fralda de Boca Florzinha', 34.9), quantity: 2, embroideryText: 'ANA', threadColor: 'Rosa' },
    { product: product('Kit Nuvem', 120), quantity: 1 },
  ],
  subtotal: 189.8,
  couponCode: null,
  couponDiscount: 0,
  deliveryMethod: 'Entrega',
  address: { street: 'Rua das Flores', number: '10', complement: '', neighborhood: 'Centro', city: 'São Paulo', state: 'SP', zipCode: '01000-000' },
  shippingCost: 25,
  total: 214.8,
  customerName: 'Maria',
  customerEmail: 'maria@exemplo.com',
  customerPhone: '(11) 91234-5678',
  notes: '',
  isGift: false,
  recipientName: '',
  giftMessage: '',
});

describe('buildWhatsappOrderMessage', () => {
  it('lists each item with quantity, embroidery, thread color and line total', () => {
    const text = buildWhatsappOrderMessage(baseDraft());
    expect(text).toContain('• 2× Fralda de Boca Florzinha (bordado "ANA", linha Rosa) — R$ 69,80');
    expect(text).toContain('• 1× Kit Nuvem — R$ 120,00');
  });

  it('includes the address, shipping estimate, total and contact', () => {
    const text = buildWhatsappOrderMessage(baseDraft());
    expect(text).toContain('Entrega: Rua das Flores, 10 — Centro — São Paulo/SP — CEP 01000-000');
    expect(text).toContain('Frete estimado: R$ 25,00');
    expect(text).toContain('Total estimado: R$ 214,80');
    expect(text).toContain('Meus dados: Maria · maria@exemplo.com · (11) 91234-5678');
  });

  it('says pickup instead of an address and skips blank optional parts', () => {
    const text = buildWhatsappOrderMessage({ ...baseDraft(), deliveryMethod: 'Retirada', address: null, shippingCost: 0, customerPhone: '' });
    expect(text).toContain('Entrega: retirar no ateliê');
    expect(text).not.toContain('Frete estimado');
    expect(text).toContain('Meus dados: Maria · maria@exemplo.com');
    expect(text).not.toContain('Observações');
  });

  it('mentions an unfilled address and no estimate yet', () => {
    const text = buildWhatsappOrderMessage({ ...baseDraft(), address: { street: '', number: '', complement: '', neighborhood: '', city: '', state: '', zipCode: '' }, shippingCost: null, total: 189.8 });
    expect(text).toContain('Entrega: endereço a combinar');
    expect(text).toContain('Total dos produtos: R$ 189,80 (frete a calcular)');
    expect(text).not.toContain('Frete estimado');
  });

  it('adds coupon, gift and notes when present', () => {
    const text = buildWhatsappOrderMessage({
      ...baseDraft(),
      couponCode: 'BEMVINDO',
      couponDiscount: 10,
      isGift: true,
      recipientName: 'Ana',
      giftMessage: 'Parabéns!',
      notes: 'Entregar à tarde',
    });
    expect(text).toContain('Cupom BEMVINDO: -R$ 10,00');
    expect(text).toContain('É presente — para Ana, recado: "Parabéns!"');
    expect(text).toContain('Observações: Entregar à tarde');
  });
});
