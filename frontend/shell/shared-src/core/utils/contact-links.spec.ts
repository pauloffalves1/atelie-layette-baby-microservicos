import { whatsappUrl } from './contact-links';

describe('whatsappUrl', () => {
  it('adds the Brazil country code to a masked local number', () => {
    expect(whatsappUrl('(11) 91234-5678')).toBe('https://wa.me/5511912345678');
  });

  it('keeps a number that already has the country code', () => {
    expect(whatsappUrl('+55 11 91234-5678')).toBe('https://wa.me/5511912345678');
  });

  it('accepts a 10-digit landline', () => {
    expect(whatsappUrl('1143211234')).toBe('https://wa.me/551143211234');
  });

  it('returns null for missing or too-short numbers', () => {
    expect(whatsappUrl(null)).toBeNull();
    expect(whatsappUrl('12345')).toBeNull();
  });

  it('url-encodes a prefilled message', () => {
    expect(whatsappUrl('11912345678', 'Olá, Ana!')).toBe('https://wa.me/5511912345678?text=Ol%C3%A1%2C%20Ana!');
  });
});
