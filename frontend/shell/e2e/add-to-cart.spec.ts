import { test, expect } from '@playwright/test';

test('cliente adiciona um produto ao carrinho e o vê na página de carrinho', async ({ page }) => {
  await page.goto('/loja');

  const firstProductCard = page.locator('.card-product').first();
  await expect(firstProductCard).toBeVisible({ timeout: 15_000 });
  const productName = (await firstProductCard.locator('h6').innerText()).trim();

  await firstProductCard.getByRole('link').first().click();
  await expect(page).toHaveURL(/\/produto\//);

  // Every product requires an embroidery text and a thread color before it can be added — enforced
  // client-side in ProductDetail.addToCart() (blocked + touched flags, no request to the backend).
  await page.locator('#embroidery-text').fill('ANA');
  await page
    .locator('div.my-4', { hasText: 'Cor da linha de bordado' })
    .getByRole('button')
    .first()
    .click();

  await page.getByRole('button', { name: 'Adicionar ao carrinho' }).click();
  await expect(page.getByText('Adicionado ao carrinho!')).toBeVisible();

  // The cart lives in localStorage (CartService), so a normal navigation to /carrinho — not the
  // "Ver carrinho" button, which just opens a summary modal in place — is what a customer who
  // clicks the "Carrinho" nav link would see.
  await page.goto('/carrinho');

  await expect(page.getByText('Seu carrinho está vazio.')).toHaveCount(0);
  await expect(page.getByText(productName)).toBeVisible();
});
