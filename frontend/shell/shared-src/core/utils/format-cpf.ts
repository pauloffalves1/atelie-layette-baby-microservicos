/** Formats a CPF's raw digits (as stored/returned by the API) as "000.000.000-00" for display in an editable field. */
export function formatCpf(cpf: string | null | undefined): string {
  if (!cpf) return '';

  const digits = cpf.replace(/\D/g, '');
  if (digits.length !== 11) return cpf;

  return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9)}`;
}
