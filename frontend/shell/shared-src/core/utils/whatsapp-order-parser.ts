/**
 * Reads back the order summary the storefront pre-fills in WhatsApp (buildWhatsappOrderMessage) so
 * the admin can paste the customer's message into "Registrar encomenda" instead of retyping every
 * item, embroidery, address and contact. Tolerant: WhatsApp may add or drop blank lines, and a
 * customer may edit the text — whatever isn't recognized is simply left for the admin to fill.
 */
export interface ParsedWhatsappOrderItem {
  name: string;
  quantity: number;
  unitPrice: number;
  embroideryText: string | null;
  threadColor: string | null;
}

export interface ParsedWhatsappOrder {
  items: ParsedWhatsappOrderItem[];
  deliveryMethod: 'Entrega' | 'Retirada' | null;
  address: {
    street: string;
    number: string;
    complement: string;
    neighborhood: string;
    city: string;
    state: string;
    zipCode: string;
  } | null;
  shippingCost: number | null;
  couponCode: string | null;
  couponDiscount: number | null;
  customerName: string | null;
  customerEmail: string | null;
  customerPhone: string | null;
  isGift: boolean;
  recipientName: string | null;
  giftMessage: string | null;
  notes: string | null;
}

/** "R$ 1.234,56" → 1234.56 */
export function parseBrl(text: string): number | null {
  const digits = text.replace(/[^\d,.-]/g, '').replace(/\./g, '').replace(',', '.');
  const value = Number.parseFloat(digits);
  return Number.isFinite(value) ? value : null;
}

const ITEM_LINE = /^[•*\-–]\s*(\d+)\s*[×xX]\s*(.+?)\s+[—–-]\s+(R\$\s*[\d.,]+)\s*$/;

export function parseWhatsappOrderMessage(message: string): ParsedWhatsappOrder {
  const result: ParsedWhatsappOrder = {
    items: [],
    deliveryMethod: null,
    address: null,
    shippingCost: null,
    couponCode: null,
    couponDiscount: null,
    customerName: null,
    customerEmail: null,
    customerPhone: null,
    isGift: false,
    recipientName: null,
    giftMessage: null,
    notes: null,
  };

  const lines = message.replace(/\r/g, '').replace(/ /g, ' ').split('\n').map((line) => line.trim()).filter(Boolean);

  for (const line of lines) {
    const item = line.match(ITEM_LINE);
    if (item) {
      const quantity = Number.parseInt(item[1], 10);
      let name = item[2].trim();
      let embroideryText: string | null = null;
      let threadColor: string | null = null;
      const details = name.match(/^(.*?)\s*\((.*)\)$/);
      if (details && /bordado|linha/i.test(details[2])) {
        name = details[1].trim();
        embroideryText = details[2].match(/bordado\s+"([^"]*)"/i)?.[1] ?? null;
        threadColor = details[2].match(/linha\s+([^,)]+)/i)?.[1]?.trim() ?? null;
      }
      const lineTotal = parseBrl(item[3]) ?? 0;
      result.items.push({ name, quantity, unitPrice: quantity > 0 ? Math.round((lineTotal / quantity) * 100) / 100 : lineTotal, embroideryText, threadColor });
      continue;
    }

    let match: RegExpMatchArray | null;
    if ((match = line.match(/^Entrega:\s*(.+)$/i))) {
      const value = match[1].trim();
      if (/retirar no ateli/i.test(value)) {
        result.deliveryMethod = 'Retirada';
      } else {
        result.deliveryMethod = 'Entrega';
        if (!/a combinar/i.test(value)) result.address = parseAddress(value);
      }
    } else if ((match = line.match(/^Frete estimado:\s*(.+)$/i))) {
      result.shippingCost = /gr[áa]tis/i.test(match[1]) ? 0 : parseBrl(match[1]);
    } else if ((match = line.match(/^Cupom\s+(\S+):\s*-?\s*(R\$.*)$/i))) {
      result.couponCode = match[1];
      result.couponDiscount = parseBrl(match[2]);
    } else if ((match = line.match(/^Meus dados:\s*(.+)$/i))) {
      for (const part of match[1].split('·').map((p) => p.trim()).filter(Boolean)) {
        if (part.includes('@')) result.customerEmail = part;
        else if (part.replace(/\D/g, '').length >= 10 && /^[\d\s()+-]+$/.test(part)) result.customerPhone = part;
        else if (!result.customerName) result.customerName = part;
      }
    } else if ((match = line.match(/^É presente(?:\s*[—–-]\s*(.*))?$/i))) {
      result.isGift = true;
      const detail = match[1] ?? '';
      result.giftMessage = detail.match(/recado:\s*"(.*)"/i)?.[1] ?? null;
      result.recipientName = detail.match(/^para\s+(.+?)(?:,\s*recado:|$)/i)?.[1]?.trim() ?? null;
    } else if ((match = line.match(/^Observações:\s*(.+)$/i))) {
      result.notes = match[1].trim();
    }
  }

  return result;
}

/** "Rua das Flores, 10 — Apto 2 — Centro — São Paulo/SP — CEP 01000-000" (parts may be missing). */
function parseAddress(text: string): ParsedWhatsappOrder['address'] {
  const parts = text.split(/\s+[—–]\s+/).map((part) => part.trim()).filter(Boolean);
  const address = { street: '', number: '', complement: '', neighborhood: '', city: '', state: '', zipCode: '' };

  const zipIndex = parts.findIndex((part) => /^CEP\s+/i.test(part));
  if (zipIndex >= 0) address.zipCode = parts.splice(zipIndex, 1)[0].replace(/^CEP\s+/i, '');

  const [streetLine = '', ...rest] = parts;
  const comma = streetLine.lastIndexOf(',');
  if (comma > 0) {
    address.street = streetLine.slice(0, comma).trim();
    address.number = streetLine.slice(comma + 1).trim();
  } else {
    address.street = streetLine;
  }

  const cityIndex = rest.findIndex((part) => /^.+\/[A-Za-z]{2}$/.test(part));
  if (cityIndex >= 0) {
    const [city, state] = [rest[cityIndex].slice(0, -3), rest[cityIndex].slice(-2)];
    address.city = city.trim();
    address.state = state.toUpperCase();
    if (cityIndex >= 1) address.neighborhood = rest[cityIndex - 1];
    if (cityIndex >= 2) address.complement = rest.slice(0, cityIndex - 1).join(' — ');
  }

  return address;
}
