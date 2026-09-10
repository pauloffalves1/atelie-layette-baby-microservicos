import { test, expect } from '@playwright/test';

test.describe('Home e navegação até um produto', () => {
  test('home mostra o hero e leva até a loja', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { level: 1 })).toContainText('fraldas de ombro e boca');
    await page.getByRole('link', { name: 'Ver a loja' }).click();

    await expect(page).toHaveURL(/\/loja$/);
    await expect(page.getByRole('heading', { name: 'Nossa Loja' })).toBeVisible();
  });

  test('loja lista produtos do catálogo seedado e abre o detalhe de um deles', async ({ page }) => {
    await page.goto('/loja');

    const firstProductCard = page.locator('.card-product').first();
    await expect(firstProductCard).toBeVisible({ timeout: 15_000 });
    const productName = await firstProductCard.locator('h6').innerText();

    await firstProductCard.getByRole('link').first().click();

    await expect(page).toHaveURL(/\/produto\//);
    await expect(page.getByRole('heading', { level: 1 })).toContainText(productName.trim());
  });

  test('busca na loja filtra a lista de produtos', async ({ page }) => {
    await page.goto('/loja');
    await expect(page.locator('.card-product').first()).toBeVisible({ timeout: 15_000 });

    await page.getByPlaceholder('Buscar produto pelo nome...').fill('produto-que-nao-existe-xyz');

    await expect(page.getByText('Nenhum produto encontrado para')).toBeVisible();
  });
});
