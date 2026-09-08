export interface CustomerSummary {
  id: string;
  name: string;
  email: string;
  phone: string | null;
  cpf: string | null;
  createdAt: string;
  isAnonymized: boolean;
  addressStreet: string | null;
  addressNumber: string | null;
  addressComplement: string | null;
  addressNeighborhood: string | null;
  addressCity: string | null;
  addressState: string | null;
  addressZipCode: string | null;
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
