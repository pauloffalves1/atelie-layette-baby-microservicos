export interface CustomerSummary {
  id: string;
  name: string;
  email: string;
  phone: string | null;
  cpf: string | null;
  createdAt: string;
  isAnonymized: boolean;
  emailVerified: boolean;
  addressStreet: string | null;
  addressNumber: string | null;
  addressComplement: string | null;
  addressNeighborhood: string | null;
  addressCity: string | null;
  addressState: string | null;
  addressZipCode: string | null;
  /** RF40 — aprovada por uma administradora como usuária de teste: vê os produtos de teste e tudo que compra é compra de teste. */
  isTest?: boolean;
}

export interface UpdateCustomerRequest {
  name: string;
  email: string;
  cpf: string;
  phone: string | null;
  addressStreet?: string | null;
  addressNumber?: string | null;
  addressComplement?: string | null;
  addressNeighborhood?: string | null;
  addressCity?: string | null;
  addressState?: string | null;
  addressZipCode?: string | null;
}
