/**
 * wa.me deep link for a Brazilian phone typed any way ("(11) 91234-5678", "11912345678",
 * "+55 11 91234-5678"), or null when there aren't enough digits to be a real number.
 */
export function whatsappUrl(phone: string | null | undefined, message?: string): string | null {
  const digits = (phone ?? '').replace(/\D/g, '');
  if (digits.length < 10) return null;
  const international = digits.length <= 11 ? `55${digits}` : digits;
  const text = message ? `?text=${encodeURIComponent(message)}` : '';
  return `https://wa.me/${international}${text}`;
}
