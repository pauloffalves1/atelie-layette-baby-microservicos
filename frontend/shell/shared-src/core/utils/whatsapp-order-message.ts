import { CartItem } from '../models/cart.model';

export interface WhatsappOrderDraft {
  items: CartItem[];
  subtotal: number;
  couponCode: string | null;
  couponDiscount: number;
  deliveryMethod: 'Entrega' | 'Retirada';
  /** Only for Entrega; any blank part is left out. */
  address: {
    street: string;
    number: string;
    complement: string;
    neighborhood: string;
    city: string;
    state: string;
    zipCode: string;
  } | null;
  /** Null when the CEP hasn't been filled yet, so there's no estimate to quote. */
  shippingCost: number | null;
  /** With the shipping estimate included — or without it when shippingCost is null. */
  total: number;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  notes: string;
  isGift: boolean;
  recipientName: string;
  giftMessage: string;
}

const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const money = (value: number) => brl.format(value).replace(/ /g, ' ');

/**
 * The text the "Enviar pedido pelo WhatsApp" button pre-fills while on-line payment is off.
 * The button used to open an empty chat (and empty the cart), so the customer had to retype every
 * product, embroidery and color — and the ateliê had nothing to go on.
 */
export function buildWhatsappOrderMessage(draft: WhatsappOrderDraft): string {
  const lines: string[] = ['Olá! Quero fazer este pedido pelo site:', ''];

  for (const item of draft.items) {
    const details = [
      item.embroideryText ? `bordado "${item.embroideryText}"` : null,
      item.threadColor ? `linha ${item.threadColor}` : null,
    ].filter((part): part is string => part !== null);
    const detailText = details.length ? ` (${details.join(', ')})` : '';
    lines.push(`• ${item.quantity}× ${item.product.name}${detailText} — ${money(item.product.effectivePrice * item.quantity)}`);
  }

  lines.push('', `Subtotal: ${money(draft.subtotal)}`);
  if (draft.couponCode && draft.couponDiscount > 0) {
    lines.push(`Cupom ${draft.couponCode}: -${money(draft.couponDiscount)}`);
  }

  if (draft.deliveryMethod === 'Retirada') {
    lines.push('Entrega: retirar no ateliê');
  } else {
    const a = draft.address;
    const streetLine = a ? [a.street, a.number].filter((p) => p.trim()).join(', ') : '';
    const place = a ? [a.complement, a.neighborhood, [a.city, a.state].filter((p) => p.trim()).join('/')].filter((p) => p.trim()).join(' — ') : '';
    const where = [streetLine, place, a?.zipCode.trim() ? `CEP ${a.zipCode.trim()}` : ''].filter(Boolean).join(' — ');
    lines.push(`Entrega: ${where || 'endereço a combinar'}`);
    if (draft.shippingCost !== null) {
      lines.push(`Frete estimado: ${draft.shippingCost === 0 ? 'grátis' : money(draft.shippingCost)}`);
    }
  }
  if (draft.deliveryMethod === 'Entrega' && draft.shippingCost === null) {
    lines.push(`Total dos produtos: ${money(draft.total)} (frete a calcular)`);
  } else {
    lines.push(`Total estimado: ${money(draft.total)}`);
  }

  const contact = [draft.customerName, draft.customerEmail, draft.customerPhone].map((p) => p.trim()).filter(Boolean);
  if (contact.length) lines.push('', `Meus dados: ${contact.join(' · ')}`);

  if (draft.isGift && (draft.recipientName.trim() || draft.giftMessage.trim())) {
    const gift = [
      draft.recipientName.trim() ? `para ${draft.recipientName.trim()}` : null,
      draft.giftMessage.trim() ? `recado: "${draft.giftMessage.trim()}"` : null,
    ].filter(Boolean);
    lines.push(`É presente — ${gift.join(', ')}`);
  } else if (draft.isGift) {
    lines.push('É presente');
  }
  if (draft.notes.trim()) lines.push(`Observações: ${draft.notes.trim()}`);

  return lines.join('\n');
}
