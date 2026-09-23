import { expect, test } from '@playwright/test';
import { registerCandidate, signIn } from '../helpers/auth.mjs';

test('anonymous visitors are redirected from HR offer templates to sign in', async ({ page }) => {
  await page.goto('/OfferTemplate/Index');

  await expect(page).toHaveURL(/\/Identity\/Account\/Login(?:\?|$)/);
  await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible();
});

test('an invalid System Admin password is rejected', async ({ page }) => {
  await signIn(page, 'admin@recruitmenttracker.com', 'wrong-qa-password');

  await expect(page).toHaveURL(/\/Identity\/Account\/Login(?:\?|$)/);
  await expect(page.getByText('Invalid login attempt.')).toBeVisible();
});

test('a newly registered candidate cannot open the HR offer templates', async ({ page }) => {
  const email = `qa-candidate-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@example.test`;
  await registerCandidate(page, email);

  await page.goto('/OfferTemplate/Index');
  await expect(page).toHaveURL(/\/Identity\/Account\/AccessDenied(?:\?|$)/);
});
