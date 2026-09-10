import { test, expect } from '@playwright/test';

// Pure client-side validation (Angular reactive forms) — no backend call involved, so this spec
// has no dependency on any seeded/real customer account.
test.describe('Validação do formulário de login', () => {
  test('envio em branco mostra as mensagens de campo obrigatório', async ({ page }) => {
    await page.goto('/entrar');

    await page.getByRole('button', { name: 'Entrar' }).click();

    await expect(page.getByText('Informe um e-mail válido.')).toBeVisible();
    await expect(page.getByText('Senha é obrigatória.')).toBeVisible();
  });

  test('e-mail com formato inválido mostra a mensagem de e-mail inválido', async ({ page }) => {
    await page.goto('/entrar');

    await page.locator('#login-email').fill('nao-e-um-email');
    await page.locator('#login-password').fill('senha123');
    await page.getByRole('button', { name: 'Entrar' }).click();

    await expect(page.getByText('Informe um e-mail válido.')).toBeVisible();
    await expect(page.getByText('Senha é obrigatória.')).toHaveCount(0);
  });

  test('link "Cadastre-se" leva para a página de cadastro', async ({ page }) => {
    await page.goto('/entrar');

    await page.getByRole('link', { name: 'Cadastre-se' }).click();

    await expect(page).toHaveURL(/\/cadastro/);
  });
});
