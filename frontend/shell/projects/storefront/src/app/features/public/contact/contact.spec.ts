import { TestBed } from '@angular/core/testing';
import { AuthService } from '@shared/core/services/auth.service';
import { Contact } from './contact';

describe('Contact', () => {
  let component: Contact;
  let windowOpenSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Contact],
      providers: [{ provide: AuthService, useValue: { currentUser: () => null } }],
    });

    const fixture = TestBed.createComponent(Contact);
    component = fixture.componentInstance;
    fixture.detectChanges();

    windowOpenSpy = vi.spyOn(window, 'open').mockImplementation(() => null);
  });

  afterEach(() => {
    windowOpenSpy.mockRestore();
  });

  function submittedUrl(): string {
    expect(windowOpenSpy).toHaveBeenCalledTimes(1);
    return windowOpenSpy.mock.calls[0][0] as string;
  }

  it('does not open WhatsApp when required fields are missing', () => {
    component.submit();

    expect(windowOpenSpy).not.toHaveBeenCalled();
    expect(component.form.controls.customerName.touched).toBe(true);
  });

  it('opens a wa.me link with the atelier phone number for a general message', () => {
    component.form.patchValue({
      customerName: 'Maria Silva',
      customerPhone: '(11) 91234-5678',
      message: 'Olá, gostaria de saber o prazo.',
    });

    component.submit();

    const url = submittedUrl();
    expect(url).toContain('https://wa.me/5511913130481?text=');
    const text = decodeURIComponent(url.split('text=')[1]);
    expect(text).toContain('Maria Silva');
    expect(text).toContain('gostaria de saber o prazo');
    expect(text).not.toContain('encomenda personalizada');
  });

  it('includes the piece details in the message only when isCustomOrder is checked', () => {
    component.form.patchValue({
      customerName: 'Maria Silva',
      customerPhone: '(11) 91234-5678',
      isCustomOrder: true,
      tipoPeca: 'Fralda de Ombro',
      tamanho: 'Padrão',
      tecido: 'algodão pima',
      cor: 'rosa claro',
      message: 'Quero para o mês que vem.',
    });

    component.submit();

    const text = decodeURIComponent(submittedUrl().split('text=')[1]);
    expect(text).toContain('encomenda personalizada');
    expect(text).toContain('Tipo de peça: Fralda de Ombro');
    expect(text).toContain('Tecido desejado: algodão pima');
    expect(text).toContain('Cor: rosa claro');
  });

  it('appends email only when provided, and always appends phone', () => {
    component.form.patchValue({
      customerName: 'Maria Silva',
      customerPhone: '(11) 91234-5678',
      message: 'Mensagem de teste.',
    });

    component.submit();

    const text = decodeURIComponent(submittedUrl().split('text=')[1]);
    expect(text).not.toContain('E-mail:');
    expect(text).toContain('Telefone: (11) 91234-5678');
  });

  it('does not open WhatsApp when phone is missing', () => {
    component.form.patchValue({ customerName: 'Maria Silva', message: 'Mensagem de teste.' });

    component.submit();

    expect(windowOpenSpy).not.toHaveBeenCalled();
    expect(component.form.controls.customerPhone.touched).toBe(true);
  });

  it('prefills name and email for a logged-in customer', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [Contact],
      providers: [
        {
          provide: AuthService,
          useValue: { currentUser: () => ({ id: 'c1', name: 'João Souza', email: 'joao@example.com' }) },
        },
      ],
    });

    const fixture = TestBed.createComponent(Contact);
    fixture.detectChanges();
    const prefilled = fixture.componentInstance;

    expect(prefilled.form.controls.customerName.value).toBe('João Souza');
    expect(prefilled.form.controls.customerEmail.value).toBe('joao@example.com');
  });
});
